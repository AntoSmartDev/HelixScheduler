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

            services.AddSingleton<IRuleRepository, FakeRuleRepository>();
            services.AddSingleton<IBusyEventRepository, FakeBusyEventRepository>();
            services.AddSingleton<IPropertyRepository, FakePropertyRepository>();
            services.AddSingleton<IResourceRepository, FakeResourceRepository>();
        });
    }

    private sealed class FakeRuleRepository : IRuleRepository
    {
        public Task<IReadOnlyList<Rules>> GetRulesAsync(
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            IReadOnlyCollection<int> resourceIds,
            CancellationToken ct)
        {
            var rules = new List<Rules>();
            foreach (var resourceId in resourceIds)
            {
                var (daysMask, startTime, endTime) = GetRulePattern(resourceId);
                rules.Add(new Rules
                {
                    Id = resourceId,
                    Kind = (byte)HelixScheduler.Core.RuleKind.RecurringWeekly,
                    IsExclude = false,
                    FromDateUtc = null,
                    ToDateUtc = null,
                    SingleDateUtc = null,
                    StartTime = startTime,
                    EndTime = endTime,
                    DaysOfWeekMask = daysMask,
                    CreatedAtUtc = DateTime.UtcNow,
                    RuleResources = { new RuleResources { RuleId = resourceId, ResourceId = resourceId } }
                });
            }

            return Task.FromResult<IReadOnlyList<Rules>>(rules);
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
        public Task<IReadOnlyList<BusyEvents>> GetBusyAsync(
            DateTime fromUtc,
            DateTime toUtc,
            IReadOnlyCollection<int> resourceIds,
            CancellationToken ct)
        {
            var list = resourceIds.ToList();
            if (list.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<BusyEvents>>(Array.Empty<BusyEvents>());
            }

            var events = new List<BusyEvents>();

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

            var filtered = new List<BusyEvents>();
            for (var i = 0; i < events.Count; i++)
            {
                var busy = events[i];
                if (!Overlaps(busy.StartUtc, busy.EndUtc, fromUtc, toUtc))
                {
                    continue;
                }

                var links = busy.BusyEventResources
                    .Where(link => resourceIds.Contains(link.ResourceId))
                    .ToList();
                if (links.Count == 0)
                {
                    continue;
                }

                busy.BusyEventResources.Clear();
                foreach (var link in links)
                {
                    busy.BusyEventResources.Add(link);
                }

                filtered.Add(busy);
            }

            return Task.FromResult<IReadOnlyList<BusyEvents>>(filtered);
        }

        private static BusyEvents CreateBusyEvent(
            long id,
            string title,
            DateTime startUtc,
            DateTime endUtc,
            IReadOnlyList<int> resourceIds)
        {
            var busyEvent = new BusyEvents
            {
                Id = id,
                Title = title,
                StartUtc = startUtc,
                EndUtc = endUtc,
                CreatedAtUtc = DateTime.UtcNow
            };

            for (var i = 0; i < resourceIds.Count; i++)
            {
                busyEvent.BusyEventResources.Add(new BusyEventResources
                {
                    BusyEventId = id,
                    ResourceId = resourceIds[i]
                });
            }

            return busyEvent;
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
}
