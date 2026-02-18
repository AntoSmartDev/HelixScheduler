using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class RuleResourcesConfiguration : IEntityTypeConfiguration<RuleResources>
{
    public void Configure(EntityTypeBuilder<RuleResources> builder)
    {
        builder.ToTable("RuleResources");
        builder.HasKey(link => new { link.TenantId, link.RuleId, link.ResourceId });

        builder.Property(link => link.TenantId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        builder.HasOne(link => link.Rule)
            .WithMany(rule => rule.RuleResources)
            .HasForeignKey(link => new { link.TenantId, link.RuleId })
            .HasPrincipalKey(rule => new { rule.TenantId, rule.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Resource)
            .WithMany(resource => resource.RuleResources)
            .HasForeignKey(link => new { link.TenantId, link.ResourceId })
            .HasPrincipalKey(resource => new { resource.TenantId, resource.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => new { link.TenantId, link.ResourceId, link.RuleId });
    }
}
