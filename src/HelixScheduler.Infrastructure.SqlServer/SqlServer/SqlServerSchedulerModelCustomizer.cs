using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HelixScheduler.Infrastructure.SqlServer;

internal sealed class SqlServerSchedulerModelCustomizer : ModelCustomizer
{
    public SqlServerSchedulerModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        modelBuilder.Entity<Rules>()
            .HasIndex(rule => new { rule.TenantId, rule.FromDateUtc, rule.ToDateUtc, rule.SingleDateUtc })
            .IncludeProperties(rule => rule.IsExclude);
    }
}
