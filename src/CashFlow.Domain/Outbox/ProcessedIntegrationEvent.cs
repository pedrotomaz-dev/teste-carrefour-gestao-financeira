namespace CashFlow.Domain.Outbox;

/// <summary>
/// Registro de deduplicação (Inbox Pattern) usado pelo ConsolidationWorker: como a entrega via
/// fila é "at-least-once", o mesmo evento pode chegar mais de uma vez (reentrega após falha,
/// reinício do worker etc.). Antes de aplicar um evento no saldo diário, o worker verifica se o
/// <see cref="EventId"/> já foi processado — garantindo que a consolidação seja idempotente.
/// </summary>
public class ProcessedIntegrationEvent
{
    public Guid EventId { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    private ProcessedIntegrationEvent() { }

    public ProcessedIntegrationEvent(Guid eventId)
    {
        EventId = eventId;
        ProcessedAt = DateTime.UtcNow;
    }
}
