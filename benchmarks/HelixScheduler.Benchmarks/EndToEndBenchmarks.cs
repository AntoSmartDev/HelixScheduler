using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Order;
using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Core;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.Repositories;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[Config(typeof(EndToEndBenchmarkConfig))]
public class EndToEndBenchmarks
{
    private SchedulerDbContext _dbContext = null!;
    private AvailabilityService _service = null!;
    private AvailabilityComputeRequest _request = null!;

    [ParamsSource(nameof(Scenarios))]
    public EndToEndScenario Scenario { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        var dbName = $"bench-{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        _dbContext = new SchedulerDbContext(options);
        _dbContext.Database.EnsureCreated();
        SeedBenchmarkData();

        var ruleRepository = new RuleRepository(_dbContext);
        var busyRepository = new BusyEventRepository(_dbContext);
        var propertyRepository = new BenchmarkPropertyRepository(_dbContext);
        var resourceRepository = new ResourceRepository(_dbContext);
        var availabilitySource = new AvailabilityDataSource(
            _dbContext,
            ruleRepository,
            busyRepository,
            propertyRepository,
            resourceRepository);
        var schemaSource = new PropertySchemaDataSource(_dbContext);
        var schemaService = new PropertySchemaService(schemaSource);
        _service = new AvailabilityService(availabilitySource, schemaService, new AvailabilityEngineV1());

        _request = Scenario.BuildRequest(_dbContext);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext.Dispose();
    }

    [Benchmark]
    public async Task Compute()
    {
        await _service.ComputeAsync(_request, CancellationToken.None);
    }

    public static IEnumerable<EndToEndScenario> Scenarios()
    {
        return new[]
        {
            new EndToEndScenario("E2E_Ancestors_OrGroups", BuildAncestorsOrRequest),
            new EndToEndScenario("E2E_PropertyFilters_SlotDuration", BuildSlotDurationRequest)
        };
    }

    private static AvailabilityComputeRequest BuildAncestorsOrRequest(SchedulerDbContext db)
    {
        var doctor7 = FindResourceId(db, "DOC-7");
        var doctor8 = FindResourceId(db, "DOC-8");
        var room1 = FindResourceId(db, "ROOM-1");
        var room4 = FindResourceId(db, "ROOM-4");
        var monday = new DateOnly(2026, 1, 12);

        return new AvailabilityComputeRequest(
            monday,
            monday,
            Array.Empty<int>(),
            ResourceOrGroups: new[]
            {
                new[] { doctor7, doctor8 },
                new[] { room1, room4 }
            },
            IncludeResourceAncestors: true);
    }

    private static AvailabilityComputeRequest BuildSlotDurationRequest(SchedulerDbContext db)
    {
        var room3 = FindResourceId(db, "ROOM-3");
        var imaging = FindPropertyId(db, "RoomFeature", "Imaging");
        var friday = new DateOnly(2026, 1, 16);

        return new AvailabilityComputeRequest(
            friday,
            friday,
            new[] { room3 },
            PropertyIds: new[] { imaging },
            IncludePropertyDescendants: true,
            SlotDurationMinutes: 60,
            IncludeRemainderSlot: true);
    }

    private static int FindResourceId(SchedulerDbContext db, string code)
    {
        var resource = db.Resources.AsNoTracking().FirstOrDefault(item => item.Code == code);
        if (resource == null)
        {
            throw new InvalidOperationException($"Resource {code} not found.");
        }

        return resource.Id;
    }

    private static int FindPropertyId(SchedulerDbContext db, string key, string label)
    {
        var property = db.ResourceProperties.AsNoTracking()
            .FirstOrDefault(item => item.Key == key && item.Label == label);
        if (property == null)
        {
            throw new InvalidOperationException($"Property {key}:{label} not found.");
        }

        return property.Id;
    }

    private void SeedBenchmarkData()
    {
        if (_dbContext.Resources.Any())
        {
            return;
        }

        var now = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc);

