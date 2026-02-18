using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class DemoScenarioStatesConfiguration : IEntityTypeConfiguration<DemoScenarioStates>
{
    public void Configure(EntityTypeBuilder<DemoScenarioStates> builder)
    {
        builder.ToTable("DemoScenarioStates");
        builder.HasKey(state => state.Id);
        builder.Property(state => state.TenantId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(state => state.BaseDateUtc).IsRequired();
        builder.Property(state => state.SeedVersion).IsRequired();
        builder.Property(state => state.UpdatedAtUtc).IsRequired();

        builder.HasOne<Tenants>()
            .WithMany()
            .HasForeignKey(state => state.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(state => state.TenantId)
            .IsUnique();
    }
}
