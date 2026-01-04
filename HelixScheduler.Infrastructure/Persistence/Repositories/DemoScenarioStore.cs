using HelixScheduler.Application.Demo;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public sealed class DemoScenarioStore : IDemoScenarioStore
{
    private readonly SchedulerDbContext _dbContext;

    public DemoScenarioStore(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DemoScenarioState?> GetAsync(CancellationToken ct)
    {
        var state = await _dbContext.DemoScenarioStates
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (state == null)
        {
            return null;
        }

        return new DemoScenarioState(
            DateOnly.FromDateTime(state.BaseDateUtc),
            state.SeedVersion,
            state.UpdatedAtUtc);
    }

    public async Task SaveAsync(DemoScenarioState state, CancellationToken ct)
    {
        var entity = await _dbContext.DemoScenarioStates
            .OrderBy(item => item.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (entity == null)
        {
            entity = new DemoScenarioStates();
            _dbContext.DemoScenarioStates.Add(entity);
        }

        entity.BaseDateUtc = DateTime.SpecifyKind(state.BaseDateUtc.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        entity.SeedVersion = state.SeedVersion;
        entity.UpdatedAtUtc = state.UpdatedAtUtc;

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
