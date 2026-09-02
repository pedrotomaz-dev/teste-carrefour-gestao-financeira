namespace CashFlow.Infrastructure.Outbox;

public class OutboxDispatcherOptions
{
    public const string SectionName = "OutboxDispatcher";

    public int PollingIntervalSeconds { get; set; } = 2;
    public int BatchSize { get; set; } = 20;
}
