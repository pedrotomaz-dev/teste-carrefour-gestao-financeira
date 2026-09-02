using System.Text;
using CashFlow.Application.Interfaces;
using CashFlow.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;

namespace CashFlow.Infrastructure.Messaging.RabbitMq;

public class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly ResiliencePipeline _pipeline;

    public RabbitMqIntegrationEventPublisher(
        RabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqIntegrationEventPublisher> logger)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
        _pipeline = ResiliencePipelines.CreatePublisherPipeline(logger);
    }

    public async Task PublishAsync(string type, string content, CancellationToken cancellationToken = default)
    {
        await _pipeline.ExecuteAsync(async ct =>
        {
            var connection = await _connectionProvider.GetConnectionAsync(ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

            await RabbitMqTopology.DeclareAsync(channel, _options, ct);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = type,
                MessageId = Guid.NewGuid().ToString()
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _options.Queue,
                mandatory: false,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(content),
                cancellationToken: ct);
        }, cancellationToken);
    }
}
