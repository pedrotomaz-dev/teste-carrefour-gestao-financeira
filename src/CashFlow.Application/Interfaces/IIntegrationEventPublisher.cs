namespace CashFlow.Application.Interfaces;

/// <summary>
/// Porta para publicação de eventos de integração já persistidos no Outbox. Duas implementações
/// na Infrastructure: RabbitMQ (produção, via docker-compose) e um canal em memória (para rodar
/// localmente sem subir um broker) — trocadas por configuração (Strategy Pattern).
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync(string type, string content, CancellationToken cancellationToken = default);
}
