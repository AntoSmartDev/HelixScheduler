using HelixScheduler.Application.Demo;

namespace HelixScheduler.Infrastructure.Startup;

internal sealed class DemoSeedInitializer : IDemoSeedInitializer
{
    private readonly IDemoSeedService _demoSeedService;

    public DemoSeedInitializer(IDemoSeedService demoSeedService)
    {
        _demoSeedService = demoSeedService;
    }

    public async Task EnsureSeedAsync(CancellationToken ct)
    {
        await _demoSeedService.EnsureSeedAsync(ct).ConfigureAwait(false);
    }
}
