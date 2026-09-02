using CashFlow.Domain.Entities;
using CashFlow.Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Application.Interfaces;

/// <summary>Persistência do serviço de Lançamentos (CashFlow.Api).</summary>
public interface IAppDbContext
{
    DbSet<CashEntry> CashEntries { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    /// <summary>
    /// Read model de saldo diário, materializado pelo CashFlow.ConsolidationWorker. A Api só
    /// consulta esta tabela (nunca escreve nela) — exposta aqui apenas para permitir que
    /// `GET /api/saldo-diario` responda mesmo que o worker esteja em outro processo/container.
    /// </summary>
    DbSet<DailyBalance> DailyBalances { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
