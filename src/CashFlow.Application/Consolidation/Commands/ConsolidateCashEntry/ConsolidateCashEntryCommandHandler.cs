using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Application.Consolidation.Commands.ConsolidateCashEntry;

public class ConsolidateCashEntryCommandHandler(IConsolidationDbContext context)
    : IRequestHandler<ConsolidateCashEntryCommand>
{
    public async Task Handle(ConsolidateCashEntryCommand request, CancellationToken cancellationToken)
    {
        var @event = request.Event;

        // Inbox Pattern: a entrega é "at-least-once", então o mesmo evento pode chegar mais de
        // uma vez. Sem esta checagem, uma reentrega duplicaria o valor no saldo do dia.
        var alreadyProcessed = await context.ProcessedIntegrationEvents
            .AsNoTracking()
            .AnyAsync(p => p.EventId == @event.EventId, cancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        var dailyBalance = await context.DailyBalances
            .FirstOrDefaultAsync(b => b.Date == @event.OccurredOn, cancellationToken);

        if (dailyBalance is null)
        {
            dailyBalance = new DailyBalance(@event.OccurredOn);
            context.DailyBalances.Add(dailyBalance);
        }

        dailyBalance.Apply(@event.Type, @event.Amount);
        context.ProcessedIntegrationEvents.Add(new ProcessedIntegrationEvent(@event.EventId));

        await context.SaveChangesAsync(cancellationToken);
    }
}
