using System.Text.Json;
using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Events;
using CashFlow.Domain.Outbox;
using MediatR;

namespace CashFlow.Application.CashEntries.Commands.RegisterCashEntry;

public class RegisterCashEntryCommandHandler(IAppDbContext context, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<RegisterCashEntryCommand, Guid>
{
    public async Task<Guid> Handle(RegisterCashEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = new CashEntry(request.Description, request.Amount, request.Type, request.OccurredOn ?? dateTimeProvider.Today);
        context.CashEntries.Add(entry);

        var integrationEvent = new CashEntryRegisteredEvent(Guid.NewGuid(), entry.Id, entry.Amount, entry.Type, entry.OccurredOn);
        var outboxMessage = new OutboxMessage(nameof(CashEntryRegisteredEvent), JsonSerializer.Serialize(integrationEvent));
        context.OutboxMessages.Add(outboxMessage);

        // Uma única transação implícita: o lançamento e o evento de outbox são gravados juntos
        // ou nenhum dos dois é — a API nunca fica em um estado onde o dado existe sem o evento
        // correspondente pronto para ser publicado.
        await context.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }
}
