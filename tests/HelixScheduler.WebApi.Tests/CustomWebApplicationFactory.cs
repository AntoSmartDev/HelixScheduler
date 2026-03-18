using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HelixScheduler.WebApi.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAvailabilityComputeQueryService>();
            services.RemoveAll<IAvailabilityFilterQueryService>();
            services.RemoveAll<IAvailabilityAncestorQueryService>();
            services.RemoveAll<IAvailabilitySummaryQueryService>();
            services.RemoveAll<ITenantStore>();

            services.AddSingleton<FakeAvailabilityQueryService>();
            services.AddSingleton<IAvailabilityComputeQueryService>(sp => sp.GetRequiredService<FakeAvailabilityQueryService>());
            services.AddSingleton<IAvailabilityFilterQueryService>(sp => sp.GetRequiredService<FakeAvailabilityQueryService>());
            services.AddSingleton<IAvailabilityAncestorQueryService>(sp => sp.GetRequiredService<FakeAvailabilityQueryService>());
            services.AddSingleton<IAvailabilitySummaryQueryService>(sp => sp.GetRequiredService<FakeAvailabilityQueryService>());
            services.AddSingleton<ITenantStore, FakeTenantStore>();
        });
    }

    private sealed class FakeAvailabilityQueryService :
        IAvailabilityComputeQueryService,
        IAvailabilityFilterQueryService,
        IAvailabilityAncestorQueryService,
        IAvailabilitySummaryQueryService
    {
        private static readonly Dictionary<int, (int DaysMask, TimeOnly Start, TimeOnly End)> RulePatterns = new()
        {
            [4] = ((1 << (int)DayOfWeek.Monday) | (1 << (int)DayOfWeek.Thursday), new TimeOnly(9, 0), new TimeOnly(13, 0)),
            [5] = ((1 << (int)DayOfWeek.Monday) | (1 << (int)DayOfWeek.Thursday), new TimeOnly(9, 0), new TimeOnly(13, 0))
        };

        private static readonly Dictionary<int, int> CustomCapacities = new()
        {
            [99] = 2
        };

        public Task<IReadOnlyList<RuleData>> GetRulesAsync(
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var rules = new List<RuleData>();
            foreach (var resourceId in resourceIds)
            {
                var (daysMask, startTime, endTime) = GetRulePattern(resourceId);
                rules.Add(new RuleData(
                    resourceId,
                    (byte)HelixScheduler.Core.RuleKind.RecurringWeekly,
                    false,
                    null,
                    null,
                    null,
                    startTime,
                    endTime,
                    daysMask,
                    null,
                    null,
                    new[] { resourceId }));
            }

            return Task.FromResult<IReadOnlyList<RuleData>>(rules);
        }

        public Task<IReadOnlyDictionary<int, int>> GetResourceCapacitiesAsync(
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var result = new Dictionary<int, int>();
            foreach (var resourceId in resourceIds)
            {
                if (CustomCapacities.TryGetValue(resourceId, out var capacity))
                {
                    result[resourceId] = capacity;
                }
            }

            return Task.FromResult<IReadOnlyDictionary<int, int>>(result);
        }

        public Task<IReadOnlyList<BusyEventData>> GetBusyEventsAsync(
            DateTime fromUtc,
            DateTime toUtcExclusive,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var list = resourceIds.ToList();
            if (list.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<BusyEventData>>(Array.Empty<BusyEventData>());
            }

            var events = new List<BusyEventData>
            {
                new(
                    1,
                    new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc),
                    new[] { list[0] }),
                new(
                    2,
                    new DateTime(2025, 3, 12, 14, 30, 0, DateTimeKind.Utc),
                    new DateTime(2025, 3, 12, 15, 0, 0, DateTimeKind.Utc),
                    list.Count > 1 ? new[] { list[0], list[1] } : new[] { list[0] }),
                new(
                    3,
                    new DateTime(2026, 3, 9, 10, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 3, 9, 11, 0, 0, DateTimeKind.Utc),
                    new[] { list[0] }),
                new(
                    4,
                    new DateTime(2026, 3, 12, 10, 30, 0, DateTimeKind.Utc),
                    new DateTime(2026, 3, 12, 11, 0, 0, DateTimeKind.Utc),
                    list.Count > 1 ? new[] { list[0], list[1] } : new[] { list[0] })
            };

            var filtered = new List<BusyEventData>();
            foreach (var busy in events)
            {
                if (busy.StartUtc >= toUtcExclusive || busy.EndUtc <= fromUtc)
                {
                    continue;
                }

                var links = busy.ResourceIds.Where(resourceIds.Contains).ToList();
                if (links.Count == 0)
                {
                    continue;
                }

                filtered.Add(busy with { ResourceIds = links });
            }

            return Task.FromResult<IReadOnlyList<BusyEventData>>(filtered);
        }

        public Task<IReadOnlyList<PropertyNode>> ExpandPropertySubtreeAsync(int propertyId, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<PropertyNode>>(Array.Empty<PropertyNode>());
        }

        public Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
            IReadOnlyList<int> propertyIds,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
        }

        public Task<IReadOnlyList<int>> GetResourceIdsByAllPropertiesAsync(
            IReadOnlyList<int> propertyIds,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
        }

        public Task<IReadOnlyList<IReadOnlyList<int>>> GetResourceIdsByPropertySetsAsync(
            IReadOnlyList<IReadOnlyList<int>> propertySets,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<IReadOnlyList<int>>>(
                propertySets.Select(_ => (IReadOnlyList<int>)Array.Empty<int>()).ToList());
        }

        public Task<IReadOnlyList<ResourceRelationLink>> GetResourceRelationsByTypesAsync(
            IReadOnlyList<string>? relationTypes,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ResourceRelationLink>>(Array.Empty<ResourceRelationLink>());
        }

        public Task<IReadOnlyList<ResourceSummary>> GetResourcesAsync(bool onlySchedulable, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ResourceSummary>>(Array.Empty<ResourceSummary>());
        }

        public async Task<IReadOnlyList<RuleSummary>> GetRuleSummariesAsync(
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var rules = await GetRulesAsync(fromDateUtc, toDateUtc, resourceIds, ct).ConfigureAwait(false);
            return rules
                .Select(rule => new RuleSummary(
                    rule.Id,
                    $"Rule {rule.Id}",
                    rule.Kind,
                    rule.IsExclude,
                    rule.FromDateUtc,
                    rule.ToDateUtc,
                    rule.SingleDateUtc,
                    rule.StartTime,
                    rule.EndTime,
                    rule.DaysOfWeekMask,
                    rule.ResourceIds))
                .ToList();
        }

        public async Task<IReadOnlyList<BusyEventSummary>> GetBusyEventSummariesAsync(
            DateTime fromUtc,
            DateTime toUtcExclusive,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var busyEvents = await GetBusyEventsAsync(fromUtc, toUtcExclusive, resourceIds, ct).ConfigureAwait(false);
            return busyEvents
                .Select(busy => new BusyEventSummary(
                    busy.Id,
                    $"Busy {busy.Id}",
                    busy.StartUtc,
                    busy.EndUtc,
                    busy.ResourceIds))
                .ToList();
        }

        private static (int DaysMask, TimeOnly Start, TimeOnly End) GetRulePattern(int resourceId)
        {
            if (RulePatterns.TryGetValue(resourceId, out var pattern))
            {
                return pattern;
            }

            var defaultDays = (1 << (int)DayOfWeek.Monday) | (1 << (int)DayOfWeek.Wednesday);
            return (defaultDays, new TimeOnly(14, 0), new TimeOnly(18, 0));
        }
    }

    private sealed class FakeTenantStore : ITenantStore
    {
        private static readonly TenantInfo DefaultTenant = new(
            new Guid("11111111-1111-1111-1111-111111111111"),
            "default",
            "Default");

        public Task<TenantInfo?> FindByKeyAsync(string key, CancellationToken ct)
        {
            if (string.Equals(key, DefaultTenant.Key, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<TenantInfo?>(DefaultTenant);
            }

            return Task.FromResult<TenantInfo?>(null);
        }

        public Task<TenantInfo> EnsureDefaultAsync(CancellationToken ct)
        {
            return Task.FromResult(DefaultTenant);
        }
    }
}
