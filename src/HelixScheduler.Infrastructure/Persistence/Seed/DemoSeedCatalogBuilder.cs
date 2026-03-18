using HelixScheduler.Application.Abstractions;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Seed;

internal sealed class DemoSeedCatalogBuilder
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public DemoSeedCatalogBuilder(SchedulerDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<DemoSeedCatalog> EnsureCatalogAsync(DateTime nowUtc, CancellationToken ct)
    {
        var types = new DemoSeedResourceTypes(
            await EnsureResourceTypeAsync("Site", "Site", 1, ct).ConfigureAwait(false),
            await EnsureResourceTypeAsync("Floor", "Floor", 2, ct).ConfigureAwait(false),
            await EnsureResourceTypeAsync("Room", "Room", 3, ct).ConfigureAwait(false),
            await EnsureResourceTypeAsync("Doctor", "Doctor", 4, ct).ConfigureAwait(false));

        var resources = new DemoSeedResources(
            await EnsureResourceAsync("SITE-A", "Site A", 1, false, types.Site.Id, nowUtc, ct).ConfigureAwait(false),
            await EnsureResourceAsync("SITE-B", "Site B", 1, false, types.Site.Id, nowUtc, ct).ConfigureAwait(false),
            await EnsureResourceAsync("FLOOR-A1", "Floor A1", 1, false, types.Floor.Id, nowUtc, ct).ConfigureAwait(false),
            await EnsureResourceAsync("FLOOR-B1", "Floor B1", 1, false, types.Floor.Id, nowUtc, ct).ConfigureAwait(false),
            await EnsureResourceAsync("ROOM-1", "Room 1", 1, true, types.Room.Id, nowUtc, ct).ConfigureAwait(false),
            await EnsureResourceAsync("ROOM-2", "Room 2", 2, true, types.Room.Id, nowUtc, ct).ConfigureAwait(false),
            await EnsureResourceAsync("ROOM-3", "Room 3", 1, true, types.Room.Id, nowUtc, ct).ConfigureAwait(false),
            await EnsureResourceAsync("ROOM-4", "Room 4", 1, true, types.Room.Id, nowUtc, ct).ConfigureAwait(false),
            await EnsureResourceAsync("DOC-7", "Doctor 7", 1, true, types.Doctor.Id, nowUtc, ct).ConfigureAwait(false),
            await EnsureResourceAsync("DOC-8", "Doctor 8", 1, true, types.Doctor.Id, nowUtc, ct).ConfigureAwait(false),
            await EnsureResourceAsync("DOC-9", "Doctor 9", 1, true, types.Doctor.Id, nowUtc, ct).ConfigureAwait(false));

        await EnsureRelationAsync(resources.SiteA.Id, resources.FloorA1.Id, "Contains", ct).ConfigureAwait(false);
        await EnsureRelationAsync(resources.FloorA1.Id, resources.Room1.Id, "Contains", ct).ConfigureAwait(false);
        await EnsureRelationAsync(resources.FloorA1.Id, resources.Room2.Id, "Contains", ct).ConfigureAwait(false);
        await EnsureRelationAsync(resources.SiteA.Id, resources.Room3.Id, "Contains", ct).ConfigureAwait(false);
        await EnsureRelationAsync(resources.SiteB.Id, resources.FloorB1.Id, "Contains", ct).ConfigureAwait(false);
        await EnsureRelationAsync(resources.FloorB1.Id, resources.Room4.Id, "Contains", ct).ConfigureAwait(false);
        await EnsureRelationAsync(resources.SiteA.Id, resources.Doctor7.Id, "WorksIn", ct).ConfigureAwait(false);
        await EnsureRelationAsync(resources.SiteA.Id, resources.Doctor8.Id, "WorksIn", ct).ConfigureAwait(false);
        await EnsureRelationAsync(resources.SiteB.Id, resources.Doctor9.Id, "WorksIn", ct).ConfigureAwait(false);

        var properties = await EnsurePropertiesAsync(ct).ConfigureAwait(false);

        await EnsureResourceTypePropertyAsync(types.Doctor.Id, properties.SpecializationRoot.Id, ct).ConfigureAwait(false);
        await EnsureResourceTypePropertyAsync(types.Room.Id, properties.RoomFeatureRoot.Id, ct).ConfigureAwait(false);
        await EnsureResourceTypePropertyAsync(types.Site.Id, properties.LocationRoot.Id, ct).ConfigureAwait(false);
        await EnsureResourceTypePropertyAsync(types.Site.Id, properties.AccreditationRoot.Id, ct).ConfigureAwait(false);

        await EnsurePropertyLinkAsync(resources.Doctor7.Id, properties.Retina.Id, ct).ConfigureAwait(false);
        await EnsurePropertyLinkAsync(resources.Doctor8.Id, properties.Cardiology.Id, ct).ConfigureAwait(false);
        await EnsurePropertyLinkAsync(resources.Doctor9.Id, properties.InterventionalCardiology.Id, ct).ConfigureAwait(false);
        await EnsurePropertyLinkAsync(resources.Room1.Id, properties.Oct.Id, ct).ConfigureAwait(false);
        await EnsurePropertyLinkAsync(resources.Room2.Id, properties.Mri.Id, ct).ConfigureAwait(false);
        await EnsurePropertyLinkAsync(resources.Room3.Id, properties.Ultrasound.Id, ct).ConfigureAwait(false);
        await EnsurePropertyLinkAsync(resources.Room4.Id, properties.Oct.Id, ct).ConfigureAwait(false);
        await EnsurePropertyLinkAsync(resources.SiteA.Id, properties.Milan.Id, ct).ConfigureAwait(false);
        await EnsurePropertyLinkAsync(resources.SiteA.Id, properties.Iso9001.Id, ct).ConfigureAwait(false);
        await EnsurePropertyLinkAsync(resources.SiteA.Id, properties.Soc2.Id, ct).ConfigureAwait(false);
        await EnsurePropertyLinkAsync(resources.SiteB.Id, properties.Rome.Id, ct).ConfigureAwait(false);
        await EnsurePropertyLinkAsync(resources.SiteB.Id, properties.Iso9001.Id, ct).ConfigureAwait(false);

        return new DemoSeedCatalog(types, resources, properties);
    }

    private async Task<DemoSeedProperties> EnsurePropertiesAsync(CancellationToken ct)
    {
        var specializationRoot = await EnsurePropertyAsync("Specialization", "Specialization", null, null, ct).ConfigureAwait(false);
        var roomFeatureRoot = await EnsurePropertyAsync("RoomFeature", "RoomFeature", null, null, ct).ConfigureAwait(false);
        var locationRoot = await EnsurePropertyAsync("Location", "Location", null, null, ct).ConfigureAwait(false);
        var accreditationRoot = await EnsurePropertyAsync("Accreditation", "Accreditation", null, null, ct).ConfigureAwait(false);

        var ophthalmology = await EnsurePropertyAsync("Specialization", "Ophthalmology", specializationRoot.Id, 1, ct).ConfigureAwait(false);
        var retina = await EnsurePropertyAsync("Specialization", "Retina", ophthalmology.Id, 1, ct).ConfigureAwait(false);
        var cardiology = await EnsurePropertyAsync("Specialization", "Cardiology", specializationRoot.Id, 2, ct).ConfigureAwait(false);
        var interventionalCardiology = await EnsurePropertyAsync(
            "Specialization",
            "Interventional Cardiology",
            cardiology.Id,
            1,
            ct).ConfigureAwait(false);
        var imaging = await EnsurePropertyAsync("RoomFeature", "Imaging", roomFeatureRoot.Id, 1, ct).ConfigureAwait(false);
        var oct = await EnsurePropertyAsync("RoomFeature", "OCT", imaging.Id, 1, ct).ConfigureAwait(false);
        var mri = await EnsurePropertyAsync("RoomFeature", "MRI", imaging.Id, 2, ct).ConfigureAwait(false);
        var ultrasound = await EnsurePropertyAsync("RoomFeature", "Ultrasound", imaging.Id, 3, ct).ConfigureAwait(false);
        var milan = await EnsurePropertyAsync("Location", "Milan", locationRoot.Id, 1, ct).ConfigureAwait(false);
        var rome = await EnsurePropertyAsync("Location", "Rome", locationRoot.Id, 2, ct).ConfigureAwait(false);
        var iso9001 = await EnsurePropertyAsync("Accreditation", "ISO 9001", accreditationRoot.Id, 1, ct).ConfigureAwait(false);
        var soc2 = await EnsurePropertyAsync("Accreditation", "SOC2", accreditationRoot.Id, 2, ct).ConfigureAwait(false);

        return new DemoSeedProperties(
            specializationRoot,
            roomFeatureRoot,
            locationRoot,
            accreditationRoot,
            ophthalmology,
            retina,
            cardiology,
            interventionalCardiology,
            imaging,
            oct,
            mri,
            ultrasound,
            milan,
            rome,
            iso9001,
            soc2);
    }

    private async Task<Resources> EnsureResourceAsync(
        string code,
        string name,
        int capacity,
        bool isSchedulable,
        int typeId,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var resource = await _dbContext.Resources
            .FirstOrDefaultAsync(item => item.Name == name, ct)
            .ConfigureAwait(false);

        if (resource != null)
        {
            resource.Code = code;
            resource.IsSchedulable = isSchedulable;
            resource.Capacity = capacity < 1 ? 1 : capacity;
            resource.TypeId = typeId;
            return resource;
        }

        resource = new Resources
        {
            TenantId = _tenantContext.TenantId,
            Code = code,
            Name = name,
            IsSchedulable = isSchedulable,
            Capacity = capacity < 1 ? 1 : capacity,
            TypeId = typeId,
            CreatedAtUtc = nowUtc
        };

        _dbContext.Resources.Add(resource);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return resource;
    }

    private async Task<ResourceTypes> EnsureResourceTypeAsync(
        string key,
        string label,
        int? sortOrder,
        CancellationToken ct)
    {
        var type = await _dbContext.ResourceTypes
            .FirstOrDefaultAsync(item => item.Key == key, ct)
            .ConfigureAwait(false);

        if (type != null)
        {
            type.Label = label;
            type.SortOrder = sortOrder;
            return type;
        }

        type = new ResourceTypes
        {
            TenantId = _tenantContext.TenantId,
            Key = key,
            Label = label,
            SortOrder = sortOrder
        };

        _dbContext.ResourceTypes.Add(type);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return type;
    }

    private async Task EnsureResourceTypePropertyAsync(
        int resourceTypeId,
        int propertyDefinitionId,
        CancellationToken ct)
    {
        var exists = await _dbContext.ResourceTypeProperties.AnyAsync(
            link => link.ResourceTypeId == resourceTypeId && link.PropertyDefinitionId == propertyDefinitionId,
            ct).ConfigureAwait(false);

        if (!exists)
        {
            _dbContext.ResourceTypeProperties.Add(new ResourceTypeProperties
            {
                TenantId = _tenantContext.TenantId,
                ResourceTypeId = resourceTypeId,
                PropertyDefinitionId = propertyDefinitionId
            });
        }
    }

    private async Task EnsureRelationAsync(
        int parentId,
        int childId,
        string relationType,
        CancellationToken ct)
    {
        var exists = await _dbContext.ResourceRelations.AnyAsync(
            relation => relation.ParentResourceId == parentId
                        && relation.ChildResourceId == childId
                        && relation.RelationType == relationType,
            ct).ConfigureAwait(false);

        if (!exists)
        {
            _dbContext.ResourceRelations.Add(new ResourceRelations
            {
                TenantId = _tenantContext.TenantId,
                ParentResourceId = parentId,
                ChildResourceId = childId,
                RelationType = relationType
            });
        }
    }

    private async Task<ResourceProperties> EnsurePropertyAsync(
        string key,
        string label,
        int? parentId,
        int? sortOrder,
        CancellationToken ct)
    {
        var property = await _dbContext.ResourceProperties
            .FirstOrDefaultAsync(item => item.Key == key && item.Label == label && item.ParentId == parentId, ct)
            .ConfigureAwait(false);

        if (property != null)
        {
            property.SortOrder = sortOrder;
            return property;
        }

        property = new ResourceProperties
        {
            TenantId = _tenantContext.TenantId,
            Key = key,
            Label = label,
            ParentId = parentId,
            SortOrder = sortOrder
        };

        _dbContext.ResourceProperties.Add(property);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return property;
    }

    private async Task EnsurePropertyLinkAsync(int resourceId, int propertyId, CancellationToken ct)
    {
        var exists = await _dbContext.ResourcePropertyLinks.AnyAsync(
            link => link.ResourceId == resourceId && link.PropertyId == propertyId,
            ct).ConfigureAwait(false);

        if (!exists)
        {
            _dbContext.ResourcePropertyLinks.Add(new ResourcePropertyLinks
            {
                TenantId = _tenantContext.TenantId,
                ResourceId = resourceId,
                PropertyId = propertyId
            });
        }
    }
}
