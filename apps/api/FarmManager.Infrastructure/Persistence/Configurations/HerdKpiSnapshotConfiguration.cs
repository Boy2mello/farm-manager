using FarmManager.Domain.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class HerdKpiSnapshotConfiguration : IEntityTypeConfiguration<HerdKpiSnapshot>
{
    public void Configure(EntityTypeBuilder<HerdKpiSnapshot> builder)
    {
        builder.ToTable("herd_kpi_snapshots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.OrganisationId).HasColumnName("organisation_id");
        builder.Property(s => s.FarmId).HasColumnName("farm_id");
        builder.Property(s => s.AsOfDate).HasColumnName("as_of_date").IsRequired();
        builder.Property(s => s.MetricName).HasColumnName("metric_name").HasMaxLength(80).IsRequired();
        builder.Property(s => s.Value).HasColumnName("value").HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(s => s.DeltaVsLastMonth).HasColumnName("delta_month").HasColumnType("numeric(18,4)");
        builder.Property(s => s.DeltaVsLastYear).HasColumnName("delta_year").HasColumnType("numeric(18,4)");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(s => new { s.OrganisationId, s.AsOfDate, s.MetricName }).IsUnique();
    }
}
