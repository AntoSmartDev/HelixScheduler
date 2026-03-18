using HelixScheduler.Infrastructure.Persistence.Entities;

namespace HelixScheduler.Infrastructure.Persistence.Seed;

internal sealed record DemoSeedCatalog(
    DemoSeedResourceTypes Types,
    DemoSeedResources Resources,
    DemoSeedProperties Properties);

internal sealed record DemoSeedResourceTypes(
    ResourceTypes Site,
    ResourceTypes Floor,
    ResourceTypes Room,
    ResourceTypes Doctor);

internal sealed record DemoSeedResources(
    Resources SiteA,
    Resources SiteB,
    Resources FloorA1,
    Resources FloorB1,
    Resources Room1,
    Resources Room2,
    Resources Room3,
    Resources Room4,
    Resources Doctor7,
    Resources Doctor8,
    Resources Doctor9);

internal sealed record DemoSeedProperties(
    ResourceProperties SpecializationRoot,
    ResourceProperties RoomFeatureRoot,
    ResourceProperties LocationRoot,
    ResourceProperties AccreditationRoot,
    ResourceProperties Ophthalmology,
    ResourceProperties Retina,
    ResourceProperties Cardiology,
    ResourceProperties InterventionalCardiology,
    ResourceProperties Imaging,
    ResourceProperties Oct,
    ResourceProperties Mri,
    ResourceProperties Ultrasound,
    ResourceProperties Milan,
    ResourceProperties Rome,
    ResourceProperties Iso9001,
    ResourceProperties Soc2);
