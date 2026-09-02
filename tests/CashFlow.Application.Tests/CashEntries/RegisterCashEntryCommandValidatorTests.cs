using CashFlow.Application.CashEntries.Commands.RegisterCashEntry;
using CashFlow.Application.Tests.TestSupport;
using CashFlow.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CashFlow.Application.Tests.CashEntries;

public class RegisterCashEntryCommandValidatorTests
{
    private readonly RegisterCashEntryCommandValidator _validator = new(new FixedDateTimeProvider(new DateTime(2026, 9, 1)));

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var command = new RegisterCashEntryCommand("Venda", 10, EntryType.Credit, new DateOnly(2026, 8, 30));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyDescription_HasError()
    {
        var command = new RegisterCashEntryCommand(string.Empty, 10, EntryType.Credit, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_NonPositiveAmount_HasError()
    {
        var command = new RegisterCashEntryCommand("Venda", 0, EntryType.Credit, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_FutureOccurredOn_HasError()
    {
        var command = new RegisterCashEntryCommand("Venda", 10, EntryType.Credit, new DateOnly(2026, 9, 2));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.OccurredOn);
    }
}
