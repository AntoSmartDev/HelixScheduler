using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class RuleResourcesConfiguration : IEntityTypeConfiguration<RuleResources>
{
    public void Configure(EntityTypeBuilder<RuleResources> builder)
    {
        builder.ToTable("RuleResources");
        builder.HasKey(link => new { link.RuleId, link.ResourceId });

        builder.HasOne(link => link.Rule)
            .WithMany(rule => rule.RuleResources)
            .HasForeignKey(link => link.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Resource)
            .WithMany(resource => resource.RuleResources)
            .HasForeignKey(link => link.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => new { link.ResourceId, link.RuleId });
    }
}
