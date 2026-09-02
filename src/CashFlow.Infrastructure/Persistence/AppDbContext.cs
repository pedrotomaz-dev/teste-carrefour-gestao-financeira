using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Outbox;
using CashFlow.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Persistence;

/// <summary>
/// Persistência do CashFlow.Api. Implementa <see cref="IConsolidationDbContext"/> também porque,
/// no modo "InMemory" (execução local sem Docker/RabbitMQ), o próprio processo da Api hospeda o
/// consumidor de consolidação — ver <c>docs/architecture.md</c> para os dois topologias de
/// implantação suportadas.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext, IConsolidationDbContext
{
    public DbSet<CashEntry> CashEntries => Set<CashEntry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CashEntryConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DailyBalanceConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedIntegrationEventConfiguration());
    }
}
