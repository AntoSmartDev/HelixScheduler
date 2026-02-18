using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class ResourceTypesConfiguration : IEntityTypeConfiguration<ResourceTypes>
{
    public void Configure(EntityTypeBuilder<ResourceTypes> builder)
    {
        builder.ToTable("ResourceTypes");
        builder.HasKey(type => type.Id);
        builder.HasAlternateKey(type => new { type.TenantId, type.Id });

        builder.Property(type => type.TenantId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.Property(type => type.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(type => type.Label)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(type => type.SortOrder);

        builder.HasOne<Tenants>()
            .WithMany()
            .HasForeignKey(type => type.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(type => new { type.TenantId, type.Key })
            .IsUnique();
    }
}
