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

        builder.Property(type => type.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(type => type.Label)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(type => type.SortOrder);

        builder.HasIndex(type => type.Key);
    }
}
