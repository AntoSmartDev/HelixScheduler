# HelixScheduler

High-performance scheduling engine for multi-resource availability queries (doctor/room/equipment) over day/week horizons.

## Key ideas
- Scheduler Core is domain-agnostic and computes availability from normalized intervals.
- Domain adapters translate domain events into negative/busy intervals.
- Performance-first and predictable memory usage.

## Getting started
- Requirements: .NET (versione da definire)
- Build: `dotnet build`
- Test: `dotnet test`

## Documentation
- Project overview: docs/context/PROJECT.md
- Architecture: docs/context/ARCHITECTURE.md
- Domain model: docs/context/DOMAIN.md
- Performance: docs/context/PERFORMANCE.md

## Contributing
See CONTRIBUTING.md