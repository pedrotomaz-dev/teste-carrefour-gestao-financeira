using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CashFlow.Domain.Tests.Entities;

public class CashEntryTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesEntry()
    {
        var occurredOn = new DateOnly(2026, 9, 1);

        var entry = new CashEntry("Venda no cartão", 150.75m, EntryType.Credit, occurredOn);

        entry.Description.Should().Be("Venda no cartão");
        entry.Amount.Should().Be(150.75m);
        entry.Type.Should().Be(EntryType.Credit);
        entry.OccurredOn.Should().Be(occurredOn);
        entry.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Constructor_WithNonPositiveAmount_ThrowsDomainException(decimal amount)
    {
        var act = () => new CashEntry("Descrição válida", amount, EntryType.Debit, DateOnly.FromDateTime(DateTime.UtcNow));

        act.Should().Throw<DomainException>().WithMessage("*maior que zero*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithoutDescription_ThrowsDomainException(string description)
    {
        var act = () => new CashEntry(description, 10, EntryType.Credit, DateOnly.FromDateTime(DateTime.UtcNow));

        act.Should().Throw<DomainException>().WithMessage("*descrição*");
    }

    [Fact]
    public void Constructor_TrimsDescription()
    {
        var entry = new CashEntry("  Aluguel  ", 500, EntryType.Debit, DateOnly.FromDateTime(DateTime.UtcNow));

        entry.Description.Should().Be("Aluguel");
    }

    [Fact]
    public void SignedAmount_ForCredit_IsPositive()
    {
        var entry = new CashEntry("Venda", 100, EntryType.Credit, DateOnly.FromDateTime(DateTime.UtcNow));

        entry.SignedAmount.Should().Be(100);
    }

    [Fact]
    public void SignedAmount_ForDebit_IsNegative()
    {
        var entry = new CashEntry("Compra", 100, EntryType.Debit, DateOnly.FromDateTime(DateTime.UtcNow));

        entry.SignedAmount.Should().Be(-100);
    }
}
