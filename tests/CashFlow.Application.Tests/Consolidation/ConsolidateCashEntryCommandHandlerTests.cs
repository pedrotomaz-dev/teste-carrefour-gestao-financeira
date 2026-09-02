using CashFlow.Application.Consolidation.Commands.ConsolidateCashEntry;
using CashFlow.Application.Tests.TestSupport;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Events;
using FluentAssertions;
using Xunit;

namespace CashFlow.Application.Tests.Consolidation;

public class ConsolidateCashEntryCommandHandlerTests
{
    [Fact]
    public async Task Handle_FirstEvent_CreatesDailyBalance()
    {
        using var context = TestDbContext.CreateInMemory();
        var handler = new ConsolidateCashEntryCommandHandler(context);
        var occurredOn = new DateOnly(2026, 9, 1);

        var @event = new CashEntryRegisteredEvent(Guid.NewGuid(), Guid.NewGuid(), 150.75m, EntryType.Credit, occurredOn);
        await handler.Handle(new ConsolidateCashEntryCommand(@event), CancellationToken.None);

        var balance = context.DailyBalances.Single(b => b.Date == occurredOn);
        balance.TotalCredits.Should().Be(150.75m);
        balance.Balance.Should().Be(150.75m);
    }

    [Fact]
    public async Task Handle_MultipleEventsSameDay_AccumulatesOnSameRow()
    {
        using var context = TestDbContext.CreateInMemory();
        var handler = new ConsolidateCashEntryCommandHandler(context);
        var occurredOn = new DateOnly(2026, 9, 1);

        await handler.Handle(new ConsolidateCashEntryCommand(new CashEntryRegisteredEvent(Guid.NewGuid(), Guid.NewGuid(), 100m, EntryType.Credit, occurredOn)), CancellationToken.None);
        await handler.Handle(new ConsolidateCashEntryCommand(new CashEntryRegisteredEvent(Guid.NewGuid(), Guid.NewGuid(), 40m, EntryType.Debit, occurredOn)), CancellationToken.None);

        context.DailyBalances.Count(b => b.Date == occurredOn).Should().Be(1);
        var balance = context.DailyBalances.Single(b => b.Date == occurredOn);
        balance.Balance.Should().Be(60m);
    }

    [Fact]
    public async Task Handle_DuplicateEventId_IsIgnoredIdempotently()
    {
        using var context = TestDbContext.CreateInMemory();
        var handler = new ConsolidateCashEntryCommandHandler(context);
        var occurredOn = new DateOnly(2026, 9, 1);
        var duplicateEvent = new CashEntryRegisteredEvent(Guid.NewGuid(), Guid.NewGuid(), 100m, EntryType.Credit, occurredOn);

        // Simula reentrega da mesma mensagem (garantia "at-least-once" da fila).
        await handler.Handle(new ConsolidateCashEntryCommand(duplicateEvent), CancellationToken.None);
        await handler.Handle(new ConsolidateCashEntryCommand(duplicateEvent), CancellationToken.None);

        var balance = context.DailyBalances.Single(b => b.Date == occurredOn);
        balance.TotalCredits.Should().Be(100m, "o mesmo evento não pode ser contado duas vezes no saldo");
    }

    [Fact]
    public async Task Handle_EventsOnDifferentDays_KeepSeparateBalances()
    {
        using var context = TestDbContext.CreateInMemory();
        var handler = new ConsolidateCashEntryCommandHandler(context);

        await handler.Handle(new ConsolidateCashEntryCommand(new CashEntryRegisteredEvent(Guid.NewGuid(), Guid.NewGuid(), 100m, EntryType.Credit, new DateOnly(2026, 9, 1))), CancellationToken.None);
        await handler.Handle(new ConsolidateCashEntryCommand(new CashEntryRegisteredEvent(Guid.NewGuid(), Guid.NewGuid(), 50m, EntryType.Credit, new DateOnly(2026, 9, 2))), CancellationToken.None);

        context.DailyBalances.Count().Should().Be(2);
    }
}
