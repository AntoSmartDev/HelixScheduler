using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelixScheduler.Infrastructure.Persistence.Configurations;

public sealed class RulesConfiguration : IEntityTypeConfiguration<Rules>
{
    public void Configure(EntityTypeBuilder<Rules> builder)
    {
        builder.ToTable("Rules");
        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Id)
            .ValueGeneratedOnAdd();

        builder.Property(rule => rule.Kind)
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(rule => rule.Title)
            .HasMaxLength(300);

        builder.Property(rule => rule.FromDateUtc)
            .HasColumnType("date");

        builder.Property(rule => rule.ToDateUtc)
            .HasColumnType("date");

        builder.Property(rule => rule.SingleDateUtc)
            .HasColumnType("date");

        builder.Property(rule => rule.StartTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(rule => rule.EndTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(rule => rule.CreatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(rule => new { rule.FromDateUtc, rule.ToDateUtc, rule.SingleDateUtc })
            .IncludeProperties(rule => rule.IsExclude);
    }
}
