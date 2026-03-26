using HelixScheduler.Application.Abstractions;

namespace HelixScheduler.Infrastructure.Persistence.Seed;

public sealed class DemoSeedService : IDemoSeedService
{
    private const int SeedVersion = 6;
    private readonly SchedulerDbContext _dbContext;
    private readonly IDemoScenarioStore _store;
    private readonly IClock _clock;
    private readonly DemoSeedCleanup _cleanup;
    private readonly DemoSeedCatalogBuilder _catalogBuilder;
    private readonly DemoSeedScheduleBuilder _scheduleBuilder;

    public DemoSeedService(
        SchedulerDbContext dbContext,
        IDemoScenarioStore store,
        IClock clock,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _store = store;
        _clock = clock;
        _cleanup = new DemoSeedCleanup(dbContext, tenantContext);
        _catalogBuilder = new DemoSeedCatalogBuilder(dbContext, tenantContext);
        _scheduleBuilder = new DemoSeedScheduleBuilder(dbContext, tenantContext);
    }

    public async Task EnsureSeedAsync(CancellationToken ct)
    {
        var state = await _store.GetAsync(ct).ConfigureAwait(false);
        if (state != null && state.SeedVersion == SeedVersion)
        {
            return;
        }

        var baseDate = state?.BaseDateUtc ?? ComputeBaseDateUtc(_clock.UtcNow);
        await ApplySeedAsync(baseDate, ct).ConfigureAwait(false);
    }

    public async Task ResetAsync(CancellationToken ct)
    {
        var baseDate = ComputeBaseDateUtc(_clock.UtcNow);
        await ApplySeedAsync(baseDate, ct).ConfigureAwait(false);
    }

    private async Task ApplySeedAsync(DateOnly baseDateUtc, CancellationToken ct)
    {
        var nowUtc = _clock.UtcNow;

        await _cleanup.CleanupAsync(ct).ConfigureAwait(false);
        var catalog = await _catalogBuilder.EnsureCatalogAsync(nowUtc, ct).ConfigureAwait(false);
        await _scheduleBuilder.EnsureScheduleAsync(catalog, baseDateUtc, nowUtc, ct).ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        var state = new DemoScenarioState(baseDateUtc, SeedVersion, nowUtc);
        await _store.SaveAsync(state, ct).ConfigureAwait(false);
    }

    private static DateOnly ComputeBaseDateUtc(DateTime utcNow)
    {
        var today = DateOnly.FromDateTime(utcNow);
        var diff = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-diff);
    }
}
