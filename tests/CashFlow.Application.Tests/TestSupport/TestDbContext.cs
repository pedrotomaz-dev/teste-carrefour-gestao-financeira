using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Application.Tests.TestSupport;

/// <summary>
/// DbContext mínimo (EF Core InMemory) usado apenas nos testes de Application: implementa as
/// mesmas portas (IAppDbContext/IConsolidationDbContext) que a Infrastructure implementa em
/// produção, sem que os testes de Application precisem referenciar a Infrastructure.
/// </summary>
public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options), IAppDbContext, IConsolidationDbContext
{
    public DbSet<CashEntry> CashEntries => Set<CashEntry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyBalance>().HasIndex(b => b.Date).IsUnique();
        modelBuilder.Entity<ProcessedIntegrationEvent>().HasKey(p => p.EventId);
    }

    public static TestDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }
}
