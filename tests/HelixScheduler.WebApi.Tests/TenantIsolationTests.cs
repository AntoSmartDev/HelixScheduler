using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class TenantIsolationTests : IAsyncLifetime
{
    private readonly TenantWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public TenantIsolationTests()
    {
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.SeedAsync(SeedTenantsAsync);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Default_Tenant_Works_Without_Header()
    {
        var response = await _client.PostAsJsonAsync("/api/availability/compute", new
        {
            fromDate = "2026-01-05",
            toDate = "2026-01-05",
            requiredResourceIds = new[] { 1 }
        });

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        var slots = doc.RootElement.GetProperty("slots");

        Assert.Equal(1, slots.GetArrayLength());
        Assert.Equal("2026-01-05T09:00:00Z", slots[0].GetProperty("startUtc").GetString());
        Assert.Equal("2026-01-05T10:00:00Z", slots[0].GetProperty("endUtc").GetString());
    }

    [Fact]
    public async Task Tenant_Isolation_Hides_Data_From_Default()
    {
        var response = await _client.PostAsJsonAsync("/api/availability/compute", new
        {
            fromDate = "2026-01-05",
            toDate = "2026-01-05",
            requiredResourceIds = new[] { 2 }
        });

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        var slots = doc.RootElement.GetProperty("slots");

        Assert.Equal(0, slots.GetArrayLength());
    }

    [Fact]
    public async Task Tenant_Header_Uses_Isolated_Data()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/availability/compute")
        {
            Content = JsonContent.Create(new
            {
                fromDate = "2026-01-05",
                toDate = "2026-01-05",
                requiredResourceIds = new[] { 2 }
            })
        };
        request.Headers.Add("X-Tenant", "tenant-b");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        var slots = doc.RootElement.GetProperty("slots");

        Assert.Equal(1, slots.GetArrayLength());
        Assert.Equal("2026-01-05T14:00:00Z", slots[0].GetProperty("startUtc").GetString());
        Assert.Equal("2026-01-05T15:00:00Z", slots[0].GetProperty("endUtc").GetString());
    }

    [Fact]
    public async Task Unknown_Tenant_Returns_NotFound()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/availability/slots?fromDate=2026-01-05&toDate=2026-01-05&resourceIds=1");
        request.Headers.Add("X-Tenant", "does-not-exist");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task SeedTenantsAsync(SchedulerDbContext dbContext)
    {
        var defaultTenantId = new Guid("11111111-1111-1111-1111-111111111111");
        var tenantBId = new Guid("22222222-2222-2222-2222-222222222222");
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mondayMask = 1 << (int)DayOfWeek.Monday;

        dbContext.Tenants.AddRange(
            new Tenants
            {
                Id = defaultTenantId,
                Key = "default",
                Label = "Default",
                CreatedAtUtc = now
            },
            new Tenants
            {
                Id = tenantBId,
                Key = "tenant-b",
                Label = "Tenant B",
                CreatedAtUtc = now
            });

        dbContext.ResourceTypes.AddRange(
            new ResourceTypes
            {
                Id = 1,
                TenantId = defaultTenantId,
                Key = "Room",
                Label = "Room",
                SortOrder = 1
            },
            new ResourceTypes
            {
                Id = 2,
                TenantId = tenantBId,
                Key = "Room",
                Label = "Room",
                SortOrder = 1
            });

        dbContext.Resources.AddRange(
            new Resources
            {
                Id = 1,
                TenantId = defaultTenantId,
                Code = "ROOM-A",
                Name = "Room A",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 1,
                CreatedAtUtc = now
            },
            new Resources
            {
                Id = 2,
                TenantId = tenantBId,
                Code = "ROOM-B",
                Name = "Room B",
                IsSchedulable = true,
                Capacity = 1,
                TypeId = 2,
                CreatedAtUtc = now
            });

        dbContext.Rules.AddRange(
            new Rules
            {
                Id = 1,
                TenantId = defaultTenantId,
                Kind = 1,
                IsExclude = false,
                Title = "Default tenant room rule",
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                DaysOfWeekMask = mondayMask,
                CreatedAtUtc = now
            },
            new Rules
            {
                Id = 2,
                TenantId = tenantBId,
                Kind = 1,
                IsExclude = false,
                Title = "Tenant B room rule",
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(15, 0),
                DaysOfWeekMask = mondayMask,
                CreatedAtUtc = now
            });

        dbContext.RuleResources.AddRange(
            new RuleResources
            {
                TenantId = defaultTenantId,
                RuleId = 1,
                ResourceId = 1
            },
            new RuleResources
            {
                TenantId = tenantBId,
                RuleId = 2,
                ResourceId = 2
            });

        await dbContext.SaveChangesAsync();
    }

    private sealed class TenantWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"tenant-tests-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<SchedulerDbContext>>();
                services.RemoveAll<SchedulerDbContext>();

                var provider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<SchedulerDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.UseInternalServiceProvider(provider);
                });
            });
        }

        public async Task SeedAsync(Func<SchedulerDbContext, Task> seed)
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            await seed(dbContext);
        }
    }
}
