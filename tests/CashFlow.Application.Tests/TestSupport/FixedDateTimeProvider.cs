using CashFlow.Application.Interfaces;

namespace CashFlow.Application.Tests.TestSupport;

public class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; } = utcNow;
    public DateOnly Today { get; } = DateOnly.FromDateTime(utcNow);
}
