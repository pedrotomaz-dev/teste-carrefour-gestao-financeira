using System.Text.Json;
using CashFlow.Application.Consolidation.Commands.ConsolidateCashEntry;
using CashFlow.Domain.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CashFlow.Infrastructure.Messaging.InMemory;

/// <summary>
/// Hospedado apenas no processo do CashFlow.Api quando Messaging:Provider = InMemory. Consome o
/// canal em memória e aplica a consolidação chamando o mesmo handler de Application usado pelo
/// consumer RabbitMQ real — a regra de negócio é idêntica, só o transporte muda.
/// </summary>
public class InMemoryConsolidationConsumer(
    InMemoryMessageBus bus,
    IServiceScopeFactory scopeFactory,
    ILogger<InMemoryConsolidationConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var envelope in bus.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var @event = JsonSerializer.Deserialize<CashEntryRegisteredEvent>(envelope.Content)
                    ?? throw new InvalidOperationException("Payload de evento vazio.");

                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new ConsolidateCashEntryCommand(@event), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha ao consolidar evento em memória: {Payload}", envelope.Content);
            }
        }
    }
}
