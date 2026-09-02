using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Events;

/// <summary>
/// Evento de integração publicado após um lançamento ser persistido. É o contrato serializado
/// no Outbox (produtor) e consumido pelo ConsolidationWorker (consumidor) — por isso vive no
/// Domain, compartilhado pelos dois lados sem precisar duplicar o DTO.
/// </summary>
public sealed record CashEntryRegisteredEvent(
    Guid EventId,
    Guid EntryId,
    decimal Amount,
    EntryType Type,
    DateOnly OccurredOn);
