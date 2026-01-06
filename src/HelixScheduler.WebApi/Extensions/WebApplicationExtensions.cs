using HelixScheduler.Application.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace HelixScheduler.Extensions;

public static class WebApplicationExtensions
{
    public static async Task UseHelixSchedulerDemoSeedAsync(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IStartupInitializer>();
        await initializer.EnsureDemoSeedAsync(CancellationToken.None);
    }
}
