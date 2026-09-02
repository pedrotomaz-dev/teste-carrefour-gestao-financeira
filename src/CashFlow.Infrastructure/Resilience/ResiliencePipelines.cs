using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace CashFlow.Infrastructure.Resilience;

/// <summary>
/// Políticas Polly compartilhadas. Retry com backoff exponencial para falhas transitórias
/// (broker/DB momentaneamente fora do ar) + Circuit Breaker para parar de bater insistentemente
/// num serviço já sabidamente indisponível, dando a ele tempo para se recuperar.
/// </summary>
public static class ResiliencePipelines
{
    public static ResiliencePipeline CreatePublisherPipeline(ILogger logger)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(200),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "Falha ao publicar evento de integração (tentativa {Attempt}). Nova tentativa em {Delay}.",
                        args.AttemptNumber + 1, args.RetryDelay);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15),
                OnOpened = args =>
                {
                    logger.LogError(
                        "Circuit breaker aberto para o publisher de mensageria por {BreakDuration} — " +
                        "o outbox continuará acumulando mensagens pendentes com segurança até o broker voltar.",
                        args.BreakDuration);
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    logger.LogInformation("Circuit breaker do publisher de mensageria fechado novamente.");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
