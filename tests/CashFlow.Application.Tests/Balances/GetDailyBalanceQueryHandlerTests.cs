using CashFlow.Application.Balances.Queries.GetDailyBalance;
using CashFlow.Application.Tests.TestSupport;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CashFlow.Application.Tests.Balances;

public class GetDailyBalanceQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingDate_ReturnsBalance()
    {
        using var context = TestDbContext.CreateInMemory();
        var date = new DateOnly(2026, 9, 1);
        var balance = new DailyBalance(date);
        balance.Apply(EntryType.Credit, 200m);
        context.DailyBalances.Add(balance);
        await context.SaveChangesAsync();

        var handler = new GetDailyBalanceQueryHandler(context);
        var result = await handler.Handle(new GetDailyBalanceQuery(date), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Balance.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_DateWithoutBalance_ReturnsNull()
    {
        using var context = TestDbContext.CreateInMemory();
        var handler = new GetDailyBalanceQueryHandler(context);

        var result = await handler.Handle(new GetDailyBalanceQuery(new DateOnly(2020, 1, 1)), CancellationToken.None);

        result.Should().BeNull();
    }
}
