using CashFlow.Domain.Enums;
using MediatR;

namespace CashFlow.Application.CashEntries.Commands.RegisterCashEntry;

public record RegisterCashEntryCommand(string Description, decimal Amount, EntryType Type, DateOnly? OccurredOn) : IRequest<Guid>;
