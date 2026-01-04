using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class BusyEventResourcesConfiguration : IEntityTypeConfiguration<BusyEventResources>
{
    public void Configure(EntityTypeBuilder<BusyEventResources> builder)
    {
        builder.ToTable("BusyEventResources");
        builder.HasKey(link => new { link.BusyEventId, link.ResourceId });

        builder.HasOne(link => link.BusyEvent)
            .WithMany(busyEvent => busyEvent.BusyEventResources)
            .HasForeignKey(link => link.BusyEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Resource)
            .WithMany(resource => resource.BusyEventResources)
            .HasForeignKey(link => link.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => new { link.ResourceId, link.BusyEventId });
    }
}
