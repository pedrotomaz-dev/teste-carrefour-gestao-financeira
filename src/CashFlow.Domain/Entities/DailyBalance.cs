using CashFlow.Domain.Common;
using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Entities;

/// <summary>
/// Read model consolidado e mantido pelo CashFlow.ConsolidationWorker. Cada linha representa
/// o saldo já calculado de um dia, permitindo que a consulta de saldo seja O(1) em vez de
/// recalcular todos os lançamentos a cada requisição.
/// </summary>
public class DailyBalance : EntityBase
{
    public DateOnly Date { get; private set; }
    public decimal TotalCredits { get; private set; }
    public decimal TotalDebits { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    // EF Core constructor
    private DailyBalance() { }

    public DailyBalance(DateOnly date)
    {
        Date = date;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void Apply(EntryType type, decimal amount)
    {
        if (type == EntryType.Credit)
        {
            TotalCredits += amount;
        }
        else
        {
            TotalDebits += amount;
        }

        Balance = TotalCredits - TotalDebits;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
