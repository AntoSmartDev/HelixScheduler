# HelixScheduler

HelixScheduler is a domain-agnostic scheduling engine for multi-resource availability.

## Key ideas
- availability = positive - negative
- per-resource computation, intersection last
- core runs in UTC only

## Domain = Core
The domain model of the engine lives in `HelixScheduler.Core` (model + invariants + deterministic algorithm).
It has no EF/HTTP dependencies and remains application-agnostic.

## DaysOfWeekMask
Weekly rules use a bitmask for active days (System.DayOfWeek):
- 1 = Sunday, 2 = Monday, 4 = Tuesday, 8 = Wednesday
- 16 = Thursday, 32 = Friday, 64 = Saturday
Example: 10 = 2 + 8 = Monday + Wednesday.

Why a bitmask:
- compact storage (single int)
- fast filtering (bitwise AND)
- simple serialization/interoperability

See `docs/context` for the canonical model.

## Quickstart (SQL Server)
Set configuration:
```
HelixScheduler__DatabaseProvider=SqlServer
ConnectionStrings__SchedulerDb=Server=.\SQLEXPRESS;Database=HelixScheduler;Trusted_Connection=True;TrustServerCertificate=True;
```
Start the API:
```
dotnet run --project src/HelixScheduler.WebApi
```
Start the demo UI:
```
dotnet run --project samples/HelixScheduler.DemoWeb
```
Open the demo:
```
https://localhost:7040
```

The API seeds a demo scenario relative to install (BaseDateUtc persisted). Reset is dev-only:
```
POST /api/demo/reset
```

Note: SQLite support is temporarily disabled.

## Recent updates
- DemoWeb waits for API readiness (health check + retry/backoff).
- WebApi exposes `/health` with status/utc/version and has a simple landing page.

## WebApi endpoints
- `POST /api/availability/compute` (canonical)
- `GET /api/availability/slots` (legacy querystring)
- `POST /api/demo/summary` (rules + busy slots for demo UI)
- `POST /api/demo/reset` (Development only)
- `GET /api/catalog/resources`

See `API_EXAMPLES.md` for ready-to-run requests.
Primer: ancestorFilters support `matchMode` (or/and), `scope` (anyAncestor/directParent/nearestOfType), and `matchAllAncestors` for strict matching.

Example: include resource ancestors (per-group mode by default)
```json
POST /api/availability/compute
{
  "fromDate": "2026-01-06",
  "toDate": "2026-01-06",
  "requiredResourceIds": [2,3],
  "includeResourceAncestors": true,
  "ancestorRelationTypes": ["Contains"],
  "ancestorMode": "perGroup"
}
```
Defaults:
- If includeResourceAncestors is false, ancestorMode/ancestorRelationTypes are ignored.
- If ancestorMode is omitted, it defaults to perGroup.
- If ancestorRelationTypes is omitted or empty, all relation types are considered.

Example: GET slots with ancestors
```
GET /api/availability/slots?fromDate=2026-01-06&toDate=2026-01-06&resourceIds=2,3&includeResourceAncestors=true&ancestorMode=perGroup&ancestorRelationTypes=Contains
```

Example: chunked slots (compute only)
```json
POST /api/availability/compute
{
  "fromDate": "2026-03-09",
  "toDate": "2026-03-09",
  "requiredResourceIds": [5,4],
  "slotDurationMinutes": 60,
  "includeRemainderSlot": false
}
```

## DemoWeb
The read-only UI shows:
- weekly availability calendar
- rule list and busy slots
- explain toggle (if enabled)
