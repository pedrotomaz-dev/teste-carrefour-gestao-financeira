using System.Threading.Channels;

namespace CashFlow.Infrastructure.Messaging.InMemory;

public record InMemoryEnvelope(string Type, string Content);

/// <summary>
/// Fila em memória (Channel) usada apenas na topologia "single-process" (sem Docker/RabbitMQ),
/// pensada para facilitar rodar e avaliar o desafio localmente com `dotnet run`. Não sobrevive a
/// um restart do processo — para a garantia real de resiliência entre processos, ver o modo
/// RabbitMq (docker-compose).
/// </summary>
public class InMemoryMessageBus
{
    private readonly Channel<InMemoryEnvelope> _channel = Channel.CreateUnbounded<InMemoryEnvelope>();

    public ChannelWriter<InMemoryEnvelope> Writer => _channel.Writer;
    public ChannelReader<InMemoryEnvelope> Reader => _channel.Reader;
}
