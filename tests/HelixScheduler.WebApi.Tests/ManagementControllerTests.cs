using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HelixScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class ManagementControllerTests
{
    [Fact]
    public async Task Tenant_Endpoints_Map_Conflict_Errors_Uniformly()
    {
        await using var factory = new ManagementWebApplicationFactory();
        var client = factory.CreateClient();

        var created = await client.PostAsJsonAsync("/api/management/tenants", new
        {
            key = "tenant-a",
            label = "Tenant A"
        });
        created.EnsureSuccessStatusCode();

        var duplicate = await client.PostAsJsonAsync("/api/management/tenants", new
        {
            key = "tenant-a",
            label = "Tenant A Duplicate"
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        using var stream = await duplicate.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("tenant.key.duplicate", doc.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
        Assert.Equal("Conflict", doc.RootElement.GetProperty("errors")[0].GetProperty("category").GetString());
    }

    [Fact]
    public async Task Catalog_And_Validation_Endpoints_Work_EndToEnd()
    {
        await using var factory = new ManagementWebApplicationFactory();
        var client = factory.CreateClient();
        var uniqueKey = $"room-{Guid.NewGuid():N}";

        var resourceTypeId = await CreateResourceTypeAsync(client, uniqueKey, "Room", 1);
        var parentResourceId = await CreateResourceAsync(client, "ROOM-A", "Room A", resourceTypeId);
        var childResourceId = await CreateResourceAsync(client, "ROOM-B", "Room B", resourceTypeId);
        await AddHierarchyRelationAsync(client, parentResourceId, childResourceId, "Contains");

        await CreatePropertyAsync(client, "Capability", "Capability", 1);
        await CreateRuleAsync(client, childResourceId);

        var snapshotResponse = await client.GetAsync("/api/management/catalog/snapshot");
        snapshotResponse.EnsureSuccessStatusCode();

        using (var snapshotStream = await snapshotResponse.Content.ReadAsStreamAsync())
        {
            var snapshot = await JsonDocument.ParseAsync(snapshotStream);
            Assert.Equal(1, snapshot.RootElement.GetProperty("resourceTypes").GetArrayLength());
            Assert.Equal(2, snapshot.RootElement.GetProperty("resources").GetArrayLength());
            Assert.Equal(1, snapshot.RootElement.GetProperty("hierarchyRelations").GetArrayLength());
            Assert.True(snapshot.RootElement.GetProperty("validation").GetProperty("isValid").GetBoolean());
        }

        var validationResponse = await client.GetAsync("/api/management/validation/tenant");
        validationResponse.EnsureSuccessStatusCode();

        using (var validationStream = await validationResponse.Content.ReadAsStreamAsync())
        {
            var validation = await JsonDocument.ParseAsync(validationStream);
            Assert.True(validation.RootElement.GetProperty("isValid").GetBoolean());
        }

        var resourceConfigurationResponse = await client.PostAsJsonAsync("/api/management/catalog/resource-configuration", new
        {
            resourceId = childResourceId,
            fromDateUtc = "2026-03-01",
            toDateUtc = "2026-03-31"
        });
        resourceConfigurationResponse.EnsureSuccessStatusCode();

        using var resourceConfigurationStream = await resourceConfigurationResponse.Content.ReadAsStreamAsync();
        var resourceConfiguration = await JsonDocument.ParseAsync(resourceConfigurationStream);
        Assert.Equal(childResourceId, resourceConfiguration.RootElement.GetProperty("resource").GetProperty("id").GetInt32());
        Assert.Equal(0, resourceConfiguration.RootElement.GetProperty("assignedProperties").GetArrayLength());
        Assert.Equal(1, resourceConfiguration.RootElement.GetProperty("hierarchyRelations").GetArrayLength());
        Assert.Equal(1, resourceConfiguration.RootElement.GetProperty("rules").GetArrayLength());
    }

    [Fact]
    public async Task Busy_Event_Endpoints_Expose_Bulk_And_Idempotent_Upsert()
    {
        await using var factory = new ManagementWebApplicationFactory();
        var client = factory.CreateClient();
        var uniqueKey = $"room-{Guid.NewGuid():N}";

        var resourceTypeId = await CreateResourceTypeAsync(client, uniqueKey, "Room", 1);
        var firstResourceId = await CreateResourceAsync(client, "ROOM-A", "Room A", resourceTypeId);
        var secondResourceId = await CreateResourceAsync(client, "ROOM-B", "Room B", resourceTypeId);

        var bulkResponse = await client.PostAsJsonAsync("/api/management/busy-events/bulk", new
        {
            definitions = new object[]
            {
                new
                {
                    resourceIds = new[] { firstResourceId },
                    startUtc = "2026-03-27T08:00:00Z",
                    endUtc = "2026-03-27T09:00:00Z",
                    title = "Bulk 1",
                    eventType = "Sync",
                    externalKey = "ext-1"
                },
                new
                {
                    resourceIds = new[] { firstResourceId, secondResourceId },
                    startUtc = "2026-03-27T09:00:00Z",
                    endUtc = "2026-03-27T10:00:00Z",
                    title = "Bulk 2",
                    eventType = "Sync",
                    externalKey = "ext-2"
                }
            }
        });
        bulkResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await client.PostAsJsonAsync("/api/management/busy-events", new
        {
            definition = new
            {
                resourceIds = new[] { secondResourceId },
                startUtc = "2026-03-28T08:00:00Z",
                endUtc = "2026-03-28T09:00:00Z",
                title = "Duplicate",
                eventType = "Sync",
                externalKey = "ext-1"
            }
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var upsertCreateResponse = await client.PutAsJsonAsync("/api/management/busy-events/upsert", new
        {
            externalKey = "ext-upsert",
            definition = new
            {
                resourceIds = new[] { firstResourceId },
                startUtc = "2026-03-29T10:00:00Z",
                endUtc = "2026-03-29T11:00:00Z",
                title = "Upsert create",
                eventType = "Sync",
                externalKey = (string?)null
            }
        });
        upsertCreateResponse.EnsureSuccessStatusCode();

        long createdId;
        using (var createStream = await upsertCreateResponse.Content.ReadAsStreamAsync())
        {
            var created = await JsonDocument.ParseAsync(createStream);
            createdId = created.RootElement.GetProperty("id").GetInt64();
            Assert.Equal("ext-upsert", created.RootElement.GetProperty("externalKey").GetString());
        }

        var upsertUpdateResponse = await client.PutAsJsonAsync("/api/management/busy-events/upsert", new
        {
            externalKey = "ext-upsert",
            definition = new
            {
                resourceIds = new[] { secondResourceId },
                startUtc = "2026-03-29T12:00:00Z",
                endUtc = "2026-03-29T13:00:00Z",
                title = "Upsert update",
                eventType = "Sync",
                externalKey = (string?)null
            }
        });
        upsertUpdateResponse.EnsureSuccessStatusCode();

        using (var updateStream = await upsertUpdateResponse.Content.ReadAsStreamAsync())
        {
            var updated = await JsonDocument.ParseAsync(updateStream);
            Assert.Equal(createdId, updated.RootElement.GetProperty("id").GetInt64());
            Assert.Equal("Upsert update", updated.RootElement.GetProperty("title").GetString());
            Assert.Equal(1, updated.RootElement.GetProperty("resourceIds").GetArrayLength());
        }

        var listResponse = await client.GetAsync("/api/management/busy-events");
        listResponse.EnsureSuccessStatusCode();
        using var listStream = await listResponse.Content.ReadAsStreamAsync();
        var listed = await JsonDocument.ParseAsync(listStream);
        Assert.Equal(3, listed.RootElement.GetArrayLength());
    }

    private static async Task<int> CreateResourceTypeAsync(HttpClient client, string key, string label, int sortOrder)
    {
        var response = await client.PostAsJsonAsync("/api/management/resource-types", new { key, label, sortOrder });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetInt32();
    }

    private static async Task<int> CreateResourceAsync(HttpClient client, string code, string name, int typeId)
    {
        var response = await client.PostAsJsonAsync("/api/management/resources", new
        {
            code,
            name,
            isSchedulable = true,
            capacity = 1,
            typeId
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetInt32();
    }

    private static async Task AddHierarchyRelationAsync(HttpClient client, int parentResourceId, int childResourceId, string relationType)
    {
        var response = await client.PostAsJsonAsync("/api/management/hierarchy/relations", new
        {
            parentResourceId,
            childResourceId,
            relationType
        });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<int> CreatePropertyAsync(HttpClient client, string key, string label, int sortOrder)
    {
        var response = await client.PostAsJsonAsync("/api/management/properties", new { key, label, sortOrder });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetInt32();
    }

    private static async Task CreateRuleAsync(HttpClient client, int resourceId)
    {
        var response = await client.PostAsJsonAsync("/api/management/rules", new
        {
            definition = new
            {
                shape = 2,
                isExclude = false,
                resourceIds = new[] { resourceId },
                title = "Single rule",
                fromDateUtc = (string?)null,
                toDateUtc = (string?)null,
                singleDateUtc = "2026-03-25",
                startTime = "09:00:00",
                endTime = "10:00:00",
                daysOfWeekMask = (int?)null,
                dayOfMonth = (int?)null,
                intervalDays = (int?)null
            }
        });
        response.EnsureSuccessStatusCode();
    }
}

public sealed class ManagementWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"management-webapi-{Guid.NewGuid()}";

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
}
