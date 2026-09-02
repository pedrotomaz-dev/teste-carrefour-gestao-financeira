namespace CashFlow.Infrastructure.Messaging;

public class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>"InMemory" (padrão, sem dependências externas) ou "RabbitMq".</summary>
    public string Provider { get; set; } = "InMemory";
}
