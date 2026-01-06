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

        builder.Property(property => property.Id)
            .ValueGeneratedOnAdd();

        builder.Property(property => property.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(property => property.Label)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(property => property.ParentId);

        builder.HasOne(property => property.Parent)
            .WithMany(parent => parent.Children)
            .HasForeignKey(property => property.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
