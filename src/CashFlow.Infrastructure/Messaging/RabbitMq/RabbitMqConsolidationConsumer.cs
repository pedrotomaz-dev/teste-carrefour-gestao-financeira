using System.Text;
using System.Text.Json;
using CashFlow.Application.Consolidation.Commands.ConsolidateCashEntry;
using CashFlow.Domain.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CashFlow.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Consumer hospedado no CashFlow.ConsolidationWorker. Implementa o requisito não-funcional de
/// suportar picos de 50 req/s tolerando até 5% de perda: um limite de concorrência (semáforo)
/// funciona como bulkhead — sob sobrecarga sustentada, novas entregas são rejeitadas sem
/// re-enfileirar (nack, requeue:false) e caem na Dead Letter Queue em vez de deixar a fila
/// principal e a memória do processo crescerem sem controle. Nada é perdido de verdade: a DLQ
/// guarda essas mensagens para reprocessamento em lote fora do caminho de consolidação em tempo
/// real (ver docs/architecture.md).
/// </summary>
public class RabbitMqConsolidationConsumer(
    RabbitMqConnectionProvider connectionProvider,
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqConsolidationConsumer> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;
    private SemaphoreSlim _capacity = null!;
    private ResiliencePipeline _retryPipeline = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _capacity = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(100)
            })
            .Build();

        // No docker-compose, o container do worker pode terminar de subir antes de o listener
        // AMQP do RabbitMQ estar de fato aceitando conexões (o healthcheck do container passa
        // um instante antes disso). Sem retry aqui, essa janela transitória derrubava o worker
        // inteiro (BackgroundServiceExceptionBehavior.StopHost é o padrão) — o próprio serviço
        // responsável por resiliência não tolerava a indisponibilidade momentânea do broker.
        var startupPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = int.MaxValue,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(30),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "RabbitMQ ainda não disponível para o worker (tentativa {Attempt}). Nova tentativa em {Delay}.",
                        args.AttemptNumber + 1, args.RetryDelay);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        IChannel channel = null!;
        await startupPipeline.ExecuteAsync(async ct =>
        {
            var connection = await connectionProvider.GetConnectionAsync(ct);
            channel = await connection.CreateChannelAsync(cancellationToken: ct);

            await RabbitMqTopology.DeclareAsync(channel, _options, ct);
            await channel.BasicQosAsync(0, _options.PrefetchCount, global: false, ct);
        }, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => OnMessageReceivedAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(_options.Queue, autoAck: false, consumer, stoppingToken);

        logger.LogInformation("Consumidor de consolidação (RabbitMQ) pronto na fila '{Queue}'.", _options.Queue);

        // O trabalho real acontece nos callbacks de ReceivedAsync; este método só precisa
        // permanecer ativo até o worker ser encerrado.
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Encerramento normal do worker (docker stop / Ctrl+C).
        }
    }

    private async Task OnMessageReceivedAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        if (!await _capacity.WaitAsync(TimeSpan.FromMilliseconds(250), stoppingToken))
        {
            logger.LogWarning(
                "Capacidade de consolidação esgotada (pico sustentado) — mensagem {DeliveryTag} enviada à DLQ para reprocessamento posterior.",
                delivery.DeliveryTag);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, stoppingToken);
            return;
        }

        try
        {
            await _retryPipeline.ExecuteAsync(async ct =>
            {
                var json = Encoding.UTF8.GetString(delivery.Body.Span);
                var @event = JsonSerializer.Deserialize<CashEntryRegisteredEvent>(json)
                    ?? throw new InvalidOperationException("Payload de evento vazio.");

                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new ConsolidateCashEntryCommand(@event), ct);
            }, stoppingToken);

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao consolidar mensagem {DeliveryTag} após retries — enviada à DLQ.", delivery.DeliveryTag);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, stoppingToken);
        }
        finally
        {
            _capacity.Release();
        }
    }
}
