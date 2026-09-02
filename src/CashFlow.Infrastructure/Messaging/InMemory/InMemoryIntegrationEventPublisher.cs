using CashFlow.Application.Interfaces;

namespace CashFlow.Infrastructure.Messaging.InMemory;

public class InMemoryIntegrationEventPublisher(InMemoryMessageBus bus) : IIntegrationEventPublisher
{
    public async Task PublishAsync(string type, string content, CancellationToken cancellationToken = default)
    {
        await bus.Writer.WriteAsync(new InMemoryEnvelope(type, content), cancellationToken);
    }
}
