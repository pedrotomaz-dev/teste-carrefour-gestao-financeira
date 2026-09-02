using CashFlow.Application.Balances.Queries.GetBalanceRange;
using FluentValidation.TestHelper;
using Xunit;

namespace CashFlow.Application.Tests.Balances;

public class GetBalanceRangeQueryValidatorTests
{
    private readonly GetBalanceRangeQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidRange_HasNoErrors()
    {
        var query = new GetBalanceRangeQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ToBeforeFrom_HasError()
    {
        var query = new GetBalanceRangeQuery(new DateOnly(2026, 8, 31), new DateOnly(2026, 8, 1));

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.To);
    }

    [Fact]
    public void Validate_RangeLongerThanAYear_HasError()
    {
        var query = new GetBalanceRangeQuery(new DateOnly(2020, 1, 1), new DateOnly(2026, 1, 1));

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x);
    }
}
