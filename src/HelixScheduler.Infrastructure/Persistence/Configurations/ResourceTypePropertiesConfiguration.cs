using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class ResourceTypePropertiesConfiguration : IEntityTypeConfiguration<ResourceTypeProperties>
{
    public void Configure(EntityTypeBuilder<ResourceTypeProperties> builder)
    {
        builder.ToTable("ResourceTypeProperties");
        builder.HasKey(link => new { link.TenantId, link.ResourceTypeId, link.PropertyDefinitionId });

        builder.Property(link => link.TenantId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.HasOne(link => link.ResourceType)
            .WithMany(type => type.ResourceTypeProperties)
            .HasForeignKey(link => new { link.TenantId, link.ResourceTypeId })
            .HasPrincipalKey(type => new { type.TenantId, type.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.PropertyDefinition)
            .WithMany()
            .HasForeignKey(link => new { link.TenantId, link.PropertyDefinitionId })
            .HasPrincipalKey(property => new { property.TenantId, property.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => new { link.TenantId, link.PropertyDefinitionId, link.ResourceTypeId });
    }
}
