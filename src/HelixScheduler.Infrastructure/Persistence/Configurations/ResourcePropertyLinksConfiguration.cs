using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class ResourcePropertyLinksConfiguration : IEntityTypeConfiguration<ResourcePropertyLinks>
{
    public void Configure(EntityTypeBuilder<ResourcePropertyLinks> builder)
    {
        builder.ToTable("ResourcePropertyLinks");
        builder.HasKey(link => new { link.TenantId, link.ResourceId, link.PropertyId });

        builder.Property(link => link.TenantId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.HasOne(link => link.Resource)
            .WithMany(resource => resource.PropertyLinks)
            .HasForeignKey(link => new { link.TenantId, link.ResourceId })
            .HasPrincipalKey(resource => new { resource.TenantId, resource.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Property)
            .WithMany(property => property.PropertyLinks)
            .HasForeignKey(link => new { link.TenantId, link.PropertyId })
            .HasPrincipalKey(property => new { property.TenantId, property.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => new { link.TenantId, link.PropertyId, link.ResourceId });
    }
}