        var siteType = new ResourceTypes { Key = "Site", Label = "Site", SortOrder = 1 };
        var floorType = new ResourceTypes { Key = "Floor", Label = "Floor", SortOrder = 2 };
        var roomType = new ResourceTypes { Key = "Room", Label = "Room", SortOrder = 3 };
        var doctorType = new ResourceTypes { Key = "Doctor", Label = "Doctor", SortOrder = 4 };
        _dbContext.ResourceTypes.AddRange(siteType, floorType, roomType, doctorType);
        _dbContext.SaveChanges();

        var siteA = new Resources { Code = "SITE-A", Name = "Site A", IsSchedulable = false, Capacity = 1, TypeId = siteType.Id, CreatedAtUtc = now };
        var siteB = new Resources { Code = "SITE-B", Name = "Site B", IsSchedulable = false, Capacity = 1, TypeId = siteType.Id, CreatedAtUtc = now };
        var floorA1 = new Resources { Code = "FLOOR-A1", Name = "Floor A1", IsSchedulable = false, Capacity = 1, TypeId = floorType.Id, CreatedAtUtc = now };
        var floorB1 = new Resources { Code = "FLOOR-B1", Name = "Floor B1", IsSchedulable = false, Capacity = 1, TypeId = floorType.Id, CreatedAtUtc = now };
        var room1 = new Resources { Code = "ROOM-1", Name = "Room 1", IsSchedulable = true, Capacity = 1, TypeId = roomType.Id, CreatedAtUtc = now };
        var room3 = new Resources { Code = "ROOM-3", Name = "Room 3", IsSchedulable = true, Capacity = 1, TypeId = roomType.Id, CreatedAtUtc = now };
        var room4 = new Resources { Code = "ROOM-4", Name = "Room 4", IsSchedulable = true, Capacity = 1, TypeId = roomType.Id, CreatedAtUtc = now };
        var doctor7 = new Resources { Code = "DOC-7", Name = "Doctor 7", IsSchedulable = true, Capacity = 1, TypeId = doctorType.Id, CreatedAtUtc = now };
        var doctor8 = new Resources { Code = "DOC-8", Name = "Doctor 8", IsSchedulable = true, Capacity = 1, TypeId = doctorType.Id, CreatedAtUtc = now };
        _dbContext.Resources.AddRange(siteA, siteB, floorA1, floorB1, room1, room3, room4, doctor7, doctor8);
        _dbContext.SaveChanges();

        _dbContext.ResourceRelations.AddRange(
            new ResourceRelations { ParentResourceId = siteA.Id, ChildResourceId = floorA1.Id, RelationType = "Contains" },
            new ResourceRelations { ParentResourceId = floorA1.Id, ChildResourceId = room1.Id, RelationType = "Contains" },
            new ResourceRelations { ParentResourceId = siteA.Id, ChildResourceId = room3.Id, RelationType = "Contains" },
            new ResourceRelations { ParentResourceId = siteB.Id, ChildResourceId = floorB1.Id, RelationType = "Contains" },
            new ResourceRelations { ParentResourceId = floorB1.Id, ChildResourceId = room4.Id, RelationType = "Contains" },
            new ResourceRelations { ParentResourceId = siteA.Id, ChildResourceId = doctor7.Id, RelationType = "WorksIn" },
            new ResourceRelations { ParentResourceId = siteA.Id, ChildResourceId = doctor8.Id, RelationType = "WorksIn" });
        _dbContext.SaveChanges();

