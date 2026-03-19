using BenchmarkDotNet.Running;

BenchmarkSwitcher
    .FromTypes(new[]
    {
        typeof(AvailabilityBenchmarks),
        typeof(AvailabilityOptimizationBenchmarks),
        typeof(CapacityHotPathBenchmarks),
        typeof(AncestorExpansionBenchmarks),
        typeof(AncestorFilterBenchmarks),
        typeof(PropertySchemaBenchmarks),
        typeof(ApplicationBenchmarks),
        typeof(EndToEndBenchmarks)
    })
    .Run(args);
