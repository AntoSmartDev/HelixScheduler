using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Availability.Orchestration;
using HelixScheduler.Application.Management.Tenants;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Core;
using HelixScheduler.Infrastructure.SqlServer;
using HelixScheduler.Infrastructure.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var services = new ServiceCollection();
services.AddHelixSchedulerInfrastructureSqlServer(configuration);
services.AddSingleton<AvailabilityEngine>();
services.AddSingleton<IClock, SystemClock>();
services.AddScoped<IAvailabilityService, AvailabilityService>();
services.AddScoped<IPropertySchemaService, PropertySchemaService>();
services.AddScoped<IResourceCatalogService, ResourceCatalogService>();
services.AddScoped<ITenantManagementService, TenantManagementService>();

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var sp = scope.ServiceProvider;

var initializer = sp.GetRequiredService<IStartupInitializer>();
await initializer.EnsureDemoSeedAsync(CancellationToken.None);

var tenantStore = sp.GetRequiredService<ITenantStore>();
var tenantContext = sp.GetRequiredService<ITenantContext>();
var defaultTenant = await tenantStore.EnsureDefaultAsync(CancellationToken.None);
tenantContext.SetTenant(defaultTenant.Id, defaultTenant.Key);

var tenantManagement = sp.GetRequiredService<ITenantManagementService>();
var tenants = await tenantManagement.ListTenantsAsync(CancellationToken.None);

var catalog = sp.GetRequiredService<IResourceCatalogService>();
var resources = await catalog.GetResourcesAsync(onlySchedulable: true, CancellationToken.None);
if (resources.Count == 0)
{
    throw new InvalidOperationException("Smoke test failed: no schedulable resources were returned by the package consumer.");
}

var targetResource = resources[0];
var today = DateOnly.FromDateTime(DateTime.UtcNow);
var request = new AvailabilityComputeRequest(
    FromDate: today,
    ToDate: today.AddDays(14),
    RequiredResourceIds: [targetResource.Id],
    SlotDurationMinutes: 30,
    Explain: true);

var availability = sp.GetRequiredService<IAvailabilityService>();
var result = await availability.ComputeAsync(request, CancellationToken.None);

Console.WriteLine("HelixScheduler package smoke succeeded.");
Console.WriteLine($"Default tenant: {defaultTenant.Key} ({defaultTenant.Id})");
Console.WriteLine($"Tenants visible: {tenants.Count}");
Console.WriteLine($"Schedulable resources: {resources.Count}");
Console.WriteLine($"Target resource: {targetResource.Name} (Id={targetResource.Id})");
Console.WriteLine($"Computed slots in next 14 days: {result.Slots.Count}");
