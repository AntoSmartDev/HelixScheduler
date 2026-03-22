using System.Net;
using System.Text.Json;
using HelixScheduler.Application.Startup;
using HelixScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class ManagementOpenApiTests
{
    [Fact]
    public async Task OpenApi_Document_Describes_Management_Journey_And_Key_Examples()
    {
        await using var factory = new ManagementOpenApiWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var infoDescription = document.RootElement.GetProperty("info").GetProperty("description").GetString();
        Assert.Contains("management layer", infoDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start from zero", infoDescription, StringComparison.OrdinalIgnoreCase);

        var paths = document.RootElement.GetProperty("paths");
        var busyBulk = paths.GetProperty("/api/management/busy-events/bulk").GetProperty("post");
        Assert.Equal("Register busy events in bulk", busyBulk.GetProperty("summary").GetString());
        Assert.True(busyBulk.GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .TryGetProperty("example", out _));

        var legacyReport = paths.GetProperty("/api/management/validation/legacy").GetProperty("get");
        Assert.Equal("Get legacy consistency report", legacyReport.GetProperty("summary").GetString());
        Assert.True(legacyReport.GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .TryGetProperty("example", out _));
    }
}

public sealed class ManagementOpenApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"management-openapi-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SchedulerDbContext>>();
            services.RemoveAll<SchedulerDbContext>();
            services.RemoveAll<IStartupInitializer>();

            var provider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<SchedulerDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.UseInternalServiceProvider(provider);
            });

            services.AddScoped<IStartupInitializer, NoOpStartupInitializer>();
        });
    }

    private sealed class NoOpStartupInitializer : IStartupInitializer
    {
        public Task EnsureDemoSeedAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
