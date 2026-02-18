using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class TenantsConfiguration : IEntityTypeConfiguration<Tenants>
{
    public void Configure(EntityTypeBuilder<Tenants> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(tenant => tenant.Id);

        builder.Property(tenant => tenant.Id)
            .ValueGeneratedNever();

        builder.Property(tenant => tenant.Key)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(tenant => tenant.Label)
            .HasMaxLength(128);

        builder.Property(tenant => tenant.CreatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(tenant => tenant.Key)
            .IsUnique();
    }
}
