using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class BusyEventResourcesConfiguration : IEntityTypeConfiguration<BusyEventResources>
{
    public void Configure(EntityTypeBuilder<BusyEventResources> builder)
    {
        builder.ToTable("BusyEventResources");
        builder.HasKey(link => new { link.TenantId, link.BusyEventId, link.ResourceId });

        builder.Property(link => link.TenantId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.HasOne(link => link.BusyEvent)
            .WithMany(busyEvent => busyEvent.BusyEventResources)
            .HasForeignKey(link => new { link.TenantId, link.BusyEventId })
            .HasPrincipalKey(busyEvent => new { busyEvent.TenantId, busyEvent.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Resource)
            .WithMany(resource => resource.BusyEventResources)
            .HasForeignKey(link => new { link.TenantId, link.ResourceId })
            .HasPrincipalKey(resource => new { resource.TenantId, resource.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => new { link.TenantId, link.ResourceId, link.BusyEventId });
    }
}
