# T320 - Capacity v1

## Context
Add per-resource capacity to availability while keeping the v1 engine deterministic
and compatible with OR groups.

## Decisions
- Capacity is per-resource, stored in DB metadata, default 1.
- Busy events consume 1 unit each; negative rules do not consume capacity.
- Availability blocks only segments where occupancy >= capacity.
- Fast path for capacity=1 preserves previous behavior.

## Scope
- Data model + migration for Resource.Capacity with constraint >= 1.
- Availability engine v1 uses capacity to derive busy blocks.
- Repositories and application service supply capacities from DB.
- Tests cover capacity=1, >1, and OR group behavior.

## Out of scope
- Assignment or booking decisions.
- Weighted busy events.
- Explainability v2 for capacity.
