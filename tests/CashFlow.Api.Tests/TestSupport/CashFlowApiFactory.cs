using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CashFlow.Api.Tests.TestSupport;

/// <summary>
/// Sobe a Api inteira em memória (WebApplicationFactory) com um banco Sqlite isolado por
/// instância de fábrica (um arquivo temporário por classe de teste) e o outbox despachando a
/// cada 200ms, para os testes de integração não precisarem esperar o intervalo de produção (2s).
/// </summary>
public class CashFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbFilePath = Path.Combine(Path.GetTempPath(), $"cashflow-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sqlite"] = $"Data Source={_dbFilePath}",
                ["Database:Provider"] = "Sqlite",
                ["Messaging:Provider"] = "InMemory",
                ["OutboxDispatcher:PollingIntervalSeconds"] = "1"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // O pool de conexões do Microsoft.Data.Sqlite mantém o arquivo aberto para reaproveitar
        // conexões; sem limpar o pool, o delete abaixo falha com IOException.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        foreach (var path in new[] { _dbFilePath, $"{_dbFilePath}-wal", $"{_dbFilePath}-shm" })
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // Arquivo temporário de teste — melhor esforço, o SO limpa a pasta temp eventualmente.
            }
        }
    }
}
