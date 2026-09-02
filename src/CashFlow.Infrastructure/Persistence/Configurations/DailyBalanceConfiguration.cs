using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Infrastructure.Persistence.Configurations;

public class DailyBalanceConfiguration : IEntityTypeConfiguration<DailyBalance>
{
    public void Configure(EntityTypeBuilder<DailyBalance> builder)
    {
        builder.ToTable("DailyBalances");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.TotalCredits).HasPrecision(18, 2);
        builder.Property(b => b.TotalDebits).HasPrecision(18, 2);
        builder.Property(b => b.Balance).HasPrecision(18, 2);

        builder.HasIndex(b => b.Date).IsUnique();
    }
}