        var specializationRoot = new ResourceProperties { Key = "Specialization", Label = "Specialization" };
        var cardiology = new ResourceProperties { Key = "Specialization", Label = "Cardiology", Parent = specializationRoot, SortOrder = 1 };
        var roomFeatureRoot = new ResourceProperties { Key = "RoomFeature", Label = "RoomFeature" };
        var imaging = new ResourceProperties { Key = "RoomFeature", Label = "Imaging", Parent = roomFeatureRoot, SortOrder = 1 };
        var oct = new ResourceProperties { Key = "RoomFeature", Label = "OCT", Parent = imaging, SortOrder = 1 };
        var locationRoot = new ResourceProperties { Key = "Location", Label = "Location" };
        var milan = new ResourceProperties { Key = "Location", Label = "Milan", Parent = locationRoot, SortOrder = 1 };
        var rome = new ResourceProperties { Key = "Location", Label = "Rome", Parent = locationRoot, SortOrder = 2 };
        var accreditationRoot = new ResourceProperties { Key = "Accreditation", Label = "Accreditation" };
        var iso = new ResourceProperties { Key = "Accreditation", Label = "ISO 9001", Parent = accreditationRoot, SortOrder = 1 };
        _dbContext.ResourceProperties.AddRange(
            specializationRoot, cardiology,
            roomFeatureRoot, imaging, oct,
            locationRoot, milan, rome,
            accreditationRoot, iso);
        _dbContext.SaveChanges();

        _dbContext.ResourceTypeProperties.AddRange(
            new ResourceTypeProperties { ResourceTypeId = siteType.Id, PropertyDefinitionId = locationRoot.Id },
            new ResourceTypeProperties { ResourceTypeId = siteType.Id, PropertyDefinitionId = accreditationRoot.Id },
            new ResourceTypeProperties { ResourceTypeId = roomType.Id, PropertyDefinitionId = roomFeatureRoot.Id },
            new ResourceTypeProperties { ResourceTypeId = doctorType.Id, PropertyDefinitionId = specializationRoot.Id });
        _dbContext.SaveChanges();

        _dbContext.ResourcePropertyLinks.AddRange(
            new ResourcePropertyLinks { ResourceId = siteA.Id, PropertyId = milan.Id },
            new ResourcePropertyLinks { ResourceId = siteA.Id, PropertyId = iso.Id },
            new ResourcePropertyLinks { ResourceId = siteB.Id, PropertyId = rome.Id },
            new ResourcePropertyLinks { ResourceId = room1.Id, PropertyId = oct.Id },
            new ResourcePropertyLinks { ResourceId = room3.Id, PropertyId = oct.Id },
            new ResourcePropertyLinks { ResourceId = room4.Id, PropertyId = oct.Id },
            new ResourcePropertyLinks { ResourceId = doctor7.Id, PropertyId = cardiology.Id },
            new ResourcePropertyLinks { ResourceId = doctor8.Id, PropertyId = cardiology.Id });
        _dbContext.SaveChanges();

