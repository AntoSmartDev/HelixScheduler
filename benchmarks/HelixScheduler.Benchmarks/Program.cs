using BenchmarkDotNet.Running;

BenchmarkSwitcher
    .FromTypes(new[]
    {
        typeof(AvailabilityBenchmarks),
        typeof(ApplicationBenchmarks),
        typeof(EndToEndBenchmarks)
    })
    .Run(args);
