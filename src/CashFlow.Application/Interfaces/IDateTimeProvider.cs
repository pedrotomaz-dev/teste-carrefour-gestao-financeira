namespace CashFlow.Application.Interfaces;

/// <summary>Abstrai a data/hora atual para manter os handlers determinísticos e testáveis.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
