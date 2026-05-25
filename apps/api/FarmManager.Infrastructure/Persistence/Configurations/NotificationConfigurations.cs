using FarmManager.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("push_subscriptions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.UserId).HasColumnName("user_id");
        builder.Property(s => s.Endpoint).HasColumnName("endpoint").HasMaxLength(500).IsRequired();
        builder.Property(s => s.P256dh).HasColumnName("p256dh").HasMaxLength(200).IsRequired();
        builder.Property(s => s.Auth).HasColumnName("auth").HasMaxLength(200).IsRequired();
        builder.Property(s => s.UserAgent).HasColumnName("user_agent").HasMaxLength(300);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(s => new { s.UserId, s.Endpoint }).IsUnique();
    }
}

internal sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");
        builder.Property(n => n.OrganisationId).HasColumnName("organisation_id");
        builder.Property(n => n.UserId).HasColumnName("user_id");
        builder.Property(n => n.Channel).HasColumnName("channel").HasConversion<int>().IsRequired();
        builder.Property(n => n.Severity).HasColumnName("severity").HasConversion<int>().IsRequired();
        builder.Property(n => n.Topic).HasColumnName("topic").HasMaxLength(120).IsRequired();
        builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasColumnName("body").IsRequired();
        builder.Property(n => n.PayloadJson).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(n => n.DeliveredAt).HasColumnName("delivered_at");
        builder.Property(n => n.Error).HasColumnName("error");
        builder.Property(n => n.Attempts).HasColumnName("attempts").IsRequired();

        builder.HasIndex(n => new { n.UserId, n.DeliveredAt });
        builder.HasIndex(n => new { n.OrganisationId, n.Topic });
    }
}
