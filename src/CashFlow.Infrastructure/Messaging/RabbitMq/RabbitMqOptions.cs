namespace CashFlow.Infrastructure.Messaging.RabbitMq;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string Exchange { get; set; } = "cashflow.events";
    public string Queue { get; set; } = "cashflow.consolidation";
    public string DeadLetterExchange { get; set; } = "cashflow.events.dlx";
    public string DeadLetterQueue { get; set; } = "cashflow.consolidation.dlq";

    /// <summary>Mensagens não confirmadas simultaneamente entregues ao consumer (backpressure).</summary>
    public ushort PrefetchCount { get; set; } = 20;

    /// <summary>
    /// Capacidade máxima de processamento concorrente no worker. Acima disso, sob pico
    /// sustentado, novas mensagens são rejeitadas (sem re-enfileirar) e vão para a DLQ em vez de
    /// deixar a fila crescer sem controle — ver docs/architecture.md, seção "Resiliência".
    /// </summary>
    public int MaxConcurrency { get; set; } = 25;
}
