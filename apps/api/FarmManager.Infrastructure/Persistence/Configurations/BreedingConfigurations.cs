using FarmManager.Domain.Breeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class CalvingEventConfiguration : IEntityTypeConfiguration<CalvingEvent>
{
    public void Configure(EntityTypeBuilder<CalvingEvent> builder)
    {
        builder.ToTable("calving_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.OrganisationId).HasColumnName("organisation_id");
        builder.Property(e => e.DamId).HasColumnName("dam_id");
        builder.Property(e => e.CalfId).HasColumnName("calf_id");
        builder.Property(e => e.SireId).HasColumnName("sire_id");
        builder.Property(e => e.SireExternalNote).HasColumnName("sire_external_note").HasMaxLength(200);
        builder.Property(e => e.CalvingDate).HasColumnName("calving_date").IsRequired();
        builder.Property(e => e.DifficultyScore).HasColumnName("difficulty_score").IsRequired();
        builder.Property(e => e.AssistanceRequired).HasColumnName("assistance_required").IsRequired();
        builder.Property(e => e.PlacentaDelivered).HasColumnName("placenta_delivered").IsRequired();
        builder.Property(e => e.MotheringAbility).HasColumnName("mothering_ability");
        builder.Property(e => e.Stillbirth).HasColumnName("stillbirth").IsRequired();
        builder.Property(e => e.CalfWeightKg).HasColumnName("calf_weight_kg").HasColumnType("numeric(6,2)");
        builder.Property(e => e.CalfVigour).HasColumnName("calf_vigour");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.AttachmentsJson).HasColumnName("attachments").HasColumnType("jsonb");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => e.DamId);
        builder.HasIndex(e => e.CalvingDate);
    }
}

internal sealed class ServiceEventConfiguration : IEntityTypeConfiguration<ServiceEvent>
{
    public void Configure(EntityTypeBuilder<ServiceEvent> builder)
    {
        builder.ToTable("service_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.OrganisationId).HasColumnName("organisation_id");
        builder.Property(e => e.CowId).HasColumnName("cow_id");
        builder.Property(e => e.BullId).HasColumnName("bull_id");
        builder.Property(e => e.AiStrawId).HasColumnName("ai_straw_id");
        builder.Property(e => e.ServiceDate).HasColumnName("service_date").IsRequired();
        builder.Property(e => e.ServiceType).HasColumnName("service_type").HasConversion<int>().IsRequired();
        builder.Property(e => e.ExpectedCalvingDate).HasColumnName("expected_calving_date").IsRequired();
        builder.Property(e => e.InbreedingCoefficient).HasColumnName("inbreeding_coefficient").HasColumnType("numeric(8,6)");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => e.CowId);
        builder.HasIndex(e => e.BullId);
        builder.HasIndex(e => e.ExpectedCalvingDate);
    }
}

internal sealed class PregnancyCheckEventConfiguration : IEntityTypeConfiguration<PregnancyCheckEvent>
{
    public void Configure(EntityTypeBuilder<PregnancyCheckEvent> builder)
    {
        builder.ToTable("pregnancy_check_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.OrganisationId).HasColumnName("organisation_id");
        builder.Property(e => e.CowId).HasColumnName("cow_id");
        builder.Property(e => e.CheckDate).HasColumnName("check_date").IsRequired();
        builder.Property(e => e.Method).HasColumnName("method").HasConversion<int>().IsRequired();
        builder.Property(e => e.Result).HasColumnName("result").HasConversion<int>().IsRequired();
        builder.Property(e => e.DaysBred).HasColumnName("days_bred");
        builder.Property(e => e.VetUserId).HasColumnName("vet_user_id");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => e.CowId);
        builder.HasIndex(e => e.CheckDate);
    }
}
