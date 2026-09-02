using System.Text.Json;
using CashFlow.Application.CashEntries.Commands.RegisterCashEntry;
using CashFlow.Application.Tests.TestSupport;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Events;
using FluentAssertions;
using Xunit;

namespace CashFlow.Application.Tests.CashEntries;

public class RegisterCashEntryCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_PersistsCashEntry()
    {
        using var context = TestDbContext.CreateInMemory();
        var dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 9, 1));
        var handler = new RegisterCashEntryCommandHandler(context, dateTimeProvider);

        var command = new RegisterCashEntryCommand("Venda no cartão", 150.75m, EntryType.Credit, null);

        var id = await handler.Handle(command, CancellationToken.None);

        var entry = await context.CashEntries.FindAsync(id);
        entry.Should().NotBeNull();
        entry!.Amount.Should().Be(150.75m);
        entry.OccurredOn.Should().Be(dateTimeProvider.Today);
    }

    [Fact]
    public async Task Handle_ValidCommand_UsesProvidedOccurredOnWhenGiven()
    {
        using var context = TestDbContext.CreateInMemory();
        var handler = new RegisterCashEntryCommandHandler(context, new FixedDateTimeProvider(DateTime.UtcNow));
        var explicitDate = new DateOnly(2026, 1, 15);

        var id = await handler.Handle(new RegisterCashEntryCommand("Aluguel", 500, EntryType.Debit, explicitDate), CancellationToken.None);

        var entry = await context.CashEntries.FindAsync(id);
        entry!.OccurredOn.Should().Be(explicitDate);
    }

    [Fact]
    public async Task Handle_ValidCommand_WritesOutboxMessageInSameCall()
    {
        using var context = TestDbContext.CreateInMemory();
        var handler = new RegisterCashEntryCommandHandler(context, new FixedDateTimeProvider(DateTime.UtcNow));

        var id = await handler.Handle(new RegisterCashEntryCommand("Venda", 100, EntryType.Credit, null), CancellationToken.None);

        var outboxMessage = context.OutboxMessages.Single();
        outboxMessage.Type.Should().Be(nameof(CashEntryRegisteredEvent));
        outboxMessage.IsProcessed.Should().BeFalse();

        var payload = JsonSerializer.Deserialize<CashEntryRegisteredEvent>(outboxMessage.Content);
        payload!.EntryId.Should().Be(id);
        payload.Amount.Should().Be(100);
        payload.Type.Should().Be(EntryType.Credit);
    }
}
