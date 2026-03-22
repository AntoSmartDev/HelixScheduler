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

        var legacyReportResponse = await client.GetAsync("/api/management/validation/legacy");
        legacyReportResponse.EnsureSuccessStatusCode();
        using (var legacyReportStream = await legacyReportResponse.Content.ReadAsStreamAsync())
        {
            var legacyReport = await JsonDocument.ParseAsync(legacyReportStream);
            Assert.True(legacyReport.RootElement.GetProperty("validation").GetProperty("isValid").GetBoolean());
            Assert.Equal(0, legacyReport.RootElement.GetProperty("repairPreview").GetProperty("totalRepairableItems").GetInt32());
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

    [Fact]
    public async Task Busy_Event_Bulk_Endpoint_Maps_Duplicate_External_Key_In_Batch_As_BadRequest()
    {
        await using var factory = new ManagementWebApplicationFactory();
        var client = factory.CreateClient();
        var uniqueKey = $"room-{Guid.NewGuid():N}";

        var resourceTypeId = await CreateResourceTypeAsync(client, uniqueKey, "Room", 1);
        var resourceId = await CreateResourceAsync(client, "ROOM-A", "Room A", resourceTypeId);

        var response = await client.PostAsJsonAsync("/api/management/busy-events/bulk", new
        {
            definitions = new object[]
            {
                new
                {
                    resourceIds = new[] { resourceId },
                    startUtc = "2026-03-27T08:00:00Z",
                    endUtc = "2026-03-27T09:00:00Z",
                    title = "Bulk 1",
                    eventType = "Sync",
                    externalKey = "dup-key"
                },
                new
                {
                    resourceIds = new[] { resourceId },
                    startUtc = "2026-03-27T10:00:00Z",
                    endUtc = "2026-03-27T11:00:00Z",
                    title = "Bulk 2",
                    eventType = "Sync",
                    externalKey = "dup-key"
                }
            }
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("busy-event.external-key.duplicate-in-batch", doc.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task ResourceType_PropertySchema_Endpoints_Work_EndToEnd()
    {
        await using var factory = new ManagementWebApplicationFactory();
        var client = factory.CreateClient();
        var uniqueKey = $"type-{Guid.NewGuid():N}";

        var resourceTypeId = await CreateResourceTypeAsync(client, uniqueKey, "Room", 1);
        var propertyOneId = await CreatePropertyAsync(client, "Capability", "Capability", 1);
        var propertyTwoId = await CreatePropertyAsync(client, "Specialty", "Specialty", 2);

        var assignResponse = await client.PostAsJsonAsync("/api/management/resource-types/property-definitions/assign", new
        {
            resourceTypeId,
            propertyDefinitionIds = new[] { propertyOneId, propertyTwoId }
        });
        assignResponse.EnsureSuccessStatusCode();

        using (var assignStream = await assignResponse.Content.ReadAsStreamAsync())
        {
            var assigned = await JsonDocument.ParseAsync(assignStream);
            Assert.Equal(resourceTypeId, assigned.RootElement.GetProperty("resourceTypeId").GetInt32());
            Assert.Equal(2, assigned.RootElement.GetProperty("propertyDefinitionIds").GetArrayLength());
        }

        var getResponse = await client.GetAsync($"/api/management/resource-types/{resourceTypeId}/property-definitions");
        getResponse.EnsureSuccessStatusCode();

        using (var getStream = await getResponse.Content.ReadAsStreamAsync())
        {
            var current = await JsonDocument.ParseAsync(getStream);
            Assert.Equal(2, current.RootElement.GetProperty("propertyDefinitionIds").GetArrayLength());
        }

        var duplicateResponse = await client.PostAsJsonAsync("/api/management/resource-types/property-definitions/assign", new
        {
            resourceTypeId,
            propertyDefinitionIds = new[] { propertyOneId }
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var removeResponse = await client.PostAsJsonAsync("/api/management/resource-types/property-definitions/remove", new
        {
            resourceTypeId,
            propertyDefinitionIds = new[] { propertyOneId }
        });
        removeResponse.EnsureSuccessStatusCode();

        using var removeStream = await removeResponse.Content.ReadAsStreamAsync();
        var removed = await JsonDocument.ParseAsync(removeStream);
        Assert.Equal(1, removed.RootElement.GetProperty("propertyDefinitionIds").GetArrayLength());
    }

    [Fact]
    public async Task Resource_Property_Assignment_Endpoint_Maps_Type_Incompatibility_As_BadRequest()
    {
        await using var factory = new ManagementWebApplicationFactory();
        var client = factory.CreateClient();
        var uniqueKey = $"type-{Guid.NewGuid():N}";

        var resourceTypeId = await CreateResourceTypeAsync(client, uniqueKey, "Room", 1);
        var resourceId = await CreateResourceAsync(client, "ROOM-T", "Room Typed", resourceTypeId);
        var propertyId = await CreatePropertyAsync(client, "UnmappedProperty", "Unmapped Property", 1);

        var response = await client.PostAsJsonAsync("/api/management/resource-property-assignments/assign", new
        {
            resourceId,
            propertyIds = new[] { propertyId }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("property.assignment.type-incompatibility", doc.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Catalog_Snapshot_Filters_Archived_Resources_But_Resource_Configuration_Still_Shows_Legacy_Bindings()
    {
        await using var factory = new ManagementWebApplicationFactory();
        var client = factory.CreateClient();
        var uniqueKey = $"room-{Guid.NewGuid():N}";

        var resourceTypeId = await CreateResourceTypeAsync(client, uniqueKey, "Room", 1);
        var activeResourceId = await CreateResourceAsync(client, "ROOM-A", "Room A", resourceTypeId);
        var archivedResourceId = await CreateResourceAsync(client, "ROOM-B", "Room B", resourceTypeId);

        await CreateRuleAsync(client, archivedResourceId);
        await CreateBusyEventAsync(client, archivedResourceId, "archived-busy");

        var archiveResponse = await client.PostAsync($"/api/management/resources/{archivedResourceId}/archive", null);
        archiveResponse.EnsureSuccessStatusCode();

        var snapshotResponse = await client.GetAsync("/api/management/catalog/snapshot");
        snapshotResponse.EnsureSuccessStatusCode();

        using (var snapshotStream = await snapshotResponse.Content.ReadAsStreamAsync())
        {
            var snapshot = await JsonDocument.ParseAsync(snapshotStream);
            var resourceIds = snapshot.RootElement
                .GetProperty("resources")
                .EnumerateArray()
                .Select(item => item.GetProperty("id").GetInt32())
                .ToArray();

            Assert.Contains(activeResourceId, resourceIds);
            Assert.DoesNotContain(archivedResourceId, resourceIds);
        }

        var resourceConfigurationResponse = await client.PostAsJsonAsync("/api/management/catalog/resource-configuration", new
        {
            resourceId = archivedResourceId,
            fromDateUtc = "2026-03-01",
            toDateUtc = "2026-03-31"
        });
        resourceConfigurationResponse.EnsureSuccessStatusCode();

        using var resourceConfigurationStream = await resourceConfigurationResponse.Content.ReadAsStreamAsync();
        var resourceConfiguration = await JsonDocument.ParseAsync(resourceConfigurationStream);
        Assert.Equal(archivedResourceId, resourceConfiguration.RootElement.GetProperty("resource").GetProperty("id").GetInt32());
        Assert.Equal(1, resourceConfiguration.RootElement.GetProperty("rules").GetArrayLength());
        Assert.Equal(1, resourceConfiguration.RootElement.GetProperty("busyEvents").GetArrayLength());
        Assert.False(resourceConfiguration.RootElement.GetProperty("validation").GetProperty("isValid").GetBoolean());
    }

    [Fact]
    public async Task Legacy_Consistency_Endpoints_Report_And_Cleanup_Inactive_Property_References()
    {
        await using var factory = new ManagementWebApplicationFactory();
        var client = factory.CreateClient();
        var uniqueKey = $"legacy-{Guid.NewGuid():N}";

        var resourceTypeId = await CreateResourceTypeAsync(client, uniqueKey, "Room", 1);
        var resourceId = await CreateResourceAsync(client, "ROOM-L", "Legacy Room", resourceTypeId);
        var propertyId = await CreatePropertyAsync(client, "LegacyProperty", "Legacy Property", 1);

        var assignTypeResponse = await client.PostAsJsonAsync("/api/management/resource-types/property-definitions/assign", new
        {
            resourceTypeId,
            propertyDefinitionIds = new[] { propertyId }
        });
        assignTypeResponse.EnsureSuccessStatusCode();

        var assignResourceResponse = await client.PostAsJsonAsync("/api/management/resource-property-assignments/assign", new
        {
            resourceId,
            propertyIds = new[] { propertyId }
        });
        assignResourceResponse.EnsureSuccessStatusCode();

        var deactivatePropertyResponse = await client.PostAsync($"/api/management/properties/{propertyId}/deactivate", null);
        Assert.Equal(HttpStatusCode.Conflict, deactivatePropertyResponse.StatusCode);

        await ForceDeactivatePropertyAsync(factory, propertyId);

        var reportResponse = await client.GetAsync("/api/management/validation/legacy");
        reportResponse.EnsureSuccessStatusCode();

        using (var reportStream = await reportResponse.Content.ReadAsStreamAsync())
        {
            var report = await JsonDocument.ParseAsync(reportStream);
            Assert.False(report.RootElement.GetProperty("validation").GetProperty("isValid").GetBoolean());
            Assert.Equal(2, report.RootElement.GetProperty("repairPreview").GetProperty("totalRepairableItems").GetInt32());
        }

        var cleanupResponse = await client.PostAsync("/api/management/validation/legacy/cleanup-inactive-property-references", null);
        cleanupResponse.EnsureSuccessStatusCode();

        using (var cleanupStream = await cleanupResponse.Content.ReadAsStreamAsync())
        {
            var cleanup = await JsonDocument.ParseAsync(cleanupStream);
            Assert.Equal(1, cleanup.RootElement.GetProperty("removedResourcePropertyAssignments").GetInt32());
            Assert.Equal(1, cleanup.RootElement.GetProperty("removedResourceTypePropertyMappings").GetInt32());
            Assert.Equal(0, cleanup.RootElement.GetProperty("reportAfter").GetProperty("repairPreview").GetProperty("totalRepairableItems").GetInt32());
        }
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

    private static async Task CreateBusyEventAsync(HttpClient client, int resourceId, string externalKey)
    {
        var response = await client.PostAsJsonAsync("/api/management/busy-events", new
        {
            definition = new
            {
                resourceIds = new[] { resourceId },
                startUtc = "2026-03-25T12:00:00Z",
                endUtc = "2026-03-25T13:00:00Z",
                title = "Busy",
                eventType = "Sync",
                externalKey
            }
        });
        response.EnsureSuccessStatusCode();
    }

    private static async Task ForceDeactivatePropertyAsync(ManagementWebApplicationFactory factory, int propertyId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var property = await dbContext.ResourceProperties.IgnoreQueryFilters().FirstAsync(item => item.Id == propertyId);
        property.IsActive = false;
        await dbContext.SaveChangesAsync();
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