        var monday = new DateOnly(2026, 1, 12);
        var friday = new DateOnly(2026, 1, 16);
        var rules = new List<Rules>
        {
            new Rules { Title = "Bench: Site A", Kind = (byte)RuleKind.SingleDate, IsExclude = false, SingleDateUtc = monday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(18, 0), CreatedAtUtc = now },
            new Rules { Title = "Bench: Floor A1", Kind = (byte)RuleKind.SingleDate, IsExclude = false, SingleDateUtc = monday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(18, 0), CreatedAtUtc = now },
            new Rules { Title = "Bench: Room 1", Kind = (byte)RuleKind.SingleDate, IsExclude = false, SingleDateUtc = monday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(18, 0), CreatedAtUtc = now },
            new Rules { Title = "Bench: Room 4", Kind = (byte)RuleKind.SingleDate, IsExclude = false, SingleDateUtc = monday, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(15, 0), CreatedAtUtc = now },
            new Rules { Title = "Bench: Doctor 7", Kind = (byte)RuleKind.SingleDate, IsExclude = false, SingleDateUtc = monday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(18, 0), CreatedAtUtc = now },
            new Rules { Title = "Bench: Doctor 8", Kind = (byte)RuleKind.SingleDate, IsExclude = false, SingleDateUtc = monday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(18, 0), CreatedAtUtc = now },
            new Rules { Title = "Bench: Room 3 short", Kind = (byte)RuleKind.SingleDate, IsExclude = false, SingleDateUtc = friday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 20), CreatedAtUtc = now },
            new Rules { Title = "Bench: Site A exclude", Kind = (byte)RuleKind.SingleDate, IsExclude = true, SingleDateUtc = monday, StartTime = new TimeOnly(15, 0), EndTime = new TimeOnly(16, 0), CreatedAtUtc = now }
        };
        _dbContext.Rules.AddRange(rules);
        _dbContext.SaveChanges();

        _dbContext.RuleResources.AddRange(
            new RuleResources { RuleId = rules[0].Id, ResourceId = siteA.Id },
            new RuleResources { RuleId = rules[1].Id, ResourceId = floorA1.Id },
            new RuleResources { RuleId = rules[2].Id, ResourceId = room1.Id },
            new RuleResources { RuleId = rules[3].Id, ResourceId = room4.Id },
            new RuleResources { RuleId = rules[4].Id, ResourceId = doctor7.Id },
            new RuleResources { RuleId = rules[5].Id, ResourceId = doctor8.Id },
            new RuleResources { RuleId = rules[6].Id, ResourceId = room3.Id },
            new RuleResources { RuleId = rules[7].Id, ResourceId = siteA.Id });
        _dbContext.SaveChanges();
    }

    public sealed record EndToEndScenario(string Name, Func<SchedulerDbContext, AvailabilityComputeRequest> BuildRequest)
    {
        public override string ToString() => Name;
    }

    private sealed class EndToEndBenchmarkConfig : ManualConfig
    {
        public EndToEndBenchmarkConfig()
        {
            AddColumn(StatisticColumn.P50, StatisticColumn.P95);
            AddExporter(MarkdownExporter.GitHub, CsvExporter.Default, HtmlExporter.Default);
        }
    }

    private sealed class BenchmarkPropertyRepository : IPropertyRepository
    {
        private readonly SchedulerDbContext _dbContext;

        public BenchmarkPropertyRepository(SchedulerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<ResourceProperties>> ExpandPropertySubtreeAsync(
            int propertyId,
            CancellationToken ct)
        {
            var properties = await _dbContext.ResourceProperties
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (properties.Count == 0)
            {
                return Array.Empty<ResourceProperties>();
            }

            var children = new Dictionary<int, List<ResourceProperties>>();
            for (var i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                if (property.ParentId == null)
                {
                    continue;
                }

                if (!children.TryGetValue(property.ParentId.Value, out var list))
                {
                    list = new List<ResourceProperties>();
                    children[property.ParentId.Value] = list;
                }

                list.Add(property);
            }

            var result = new List<ResourceProperties>();
            var stack = new Stack<int>();
            stack.Push(propertyId);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var node = properties.FirstOrDefault(item => item.Id == current);
                if (node == null)
                {
                    continue;
                }

                result.Add(node);
                if (!children.TryGetValue(current, out var directChildren))
                {
                    continue;
                }

                for (var i = 0; i < directChildren.Count; i++)
                {
                    stack.Push(directChildren[i].Id);
                }
            }

            return result;
        }

        public async Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
            IReadOnlyCollection<int> propertyIds,
            CancellationToken ct)
        {
            if (propertyIds.Count == 0)
            {
                return Array.Empty<int>();
            }

            var ids = await _dbContext.ResourcePropertyLinks
                .AsNoTracking()
                .Where(link => propertyIds.Contains(link.PropertyId))
                .Select(link => link.ResourceId)
                .Distinct()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return ids;
        }

        public async Task<IReadOnlyList<int>> GetResourceIdsByAllPropertiesAsync(
            IReadOnlyCollection<int> propertyIds,
            CancellationToken ct)
        {
            if (propertyIds.Count == 0)
            {
                return Array.Empty<int>();
            }

            var distinctIds = propertyIds.Distinct().ToList();
            var requiredCount = distinctIds.Count;
            if (requiredCount == 1)
            {
                return await GetResourceIdsByPropertiesAsync(distinctIds, ct).ConfigureAwait(false);
            }

            var matches = await _dbContext.ResourcePropertyLinks
                .AsNoTracking()
                .Where(link => distinctIds.Contains(link.PropertyId))
                .GroupBy(link => link.ResourceId)
                .Where(group => group.Select(link => link.PropertyId).Distinct().Count() == requiredCount)
                .Select(group => group.Key)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return matches;
        }
    }

}
