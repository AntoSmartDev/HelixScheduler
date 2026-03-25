# HelixScheduler.Core

`HelixScheduler.Core` is the canonical scheduling engine at the center of HelixScheduler.

It provides a deterministic, domain-agnostic model for computing availability from:
- structural availability rules
- normalized busy slots
- single-resource availability first
- multi-resource composition only after per-resource computation

## What The Package Contains

The package exposes the canonical core surface:
- `AvailabilityEngine`
- `AvailabilityQuery`
- `AvailabilityResult`
- `AvailabilityRule`
- `BusySlot`
- `ResourceDefinition`
- supporting model/value types required by the compute flow

## What The Package Does Not Contain

This package does not include:
- persistence or EF Core integration
- HTTP or WebApi adapters
- management-layer services
- domain-specific booking logic

Those concerns live in higher layers of the HelixScheduler solution.

## Design Constraints

`HelixScheduler.Core` is intentionally small and strict:
- UTC-only semantics
- deterministic output for identical inputs
- no dependency on database, HTTP, or application-domain code
- availability computed per resource first, then composed across resources

## Typical Usage

Use this package when you want to:
- compute availability directly in-process
- build your own adapters around the canonical engine
- keep scheduling logic separate from application-specific workflows

## Install

```powershell
dotnet add package HelixScheduler.Core
```

## Minimal Example

```csharp
using HelixScheduler.Core;

var engine = new AvailabilityEngine();

var query = new AvailabilityQuery(
    period: new DatePeriod(
        new DateOnly(2026, 3, 25),
        new DateOnly(2026, 3, 25)),
    requiredResourceIds: [1]);

var rules = new List<AvailabilityRule>
{
    new(
        id: 1,
        kind: RuleKind.SingleDate,
        isExclude: false,
        fromDate: null,
        toDate: null,
        singleDate: new DateOnly(2026, 3, 25),
        startTime: TimeSpan.FromHours(9),
        endTime: TimeSpan.FromHours(17),
        daysOfWeekMask: null,
        dayOfMonth: null,
        intervalDays: null,
        resourceId: 1)
};
var busySlots = new List<BusySlot>();
var inputs = new AvailabilityInputs(rules, busySlots);

AvailabilityResult result = engine.Compute(query, inputs);
```

The exact rule and busy-slot shapes depend on the scheduling model you build on top of the core.

For solution-level guidance, see the repository README. For additional core usage examples, see the
`HelixScheduler.Core.Tests` project in the main repo.
