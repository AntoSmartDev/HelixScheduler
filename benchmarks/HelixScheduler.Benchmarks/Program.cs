using BenchmarkDotNet.Running;

BenchmarkSwitcher
    .FromTypes(new[]
    {
        typeof(AvailabilityBenchmarks),
        typeof(AvailabilityOptimizationBenchmarks),
        typeof(CapacityHotPathBenchmarks),
        typeof(AncestorExpansionBenchmarks),
        typeof(ApplicationBenchmarks),
        typeof(EndToEndBenchmarks)
    })
    .Run(args);
