using HelixScheduler.Infrastructure.Persistence.Configurations;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence;

public sealed class SchedulerDbContext : DbContext
{
    public SchedulerDbContext(DbContextOptions<SchedulerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Resources> Resources => Set<Resources>();
    public DbSet<ResourceTypes> ResourceTypes => Set<ResourceTypes>();
    public DbSet<ResourceRelations> ResourceRelations => Set<ResourceRelations>();
    public DbSet<ResourceProperties> ResourceProperties => Set<ResourceProperties>();
    public DbSet<ResourcePropertyLinks> ResourcePropertyLinks => Set<ResourcePropertyLinks>();
    public DbSet<ResourceTypeProperties> ResourceTypeProperties => Set<ResourceTypeProperties>();
    public DbSet<Rules> Rules => Set<Rules>();
    public DbSet<RuleResources> RuleResources => Set<RuleResources>();
    public DbSet<BusyEvents> BusyEvents => Set<BusyEvents>();
    public DbSet<BusyEventResources> BusyEventResources => Set<BusyEventResources>();
    public DbSet<DemoScenarioStates> DemoScenarioStates => Set<DemoScenarioStates>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ResourcesConfiguration());
        modelBuilder.ApplyConfiguration(new ResourceTypesConfiguration());
        modelBuilder.ApplyConfiguration(new ResourceRelationsConfiguration());
        modelBuilder.ApplyConfiguration(new ResourcePropertiesConfiguration());
        modelBuilder.ApplyConfiguration(new ResourcePropertyLinksConfiguration());
        modelBuilder.ApplyConfiguration(new ResourceTypePropertiesConfiguration());
        modelBuilder.ApplyConfiguration(new RulesConfiguration());
        modelBuilder.ApplyConfiguration(new RuleResourcesConfiguration());
        modelBuilder.ApplyConfiguration(new BusyEventsConfiguration());
        modelBuilder.ApplyConfiguration(new BusyEventResourcesConfiguration());
        modelBuilder.ApplyConfiguration(new DemoScenarioStatesConfiguration());
    }
}
