using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Infrastructure.Persistence.Configurations;

public class CashEntryConfiguration : IEntityTypeConfiguration<CashEntry>
{
    public void Configure(EntityTypeBuilder<CashEntry> builder)
    {
        builder.ToTable("CashEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.OccurredOn).IsRequired();

        builder.HasIndex(e => e.OccurredOn);
    }
}
