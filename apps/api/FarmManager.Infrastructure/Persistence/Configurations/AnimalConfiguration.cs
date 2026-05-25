using FarmManager.Domain.Animals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("animals");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.OrganisationId).HasColumnName("organisation_id");
        builder.Property(a => a.FarmId).HasColumnName("farm_id");

        builder.Property(a => a.CodeName).HasColumnName("code_name").HasMaxLength(32).IsRequired();
        builder.Property(a => a.CodeNamePrefix).HasColumnName("code_name_prefix").HasMaxLength(8).IsRequired();
        builder.Property(a => a.CodeNameYear).HasColumnName("code_name_year").IsRequired();
        builder.Property(a => a.CodeNameSequence).HasColumnName("code_name_sequence").IsRequired();

        builder.Property(a => a.PrimaryName).HasColumnName("primary_name").HasMaxLength(120);
        builder.Property<List<string>>("_aliases")
            .HasColumnName("aliases")
            .HasField("_aliases")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnType("text[]");

        builder.Property(a => a.ExternalTag).HasColumnName("external_tag").HasMaxLength(32);
        builder.Property(a => a.Rfid).HasColumnName("rfid").HasMaxLength(32);

        builder.Property(a => a.Sex).HasColumnName("sex").HasConversion<int>().IsRequired();
        builder.Property(a => a.BreedId).HasColumnName("breed_id");
        builder.Property(a => a.BreedCompositionJson).HasColumnName("breed_composition").HasColumnType("jsonb");

        builder.Property(a => a.Dob).HasColumnName("dob").IsRequired();
        builder.Property(a => a.DobPrecision).HasColumnName("dob_precision").HasConversion<int>().IsRequired();

        builder.Property(a => a.SireId).HasColumnName("sire_id");
        builder.Property(a => a.DamId).HasColumnName("dam_id");

        builder.Property(a => a.Source).HasColumnName("source").HasConversion<int>().IsRequired();
        builder.Property(a => a.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(a => a.DisposalDate).HasColumnName("disposal_date");
        builder.Property(a => a.DisposalReason).HasColumnName("disposal_reason").HasMaxLength(200);

        builder.Property(a => a.LocationId).HasColumnName("location_id");

        builder.Property<List<string>>("_photoUrls")
            .HasColumnName("photo_urls")
            .HasField("_photoUrls")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnType("text[]");

        builder.Property(a => a.IsBSired).HasColumnName("is_b_sired").IsRequired();
        builder.Property(a => a.WithdrawalUntil).HasColumnName("withdrawal_until");
        builder.Property(a => a.PerformanceTier).HasColumnName("performance_tier").HasConversion<int>().IsRequired();

        builder.Property(a => a.CalfCount).HasColumnName("calf_count").IsRequired();
        builder.Property(a => a.CalvesAlive).HasColumnName("calves_alive").IsRequired();
        builder.Property(a => a.LastCalvingDate).HasColumnName("last_calving_date");
        builder.Property(a => a.AvgCalvingIntervalDays).HasColumnName("avg_calving_interval_days").HasColumnType("numeric(10,2)");
        builder.Property(a => a.CalvesPerYear).HasColumnName("calves_per_year").HasColumnType("numeric(8,4)");

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(a => a.DomainEvents);
        builder.Ignore(a => a.Aliases);
        builder.Ignore(a => a.PhotoUrls);

        builder.HasIndex(a => new { a.OrganisationId, a.CodeName }).IsUnique();
        builder.HasIndex(a => new { a.OrganisationId, a.Status });
        builder.HasIndex(a => a.DamId);
        builder.HasIndex(a => a.SireId);
    }
}
