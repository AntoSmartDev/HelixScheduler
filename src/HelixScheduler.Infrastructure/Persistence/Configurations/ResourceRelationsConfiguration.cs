using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class ResourceRelationsConfiguration : IEntityTypeConfiguration<ResourceRelations>
{
    public void Configure(EntityTypeBuilder<ResourceRelations> builder)
    {
        builder.ToTable("ResourceRelations");
        builder.HasKey(relation => new
        {
            relation.TenantId,
            relation.ParentResourceId,
            relation.ChildResourceId,
            relation.RelationType
        });

        builder.Property(relation => relation.TenantId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.Property(relation => relation.RelationType)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(relation => relation.ParentResource)
            .WithMany(resource => resource.ParentRelations)
            .HasForeignKey(relation => new { relation.TenantId, relation.ParentResourceId })
            .HasPrincipalKey(resource => new { resource.TenantId, resource.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(relation => relation.ChildResource)
            .WithMany(resource => resource.ChildRelations)
            .HasForeignKey(relation => new { relation.TenantId, relation.ChildResourceId })
            .HasPrincipalKey(resource => new { resource.TenantId, resource.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(relation => new { relation.TenantId, relation.ChildResourceId });
    }
}
