using HelixScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.Infrastructure.SqlServer.DependencyInjection;

internal static class InfrastructureSqlServerServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSchedulerInfrastructureSqlServerProvider(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SchedulerDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString,
                sqlServer => sqlServer.MigrationsAssembly(typeof(ServiceCollectionExtensions).Assembly.GetName().Name));
            options.ReplaceService<IModelCustomizer, SqlServerSchedulerModelCustomizer>();
        });

        services.AddScoped<ISqlServerDatabaseInitializer, SqlServerDatabaseInitializer>();

        return services;
    }
}
