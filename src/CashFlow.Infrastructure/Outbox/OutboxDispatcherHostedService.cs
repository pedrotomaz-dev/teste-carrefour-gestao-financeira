using CashFlow.Application.Interfaces;
using CashFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashFlow.Infrastructure.Outbox;

/// <summary>
/// Despacha mensagens pendentes do Outbox para o publisher configurado. Roda dentro do processo
/// da Api, mas é totalmente desacoplado da requisição HTTP: se o broker estiver fora do ar, esta
/// tarefa fica falhando/tentando novamente em segundo plano — a Api continua aceitando novos
/// lançamentos normalmente, satisfazendo o requisito de disponibilidade independente da
/// consolidação.
/// </summary>
public class OutboxDispatcherHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxDispatcherOptions> options,
    ILogger<OutboxDispatcherHostedService> logger) : BackgroundService
{
    private readonly OutboxDispatcherOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(_options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha inesperada ao despachar mensagens do outbox.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task DispatchPendingMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var pending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(stoppingToken);

        foreach (var message in pending)
        {
            try
            {
                await publisher.PublishAsync(message.Type, message.Content, stoppingToken);
                message.MarkAsProcessed();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.RegisterFailure(ex.Message);
                logger.LogWarning(ex, "Falha ao publicar mensagem de outbox {MessageId}. Será retentada no próximo ciclo.", message.Id);
            }
        }

        if (pending.Count > 0)
        {
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
