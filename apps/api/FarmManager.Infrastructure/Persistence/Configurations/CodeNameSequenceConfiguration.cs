using FarmManager.Domain.Animals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class CodeNameSequenceConfiguration : IEntityTypeConfiguration<CodeNameSequence>
{
    public void Configure(EntityTypeBuilder<CodeNameSequence> builder)
    {
        builder.ToTable("code_name_sequences");

        builder.HasKey(s => new { s.OrganisationId, s.Prefix, s.Year });

        builder.Property(s => s.OrganisationId).HasColumnName("organisation_id");
        builder.Property(s => s.Prefix).HasColumnName("prefix").HasMaxLength(8).IsRequired();
        builder.Property(s => s.Year).HasColumnName("year").IsRequired();
        builder.Property(s => s.NextSequence).HasColumnName("next_sequence").IsRequired();
    }
}
