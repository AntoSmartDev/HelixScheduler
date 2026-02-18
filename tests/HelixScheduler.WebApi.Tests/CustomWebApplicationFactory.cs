using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability;
using HelixScheduler.Infrastructure.Persistence.Repositories;
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
            services.RemoveAll<IRuleRepository>();
            services.RemoveAll<IBusyEventRepository>();
            services.RemoveAll<IPropertyRepository>();
            services.RemoveAll<IResourceRepository>();
            services.RemoveAll<ITenantStore>();
            services.RemoveAll<IAvailabilityDataSource>();

            services.AddSingleton<IRuleRepository, FakeRuleRepository>();
            services.AddSingleton<IBusyEventRepository, FakeBusyEventRepository>();
            services.AddSingleton<IPropertyRepository, FakePropertyRepository>();
            services.AddSingleton<IResourceRepository, FakeResourceRepository>();
            services.AddSingleton<ITenantStore, FakeTenantStore>();
            services.AddSingleton<IAvailabilityDataSource, FakeAvailabilityDataSource>();
        });
    }

    private sealed class FakeRuleRepository : IRuleRepository
    {
        public Task<IReadOnlyList<RuleRow>> GetRulesAsync(
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            IReadOnlyCollection<int> resourceIds,
            CancellationToken ct)
        {
            var rules = new List<RuleRow>();
            foreach (var resourceId in resourceIds)
            {
                var (daysMask, startTime, endTime) = GetRulePattern(resourceId);
                rules.Add(new RuleRow(
                    resourceId,
                    (byte)HelixScheduler.Core.RuleKind.RecurringWeekly,
                    false,
                    $"Rule {resourceId}",
                    null,
                    null,
                    null,
                    startTime,
                    endTime,
                    daysMask,
                    null,
                    null,
                    DateTime.UtcNow,
                    new[] { resourceId }));
            }

            return Task.FromResult<IReadOnlyList<RuleRow>>(rules);
        }

        private static (int DaysMask, TimeOnly Start, TimeOnly End) GetRulePattern(int resourceId)
        {
            if (resourceId is 4 or 5)
            {
                var days = (1 << (int)DayOfWeek.Monday) | (1 << (int)DayOfWeek.Thursday);
                return (days, new TimeOnly(9, 0), new TimeOnly(13, 0));
            }

            var defaultDays = (1 << (int)DayOfWeek.Monday) | (1 << (int)DayOfWeek.Wednesday);
            return (defaultDays, new TimeOnly(14, 0), new TimeOnly(18, 0));
        }
    }

    private sealed class FakeBusyEventRepository : IBusyEventRepository
    {
        public Task<IReadOnlyList<BusyEventRow>> GetBusyAsync(
            DateTime fromUtc,
            DateTime toUtc,
            IReadOnlyCollection<int> resourceIds,
            CancellationToken ct)
        {
            var list = resourceIds.ToList();
            if (list.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<BusyEventRow>>(Array.Empty<BusyEventRow>());
            }

            var events = new List<BusyEventRow>();

            var busyDoctor = CreateBusyEvent(
                1,
                "Doctor busy",
                new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc),
                new[] { list[0] });
            events.Add(busyDoctor);

            var busyBoth = CreateBusyEvent(
                2,
                "Doctor+Room busy",
                new DateTime(2025, 3, 12, 14, 30, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 12, 15, 0, 0, DateTimeKind.Utc),
                list.Count > 1 ? new[] { list[0], list[1] } : new[] { list[0] });
            events.Add(busyBoth);

            var busyDoctor2026 = CreateBusyEvent(
                3,
                "Doctor 8 busy",
                new DateTime(2026, 3, 9, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 9, 11, 0, 0, DateTimeKind.Utc),
                new[] { list[0] });
            events.Add(busyDoctor2026);

            var busyBoth2026 = CreateBusyEvent(
                4,
                "Doctor 8 + Room 2 busy",
                new DateTime(2026, 3, 12, 10, 30, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 12, 11, 0, 0, DateTimeKind.Utc),
                list.Count > 1 ? new[] { list[0], list[1] } : new[] { list[0] });
            events.Add(busyBoth2026);

            var filtered = new List<BusyEventRow>();
            for (var i = 0; i < events.Count; i++)
            {
                var busy = events[i];
                if (!Overlaps(busy.StartUtc, busy.EndUtc, fromUtc, toUtc))
                {
                    continue;
                }

                var links = busy.ResourceIds
                    .Where(resourceIds.Contains)
                    .ToList();
                if (links.Count == 0)
                {
                    continue;
                }

                filtered.Add(busy with { ResourceIds = links });
            }

            return Task.FromResult<IReadOnlyList<BusyEventRow>>(filtered);
        }

        private static BusyEventRow CreateBusyEvent(
            long id,
            string title,
            DateTime startUtc,
            DateTime endUtc,
            IReadOnlyList<int> resourceIds)
        {
            return new BusyEventRow(
                id,
                title,
                startUtc,
                endUtc,
                string.Empty,
                DateTime.UtcNow,
                resourceIds.ToList());
        }

        private static bool Overlaps(DateTime startUtc, DateTime endUtc, DateTime windowStartUtc, DateTime windowEndUtc)
        {
            return startUtc < windowEndUtc && endUtc > windowStartUtc;
        }
    }

    private sealed class FakePropertyRepository : IPropertyRepository
    {
        public Task<IReadOnlyList<ResourceProperties>> ExpandPropertySubtreeAsync(int propertyId, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ResourceProperties>>(Array.Empty<ResourceProperties>());
        }

        public Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
            IReadOnlyCollection<int> propertyIds,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
        }

        public Task<IReadOnlyList<int>> GetResourceIdsByAllPropertiesAsync(
            IReadOnlyCollection<int> propertyIds,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
        }
    }

    private sealed class FakeResourceRepository : IResourceRepository
    {
        private static readonly Dictionary<int, int> CustomCapacities = new()
        {
            [99] = 2
        };

        public Task<IReadOnlyDictionary<int, int>> GetCapacitiesAsync(
            IReadOnlyCollection<int> resourceIds,
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

    private sealed class FakeAvailabilityDataSource : IAvailabilityDataSource
    {
        private readonly IRuleRepository _ruleRepository;
        private readonly IBusyEventRepository _busyEventRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IResourceRepository _resourceRepository;

        public FakeAvailabilityDataSource(
            IRuleRepository ruleRepository,
            IBusyEventRepository busyEventRepository,
            IPropertyRepository propertyRepository,
            IResourceRepository resourceRepository)
        {
            _ruleRepository = ruleRepository;
            _busyEventRepository = busyEventRepository;
            _propertyRepository = propertyRepository;
            _resourceRepository = resourceRepository;
        }

        public async Task<IReadOnlyList<RuleData>> GetRulesAsync(
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var rules = await _ruleRepository
                .GetRulesAsync(fromDateUtc, toDateUtc, resourceIds, ct)
                .ConfigureAwait(false);

            return rules
                .Select(rule => new RuleData(
                    rule.Id,
                    rule.Kind,
                    rule.IsExclude,
                    rule.FromDateUtc,
                    rule.ToDateUtc,
                    rule.SingleDateUtc,
                    rule.StartTime,
                    rule.EndTime,
                    rule.DaysOfWeekMask,
                    rule.DayOfMonth,
                    rule.IntervalDays,
                    rule.ResourceIds))
                .ToList();
        }

        public Task<IReadOnlyDictionary<int, int>> GetResourceCapacitiesAsync(
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            return _resourceRepository.GetCapacitiesAsync(resourceIds, ct);
        }

        public async Task<IReadOnlyList<BusyEventData>> GetBusyEventsAsync(
            DateTime fromUtc,
            DateTime toUtcExclusive,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var busyEvents = await _busyEventRepository
                .GetBusyAsync(fromUtc, toUtcExclusive, resourceIds, ct)
                .ConfigureAwait(false);

            return busyEvents
                .Select(busy => new BusyEventData(
                    busy.Id,
                    busy.StartUtc,
                    busy.EndUtc,
                    busy.ResourceIds))
                .ToList();
        }

        public async Task<IReadOnlyList<PropertyNode>> ExpandPropertySubtreeAsync(
            int propertyId,
            CancellationToken ct)
        {
            var properties = await _propertyRepository
                .ExpandPropertySubtreeAsync(propertyId, ct)
                .ConfigureAwait(false);

            return properties
                .Select(property => new PropertyNode(
                    property.Id,
                    property.ParentId,
                    property.Key,
                    property.Label,
                    property.SortOrder))
                .ToList();
        }

        public Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
            IReadOnlyList<int> propertyIds,
            CancellationToken ct)
        {
            return _propertyRepository.GetResourceIdsByPropertiesAsync(propertyIds, ct);
        }

        public Task<IReadOnlyList<int>> GetResourceIdsByAllPropertiesAsync(
            IReadOnlyList<int> propertyIds,
            CancellationToken ct)
        {
            return _propertyRepository.GetResourceIdsByAllPropertiesAsync(propertyIds, ct);
        }

        public Task<IReadOnlyList<ResourceSummary>> GetResourcesAsync(
            bool onlySchedulable,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ResourceSummary>>(Array.Empty<ResourceSummary>());
        }

        public async Task<IReadOnlyList<RuleSummary>> GetRuleSummariesAsync(
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var rules = await _ruleRepository
                .GetRulesAsync(fromDateUtc, toDateUtc, resourceIds, ct)
                .ConfigureAwait(false);

            return rules
                .Select(rule => new RuleSummary(
                    rule.Id,
                    rule.Title,
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
            var busyEvents = await _busyEventRepository
                .GetBusyAsync(fromUtc, toUtcExclusive, resourceIds, ct)
                .ConfigureAwait(false);

            return busyEvents
                .Select(busy => new BusyEventSummary(
                    busy.Id,
                    busy.Title,
                    busy.StartUtc,
                    busy.EndUtc,
                    busy.ResourceIds))
                .ToList();
        }

        public Task<IReadOnlyList<ResourceRelationLink>> GetResourceRelationsAsync(
            IReadOnlyList<int> childResourceIds,
            IReadOnlyList<string>? relationTypes,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ResourceRelationLink>>(Array.Empty<ResourceRelationLink>());
        }
    }
}
