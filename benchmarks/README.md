# Benchmarks

Run all benchmarks:
```
dotnet run -c Release --project benchmarks/HelixScheduler.Benchmarks
```

Run a single benchmark class:
```
dotnet run -c Release --project benchmarks/HelixScheduler.Benchmarks -- --filter *AvailabilityBenchmarks*
dotnet run -c Release --project benchmarks/HelixScheduler.Benchmarks -- --filter *ApplicationBenchmarks*
dotnet run -c Release --project benchmarks/HelixScheduler.Benchmarks -- --filter *EndToEndBenchmarks*
```

Notes:
- Core benchmarks use purely in-memory data and exercise the AvailabilityEngine.
- Application benchmarks include filters, ancestors, and slot duration logic with in-memory data sources.
- End-to-end benchmarks use EF Core InMemory plus demo seed for a full pipeline sanity check.
