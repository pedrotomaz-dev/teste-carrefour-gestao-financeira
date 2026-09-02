using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CashFlow.Domain.Tests.Entities;

public class DailyBalanceTests
{
    [Fact]
    public void Apply_SingleCredit_UpdatesTotalsAndBalance()
    {
        var balance = new DailyBalance(new DateOnly(2026, 9, 1));

        balance.Apply(EntryType.Credit, 150.75m);

        balance.TotalCredits.Should().Be(150.75m);
        balance.TotalDebits.Should().Be(0);
        balance.Balance.Should().Be(150.75m);
    }

    [Fact]
    public void Apply_CreditsAndDebits_ComputesNetBalance()
    {
        var balance = new DailyBalance(new DateOnly(2026, 9, 1));

        balance.Apply(EntryType.Credit, 150.75m);
        balance.Apply(EntryType.Debit, 60m);
        balance.Apply(EntryType.Credit, 10m);

        balance.TotalCredits.Should().Be(160.75m);
        balance.TotalDebits.Should().Be(60m);
        balance.Balance.Should().Be(100.75m);
    }

    [Fact]
    public void Apply_CanResultInNegativeBalance()
    {
        var balance = new DailyBalance(new DateOnly(2026, 9, 1));

        balance.Apply(EntryType.Debit, 200m);

        balance.Balance.Should().Be(-200m);
    }

    [Fact]
    public void Apply_UpdatesLastUpdatedAt()
    {
        var balance = new DailyBalance(new DateOnly(2026, 9, 1));
        var before = balance.LastUpdatedAt;

        balance.Apply(EntryType.Credit, 10m);

        balance.LastUpdatedAt.Should().BeOnOrAfter(before);
    }
}
