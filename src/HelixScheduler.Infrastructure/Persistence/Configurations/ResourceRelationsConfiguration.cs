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
            relation.ParentResourceId,
            relation.ChildResourceId,
            relation.RelationType
        });

        builder.Property(relation => relation.RelationType)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(relation => relation.ParentResource)
            .WithMany(resource => resource.ParentRelations)
            .HasForeignKey(relation => relation.ParentResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(relation => relation.ChildResource)
            .WithMany(resource => resource.ChildRelations)
            .HasForeignKey(relation => relation.ChildResourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
