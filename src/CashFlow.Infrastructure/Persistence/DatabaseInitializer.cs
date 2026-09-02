using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.Infrastructure.Persistence;

/// <summary>
/// Api e Worker sobem como processos separados mas apontam para o mesmo banco físico
/// (docker-compose simplificado — ver docs/architecture.md). Como os dois chamam
/// EnsureCreatedAsync ao subir, existe uma janela real de corrida: ambos podem checar "o schema
/// existe?" ao mesmo tempo, ver que não, e tentar criar as mesmas tabelas — o "perdedor" recebe
/// um erro de "relation already exists" do Postgres. Como EnsureCreatedAsync não passa a ser
/// destrutivo ao ser chamado de novo (na segunda tentativa ele só constata que as tabelas já
/// existem), um retry simples resolve sem precisar de um lock distribuído.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(DbContext dbContext, ILogger logger, CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                await SqlitePragmaConfigurator.ApplyAsync(dbContext, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Falha ao inicializar o schema do banco (tentativa {Attempt}/{MaxAttempts}) — provável corrida com outro " +
                    "serviço criando o schema ao mesmo tempo. Tentando novamente.",
                    attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
