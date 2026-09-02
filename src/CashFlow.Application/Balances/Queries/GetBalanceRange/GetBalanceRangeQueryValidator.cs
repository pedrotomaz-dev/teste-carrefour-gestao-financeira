using FluentValidation;

namespace CashFlow.Application.Balances.Queries.GetBalanceRange;

public class GetBalanceRangeQueryValidator : AbstractValidator<GetBalanceRangeQuery>
{
    public GetBalanceRangeQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("A data final deve ser maior ou igual à data inicial.");

        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber <= 366)
            .WithMessage("O período consultado não pode exceder 366 dias.");
    }
}
