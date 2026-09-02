using CashFlow.Application.Interfaces;
using FluentValidation;

namespace CashFlow.Application.CashEntries.Commands.RegisterCashEntry;

public class RegisterCashEntryCommandValidator : AbstractValidator<RegisterCashEntryCommand>
{
    public RegisterCashEntryCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição do lançamento é obrigatória.")
            .MaximumLength(200);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor do lançamento deve ser maior que zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo de lançamento inválido.");

        RuleFor(x => x.OccurredOn)
            .LessThanOrEqualTo(dateTimeProvider.Today).WithMessage("A data do lançamento não pode ser no futuro.")
            .When(x => x.OccurredOn.HasValue);
    }
}
