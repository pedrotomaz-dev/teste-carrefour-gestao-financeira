using CashFlow.Domain.Common;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Exceptions;

namespace CashFlow.Domain.Entities;

public class CashEntry : EntityBase
{
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public EntryType Type { get; private set; }
    public DateOnly OccurredOn { get; private set; }

    // EF Core constructor
    private CashEntry()
    {
        Description = string.Empty;
    }

    public CashEntry(string description, decimal amount, EntryType type, DateOnly occurredOn)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("A descrição do lançamento é obrigatória.");
        }

        if (amount <= 0)
        {
            throw new DomainException("O valor do lançamento deve ser maior que zero.");
        }

        Description = description.Trim();
        Amount = amount;
        Type = type;
        OccurredOn = occurredOn;
    }

    /// <summary>Valor com sinal aplicado (crédito positivo, débito negativo) — usado nos totais de saldo.</summary>
    public decimal SignedAmount => Type == EntryType.Credit ? Amount : -Amount;
}
