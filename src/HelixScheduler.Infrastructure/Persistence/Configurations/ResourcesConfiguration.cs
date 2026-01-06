using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class ResourcesConfiguration : IEntityTypeConfiguration<Resources>
{
    public void Configure(EntityTypeBuilder<Resources> builder)
    {
        builder.ToTable("Resources", table => table.HasCheckConstraint("CK_Resources_Capacity", "[Capacity] >= 1"));
        builder.HasKey(resource => resource.Id);

        builder.Property(resource => resource.Id)
            .ValueGeneratedOnAdd();

        builder.Property(resource => resource.Code)
            .HasMaxLength(64);

        builder.Property(resource => resource.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(resource => resource.IsSchedulable)
            .IsRequired();

        builder.Property(resource => resource.Capacity)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(resource => resource.TypeId)
            .IsRequired();

        builder.Property(resource => resource.CreatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne(resource => resource.Type)
            .WithMany(type => type.Resources)
            .HasForeignKey(resource => resource.TypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(resource => resource.Code);
        builder.HasIndex(resource => resource.IsSchedulable);
        builder.HasIndex(resource => resource.TypeId);
    }
}
