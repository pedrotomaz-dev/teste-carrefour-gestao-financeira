using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Outbox;
using CashFlow.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Persistence;

/// <summary>
/// Persistência exclusiva do CashFlow.ConsolidationWorker, usada na topologia de produção
/// (RabbitMQ + processos separados via docker-compose). A interface <see cref="IConsolidationDbContext"/>
/// só expõe as tabelas que este serviço de fato usa — ele nunca escreve em
/// CashEntries/OutboxMessages.
/// </summary>
public class ConsolidationDbContext(DbContextOptions<ConsolidationDbContext> options) : DbContext(options), IConsolidationDbContext
{
    public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DailyBalanceConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedIntegrationEventConfiguration());

        // Api e Worker compartilham o mesmo banco físico (docker-compose) e cada um chama
        // EnsureCreatedAsync ao subir. EnsureCreatedAsync só verifica se ALGUMA tabela já existe
        // (não se TODAS as do modelo atual existem) para decidir se cria o schema. Como o modelo
        // da Api é um superconjunto deste (inclui CashEntries/OutboxMessages), se o Worker vencer
        // a corrida e criar suas tabelas primeiro, a Api depois veria "já existe tabela" e
        // deixaria de criar as suas — silenciosamente, sem lançar exceção. Mapear as mesmas
        // tabelas extras aqui (sem expor DbSet público — o Worker nunca as usa) garante que quem
        // vencer a corrida sempre cria o schema completo, então o outro sempre encontra tudo.
        modelBuilder.ApplyConfiguration(new CashEntryConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
