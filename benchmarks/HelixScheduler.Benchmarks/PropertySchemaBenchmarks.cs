using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Order;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.PropertySchema;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[Config(typeof(PropertySchemaBenchmarkConfig))]
public class PropertySchemaBenchmarks
{
    private PropertySchemaService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        _service = new PropertySchemaService(new DemoPropertySchemaQueryService());
    }

    [Benchmark]
    public Task<PropertySchemaResponse> GetSchema()
    {
        return _service.GetSchemaAsync(CancellationToken.None);
    }

    [Benchmark]
    public Task ValidatePropertyFilters()
    {
        return _service.ValidatePropertyFiltersAsync(
            resourceIds: [DemoPropertySchemaQueryService.Room1Id, DemoPropertySchemaQueryService.Room2Id],
            propertyIds: [DemoPropertySchemaQueryService.OctId, DemoPropertySchemaQueryService.MriId],
            CancellationToken.None);
    }

    [Benchmark]
    public Task ValidatePropertyFiltersForType()
    {
        return _service.ValidatePropertyFiltersForTypeAsync(
            resourceTypeId: 12,
            propertyIds: [DemoPropertySchemaQueryService.OctId, DemoPropertySchemaQueryService.MriId],
            CancellationToken.None);
    }

    private sealed class PropertySchemaBenchmarkConfig : ManualConfig
    {
        public PropertySchemaBenchmarkConfig()
        {
            AddColumn(StatisticColumn.P50, StatisticColumn.P95);
            AddExporter(MarkdownExporter.GitHub, CsvExporter.Default, HtmlExporter.Default);
        }
    }

    private sealed class DemoPropertySchemaQueryService : IPropertySchemaQueryService
    {
        public const int Room1Id = 11;
        public const int Room2Id = 12;

        public const int SpecializationRootId = 100;
        public const int OphthalmologyId = 101;
        public const int CardiologyId = 102;

        public const int RoomFeatureRootId = 110;
        public const int ImagingRootId = 111;
        public const int OctId = 112;
        public const int MriId = 113;
        public const int UltrasoundId = 114;

        public const int LocationRootId = 120;
        public const int LocationMilanId = 121;
        public const int LocationRomeId = 122;

        public const int AccreditationRootId = 130;
        public const int AccreditationIsoId = 131;

        private static readonly List<PropertySchemaNode> PropertyNodes =
        [
            new(SpecializationRootId, null, "Specialization", "Specialization", null),
            new(OphthalmologyId, SpecializationRootId, "Specialization", "Ophthalmology", 1),
            new(CardiologyId, SpecializationRootId, "Specialization", "Cardiology", 2),
            new(RoomFeatureRootId, null, "RoomFeature", "RoomFeature", null),
            new(ImagingRootId, RoomFeatureRootId, "RoomFeature", "Imaging", 1),
            new(OctId, ImagingRootId, "RoomFeature", "OCT", 1),
            new(MriId, ImagingRootId, "RoomFeature", "MRI", 2),
            new(UltrasoundId, ImagingRootId, "RoomFeature", "Ultrasound", 3),
            new(LocationRootId, null, "Location", "Location", null),
            new(LocationMilanId, LocationRootId, "Location", "Milan", 1),
            new(LocationRomeId, LocationRootId, "Location", "Rome", 2),
            new(AccreditationRootId, null, "Accreditation", "Accreditation", null),
            new(AccreditationIsoId, AccreditationRootId, "Accreditation", "ISO 9001", 1)
        ];

        private static readonly List<ResourceTypePropertyLink> TypeLinks =
        [
            new(13, SpecializationRootId),
            new(12, RoomFeatureRootId),
            new(10, LocationRootId),
            new(10, AccreditationRootId)
        ];

        private static readonly List<ResourceTypeAssignment> Assignments =
        [
            new(Room1Id, 12),
            new(Room2Id, 12)
        ];

        public Task<IReadOnlyList<PropertySchemaNode>> GetPropertyNodesAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<PropertySchemaNode>>(PropertyNodes);
        }

        public Task<IReadOnlyList<ResourceTypePropertyLink>> GetResourceTypePropertiesAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ResourceTypePropertyLink>>(TypeLinks);
        }

        public Task<IReadOnlyList<ResourceTypeAssignment>> GetResourceTypeAssignmentsAsync(
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ResourceTypeAssignment>>(
                Assignments.Where(item => resourceIds.Contains(item.ResourceId)).ToList());
        }
    }
}
