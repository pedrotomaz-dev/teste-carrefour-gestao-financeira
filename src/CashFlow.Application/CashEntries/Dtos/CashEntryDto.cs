using CashFlow.Domain.Enums;

namespace CashFlow.Application.CashEntries.Dtos;

public record CashEntryDto(Guid Id, string Description, decimal Amount, EntryType Type, DateOnly OccurredOn, DateTime CreatedAt);
