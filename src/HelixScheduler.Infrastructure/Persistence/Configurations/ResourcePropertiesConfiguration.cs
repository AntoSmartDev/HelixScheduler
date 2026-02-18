using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class ResourcePropertiesConfiguration : IEntityTypeConfiguration<ResourceProperties>
{
    public void Configure(EntityTypeBuilder<ResourceProperties> builder)
    {
        builder.ToTable("ResourceProperties");
        builder.HasKey(property => property.Id);
        builder.HasAlternateKey(property => new { property.TenantId, property.Id });

        builder.Property(property => property.Id)
            .ValueGeneratedOnAdd();

        builder.Property(property => property.TenantId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.Property(property => property.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(property => property.Label)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne<Tenants>()
            .WithMany()
            .HasForeignKey(property => property.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(property => new { property.TenantId, property.ParentId });
        builder.HasIndex(property => new { property.TenantId, property.Key });

        builder.HasOne(property => property.Parent)
            .WithMany(parent => parent.Children)
            .HasForeignKey(property => new { property.TenantId, property.ParentId })
            .HasPrincipalKey(parent => new { parent.TenantId, parent.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
