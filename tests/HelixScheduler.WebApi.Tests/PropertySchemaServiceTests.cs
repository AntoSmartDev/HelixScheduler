using HelixScheduler.Application.Availability;
using HelixScheduler.Application.PropertySchema;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class PropertySchemaServiceTests
{
    private readonly PropertySchemaService _service = new(new FakePropertySchemaQueryService());

    [Fact]
    public async Task ValidatePropertyFiltersForType_Allows_Multiple_Properties_From_Same_Definition()
    {
        await _service.ValidatePropertyFiltersForTypeAsync(
            resourceTypeId: 1,
            propertyIds: new[] { 11, 12 },
            CancellationToken.None);
    }

    [Fact]
    public async Task ValidatePropertyFilters_Throws_For_Unknown_Property()
    {
        var error = await Assert.ThrowsAsync<AvailabilityRequestException>(() =>
            _service.ValidatePropertyFiltersAsync(
                resourceIds: new[] { 100 },
                propertyIds: new[] { 999 },
                CancellationToken.None));

        Assert.Contains("unknown id 999", error.Message);
    }

    [Fact]
    public async Task ValidatePropertyFilters_Throws_For_Incompatible_Type()
    {
        var error = await Assert.ThrowsAsync<AvailabilityRequestException>(() =>
            _service.ValidatePropertyFiltersAsync(
                resourceIds: new[] { 200 },
                propertyIds: new[] { 11 },
                CancellationToken.None));

        Assert.Contains("not compatible with resource type 3", error.Message);
    }

    private sealed class FakePropertySchemaQueryService : IPropertySchemaQueryService
    {
        private static readonly List<PropertySchemaNode> PropertyNodes =
        [
            new(10, null, "Location", "Location", null),
            new(11, 10, "Location", "Milan", 1),
            new(12, 10, "Location", "Rome", 2),
            new(20, null, "FloorFeature", "FloorFeature", null),
            new(21, 20, "FloorFeature", "Sterile", 1)
        ];

        private static readonly List<ResourceTypePropertyLink> TypeLinks =
        [
            new(1, 10),
            new(3, 20)
        ];

        private static readonly List<ResourceTypeAssignment> Assignments =
        [
            new(100, 1),
            new(200, 3)
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
