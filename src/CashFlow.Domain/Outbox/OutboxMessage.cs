using CashFlow.Domain.Common;

namespace CashFlow.Domain.Outbox;

/// <summary>
/// Implementa o Transactional Outbox Pattern: a mensagem é gravada na mesma transação do
/// agregado que a originou, garantindo que o evento nunca se perca mesmo que o broker de
/// mensageria (RabbitMQ) esteja indisponível no momento da escrita. Um despachante em
/// background (OutboxDispatcher) publica as mensagens pendentes de forma assíncrona e com
/// retry — a API de lançamentos nunca depende da disponibilidade do broker para responder.
/// </summary>
public class OutboxMessage : EntityBase
{
    public string Type { get; private set; }
    public string Content { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    // EF Core constructor
    private OutboxMessage()
    {
        Type = string.Empty;
        Content = string.Empty;
    }

    public OutboxMessage(string type, string content)
    {
        Type = type;
        Content = content;
    }

    public bool IsProcessed => ProcessedAt is not null;

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public void RegisterFailure(string error)
    {
        Attempts++;
        LastError = error;
    }
}
