using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class ResourceTypePropertiesConfiguration : IEntityTypeConfiguration<ResourceTypeProperties>
{
    public void Configure(EntityTypeBuilder<ResourceTypeProperties> builder)
    {
        builder.ToTable("ResourceTypeProperties");
        builder.HasKey(link => new { link.ResourceTypeId, link.PropertyDefinitionId });

        builder.HasOne(link => link.ResourceType)
            .WithMany(type => type.ResourceTypeProperties)
            .HasForeignKey(link => link.ResourceTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.PropertyDefinition)
            .WithMany()
            .HasForeignKey(link => link.PropertyDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => new { link.PropertyDefinitionId, link.ResourceTypeId });
    }
}
