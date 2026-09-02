using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Persistence;

/// <summary>
/// SQLite só permite um único escritor por vez; no modo "single-process" a mesma base é acessada
/// concorrentemente pelas requisições HTTP, pelo OutboxDispatcher e pelo consumer de
/// consolidação. Sem WAL + busy_timeout, gravações concorrentes falham com "database is locked"
/// em vez de esperar a vez — isso não afeta o modo Postgres/produção (docker-compose).
/// </summary>
public static class SqlitePragmaConfigurator
{
    public static async Task ApplyAsync(DbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsSqlite())
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;", cancellationToken);
    }
}
