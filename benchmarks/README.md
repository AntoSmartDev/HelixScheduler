# Benchmarks

Run all benchmarks:
```
dotnet run -c Release --project benchmarks/HelixScheduler.Benchmarks
```

Run a single benchmark class:
```
dotnet run -c Release --project benchmarks/HelixScheduler.Benchmarks -- --filter *AvailabilityBenchmarks*
dotnet run -c Release --project benchmarks/HelixScheduler.Benchmarks -- --filter *AvailabilityOptimizationBenchmarks*
dotnet run -c Release --project benchmarks/HelixScheduler.Benchmarks -- --filter *ApplicationBenchmarks*
dotnet run -c Release --project benchmarks/HelixScheduler.Benchmarks -- --filter *EndToEndBenchmarks*
```

`AvailabilityOptimizationBenchmarks` adds focused comparisons for the availability performance work:
- 20 resources fixed
- `RuleCount`: 100 or 300
- `BusyCount`: 200 or 500
- core comparison:
  - baseline scan rules/busy + old subtract
  - grouped rules/busy + old subtract
  - current production path
- application comparison:
  - OR-only baseline vs optimized reuse
  - ancestor `perGroup` baseline vs optimized reuse
- subtract comparison:
  - old subtract vs current subtract

Engine call counts are encoded in the benchmark descriptions for the application paths:
- baseline OR-only: 20 `_engine.Compute(...)` calls
- baseline ancestor `perGroup`: 20 `_engine.Compute(...)` calls
- optimized paths: 1 per-resource build, 0 `_engine.Compute(...)` calls

Notes:
- Core benchmarks use purely in-memory data and exercise the AvailabilityEngine.
- Optimization benchmarks compare baseline helper implementations against the current production path.
- Application benchmarks include filters, ancestors, and slot duration logic with in-memory data sources.
- End-to-end benchmarks use EF Core InMemory plus demo seed for a full pipeline sanity check.
