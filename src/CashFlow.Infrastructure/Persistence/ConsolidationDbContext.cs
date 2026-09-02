using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Outbox;
using CashFlow.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Persistence;

/// <summary>
/// Persistência exclusiva do CashFlow.ConsolidationWorker, usada na topologia de produção
/// (RabbitMQ + processos separados via docker-compose). Mapeia apenas as tabelas que este
/// serviço possui — ele nunca escreve em CashEntries/OutboxMessages.
/// </summary>
public class ConsolidationDbContext(DbContextOptions<ConsolidationDbContext> options) : DbContext(options), IConsolidationDbContext
{
    public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DailyBalanceConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedIntegrationEventConfiguration());
    }
}
