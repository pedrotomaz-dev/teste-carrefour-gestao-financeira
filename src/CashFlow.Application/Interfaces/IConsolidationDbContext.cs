using CashFlow.Domain.Entities;
using CashFlow.Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Application.Interfaces;

/// <summary>Persistência do serviço de Consolidação (CashFlow.ConsolidationWorker).</summary>
public interface IConsolidationDbContext
{
    DbSet<DailyBalance> DailyBalances { get; }
    DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
