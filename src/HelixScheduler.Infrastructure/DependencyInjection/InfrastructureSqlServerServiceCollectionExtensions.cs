using HelixScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.Infrastructure.DependencyInjection;

internal static class InfrastructureSqlServerServiceCollectionExtensions
{
    public static IServiceCollection AddHelixSchedulerInfrastructureSqlServer(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SchedulerDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }
}
