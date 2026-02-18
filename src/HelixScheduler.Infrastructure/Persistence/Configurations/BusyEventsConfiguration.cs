using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class BusyEventsConfiguration : IEntityTypeConfiguration<BusyEvents>
{
    public void Configure(EntityTypeBuilder<BusyEvents> builder)
    {
        builder.ToTable("BusyEvents");
        builder.HasKey(busyEvent => busyEvent.Id);
        builder.HasAlternateKey(busyEvent => new { busyEvent.TenantId, busyEvent.Id });

        builder.Property(busyEvent => busyEvent.Id)
            .ValueGeneratedOnAdd();

        builder.Property(busyEvent => busyEvent.TenantId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.Property(busyEvent => busyEvent.Title)
            .HasMaxLength(300);

        builder.Property(busyEvent => busyEvent.StartUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(busyEvent => busyEvent.EndUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(busyEvent => busyEvent.EventType)
            .HasMaxLength(50);

        builder.Property(busyEvent => busyEvent.CreatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne<Tenants>()
            .WithMany()
            .HasForeignKey(busyEvent => busyEvent.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(busyEvent => new { busyEvent.TenantId, busyEvent.StartUtc, busyEvent.EndUtc });
    }
}
