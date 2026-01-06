using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class ResourcePropertyLinksConfiguration : IEntityTypeConfiguration<ResourcePropertyLinks>
{
    public void Configure(EntityTypeBuilder<ResourcePropertyLinks> builder)
    {
        builder.ToTable("ResourcePropertyLinks");
        builder.HasKey(link => new { link.ResourceId, link.PropertyId });

        builder.HasOne(link => link.Resource)
            .WithMany(resource => resource.PropertyLinks)
            .HasForeignKey(link => link.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Property)
            .WithMany(property => property.PropertyLinks)
            .HasForeignKey(link => link.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => new { link.PropertyId, link.ResourceId });
    }
}
