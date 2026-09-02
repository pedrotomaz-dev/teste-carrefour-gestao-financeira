using RabbitMQ.Client;

namespace CashFlow.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Declaração idempotente da topologia (exchange/fila principal + dead-letter). Chamada tanto
/// pelo publisher quanto pelo consumer, então funciona independentemente de qual processo sobe
/// primeiro.
/// </summary>
public static class RabbitMqTopology
{
    public static async Task DeclareAsync(IChannel channel, RabbitMqOptions options, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(options.DeadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(options.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(options.DeadLetterQueue, options.DeadLetterExchange, routingKey: string.Empty, cancellationToken: cancellationToken);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = options.DeadLetterExchange
        };

        await channel.QueueDeclareAsync(options.Queue, durable: true, exclusive: false, autoDelete: false, arguments: queueArguments, cancellationToken: cancellationToken);
    }
}
