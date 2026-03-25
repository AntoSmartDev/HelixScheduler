# HelixScheduler

**Deterministic scheduling and availability engine for real-world resource planning on modern .NET**

HelixScheduler is a domain-agnostic deterministic scheduling and availability engine for resource planning systems. It computes real availability across complex rules, resource hierarchies, capacity constraints, busy events, and structural filters.

It is designed to be the logical core of planning systems: the place where availability is defined once and computed correctly.

Rendering a calendar is easy. Computing what can actually be shown is not.

Ideal for systems that need reliable availability computation for multi-resource scheduling, appointment planning, facility planning, or other constraint-aware planning scenarios.

---

# What Is HelixScheduler

HelixScheduler is a core scheduling engine designed to:

- model resource availability
- apply structural rules and exclusions
- consider unavailability and real-world busy slots
- combine required and alternative resources into coherent availability results
- return truly usable time slots
- apply grouped property filters with OR-within-group and AND-between-group semantics
- evaluate ancestor-based structural constraints
- support explainable availability results

Its purpose is to solve, in a coherent and explainable way, the most critical part of scheduling and booking systems:

> computing correctly when a combination of resources is actually available.

Availability is computed per resource first, and only then combined across the requested resources.

This keeps the model explicit and predictable, and avoids treating multi-resource bookings as opaque monolithic events.

---


## Determinism

With the same inputs - rules, unavailability, busy slots, properties, and calculation options - the result is always identical.

The engine contains no heuristics, randomness, or implicit behavior. Computation is fully deterministic and verifiable.

This makes HelixScheduler suitable for:

- regulated environments
- critical systems
- contexts where reproducibility is essential

---

# Why HelixScheduler Exists

In real systems, availability depends on many entities:

- people
- spaces
- equipment
- facilities
- organizational constraints
- properties and qualifications

Implementing these rules directly on calendar events quickly leads to:

- duplication
- inconsistencies
- conflicts that are hard to detect
- non-scalable models

HelixScheduler separates availability logic from user interfaces and booking management.

A key design choice is the separation between structural availability and operational occupancy.

Structural availability is expressed through rules and exclusions.
Operational occupancy is expressed through normalized busy slots derived from real bookings or domain events.

This keeps the engine compact, deterministic, and domain-agnostic while still reflecting real usage.

The engine computes. The application decides how to use the result.
  
---

# Why Adopt HelixScheduler

HelixScheduler is useful when a system needs to:

- compute real availability instead of rendering naive calendar openings
- combine structural rules and real operational occupancy in one deterministic model
- support multi-resource scheduling without opaque event coupling
- apply structural constraints from organizational hierarchies and property filters
- explain availability decisions in a way that operators and developers can verify
  
---

## Model Scalability

HelixScheduler does not pre-materialize events over time.

It does not generate future calendars or expand rules into persisted event lists.

The model relies on:

- a small set of structural rules
- normalized intervals
- dynamic busy slots

The number of rules typically remains limited and stable over time, even with many bookings.

Computation happens on demand, for an explicit time range.

This avoids:

- data explosions
- pre-generation of millions of records
- complex synchronization across duplicated events

The result is a system that is:

- lighter
- more coherent
- more predictable

This keeps the scheduling model compact even when operational booking volume grows.

---

# Core Concepts

## Resources

Everything is a resource:

- doctor
- room
- device
- site
- department
- machine
- team

Resources can be organized in hierarchical relations:

- Site → Room
- Department → Doctor
- Facility → Clinic

You can request that the calculation also accounts for ancestor availability (`includeResourceAncestors`).

This enables proper modeling of:

- site closures
- organizational blocks
- structural constraints

Without duplicating unavailability on child resources.

This is especially valuable in organizational models where structural constraints live above the schedulable resource itself.

---

## Rules

Rules define structural availability and unavailability.

Supported rule shapes include:

- recurring weekly rules
- single-date rules
- date-range rules
- monthly rules
- repeating interval-based rules

This allows the engine to model not only weekly business calendars, but also more irregular recurring availability patterns that often appear in real operations.

Rules may also be open-ended or bounded by explicit date ranges.

Examples include:

- every first Monday of the month
- until a certain date
- no end date

Rules can be positive or negative, allowing the engine to model both structural availability and structural blocks without materializing future events.

Recurrence is optional: it is a feature, not a requirement.

---

## Unavailability

These fully block a resource.

Examples:

- vacations
- site closure
- maintenance

An unavailability is equivalent to capacity = 0 for that interval.

---

## Busy Slots

In real systems, busy slots derive from domain bookings and can be projected or sent to the engine as normalized input at calculation time.

They represent real resource usage.

This keeps domain events outside the engine while preserving a deterministic and normalized computation model.

A confirmed booking generates a busy slot.

Busy slots:

- consume capacity
- can coexist if capacity > 1
- prevent invalid overlaps

A busy slot can involve multiple resources at the same time.

For example, a visit requiring both a doctor and a room creates a coherent busy slot on both resources, ensuring the constraint is enforced atomically in the availability calculation.

---

## Capacity

Each resource can have a capacity.

Examples:

- doctor → 1
- mobile ultrasound → 1
- laboratory → 3
- classroom → 20

Availability is computed as:

Effective availability =
Availability from rules
- unavailability
- busy slots (up to capacity)

---

## Properties

Resources can have properties organized in categories and hierarchies.

Example:

Diagnostics
→ Ultrasound
→ X-ray
→ CT

With `includePropertyDescendants` you can filter by a category and automatically include all specializations.

Property filters can also be grouped to express richer structural constraints.

This allows scenarios such as:

- OR within a group (`Milan OR Rome`)
- AND between groups (`(Milan OR Rome) AND (ISO9001 OR SOC2)`)

Ancestor-aware property filters can also be applied when constraints live on the organizational context rather than on the schedulable resource itself.

---

# Real-World Problems Solved

## Visit with doctor + room

A visit requires:

- a doctor
- a room

A slot exists only if both are available in the same interval (AND).

---

## Shared mobile equipment

A visit also requires a mobile ultrasound.

- capacity = 1
- shared across clinics

The slot exists only if:

doctor AND room AND ultrasound are available.

No double booking.

---

## Multiple equivalent rooms

There are multiple equivalent clinics.

It is enough that at least one room is available.

This is modeled as OR across alternative resources.

---

## Site closed for holidays

A room belongs to a site.

If the site is closed:

- no need to add unavailability on every room
- it is enough to set it on the site

With `includeResourceAncestors`, the constraint propagates automatically.

---

## Filtering by site characteristics

A visit requires:

- suitable room
- accredited site
- site in a specific area

Properties live on the site, not the room.

The engine can select only resources whose organizational context satisfies constraints, avoiding duplication.

---

## Filtering with grouped property constraints

A request may require a resource that belongs to:

- a site in Milan or Rome
- and an accredited site with ISO 9001 or SOC2

This is modeled through grouped property filters with OR semantics inside each group and AND semantics across groups.

The result is a precise structural selection before availability is computed.

---

# Querying Availability

Availability is requested by providing:

- time range (date range, always UTC, date-only)
- slot duration (`slotDurationMinutes`)
- required resources and optional OR groups of alternative resources
- grouped property filters with descendant-aware matching
- ancestor filters
- optional explainability output

The engine returns coherent slots.

---

## Slot duration and granularity

Availability is not returned as a continuous, indistinct interval.

Each request specifies:

- time range
- slot duration
- optional handling of remainder slot (`includeRemainderSlot`)

The engine splits the requested range into coherent windows and checks actual availability for each.

Including remainder slots is an explicit and optional decision left to the consumer.

---

## Explainability

The model clearly separates:

- rules
- unavailability
- busy slots
- filters

This makes it always possible to explain why a slot is available or not.

Availability is the result of an explicit combination of traceable elements.

Each slot derives from an explicit set of rules and negative intervals (unavailability and busy slots) that can be verified.

This is especially useful in systems where operators need to understand whether a slot was enabled by structural rules, removed by exclusions, or blocked by busy usage.

---

# Multi-tenant

HelixScheduler supports multi-tenant mode with data isolation at the Infrastructure layer.

Features:

- Tenants table with default seed
- Tenant identification via HTTP header (`X-Tenant`, `X-Helix-Tenant`)
- Automatic fallback to tenant `default`
- 404 response if the requested tenant does not exist
- EF Core global query filters for data isolation (row-level isolation via tenantId)
- No changes to the engine core

Each tenant has:

- its own resources
- its own rules
- its own unavailability
- its own busy slots
- isolated properties and relations

The engine remains deterministic and tenant-neutral.

---

# Architecture

- Core → pure deterministic engine
- Application → orchestration
- Infrastructure → persistence and isolation
- WebApi → HTTP exposure
- DemoWeb → demo interface

The Core is independent from databases, HTTP, or external frameworks.

The engine can be fully tested in-memory, without a database or WebApi, enabling deterministic and reproducible unit tests.

---

# Technology Stack

HelixScheduler is built with:

- .NET 10
- C#
- Entity Framework Core
- ASP.NET Core Web API

Technical characteristics:

- Core completely independent of the framework
- Project-based architecture (Core / Application / Infrastructure / WebApi)
- SQL Server runtime support
- Compatible with cross-platform environments (.NET runtime)

The engine can be used:

- embedded in .NET applications
- via HTTP WebApi
- in multi-tenant scenarios

---

# Quickstart

## Prerequisites
- .NET 10 SDK
- SQL Server (current runtime database)

## Management Layer

HelixScheduler exposes two official product surfaces:

- compute layer
- management layer

The compute layer answers availability questions.
The management layer governs the scheduler model that the compute layer consumes.

Management documentation:

- [Management Overview](docs/management/overview.md)
- [Management Quickstart](docs/management/quickstart.md)
- [Management WebApi](docs/management/webapi.md)
- [Management Examples](docs/management/examples.md)

OpenAPI is available at `/openapi/v1.json` when enabled. By default it is exposed in
`Development`; in other environments set `OpenApi:Expose=true`.

## Database Setup
The current runtime setup uses SQL Server. Configure the connection string in `appsettings.json` or environment variables.

Example `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "SchedulerDb": "Server=.\\SQLEXPRESS;Database=HelixScheduler;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

## Migrations 

### 1. Apply database migrations (create/update the database schema)

```bash
dotnet ef database update --project src/HelixScheduler.Infrastructure --startup-project src/HelixScheduler.WebApi
```

This command creates or updates the database schema using EF Core migrations.
At this stage, the tables are created but contain no data.

---

### 2. Run the WebApi (demo seed runs automatically)

```bash
dotnet run --project src/HelixScheduler.WebApi
```

On startup (in non-`Testing` environments), the WebApi executes the demo seed and populates:

- Resources
- Scheduling rules
- Busy intervals
- Demo scenario state (relative to the first run date)

> The demo seed is intended for development and demonstration purposes.
> Disable or replace it for production usage.

---

### 3. Run the DemoWeb UI

Open a second terminal and run:

```bash
dotnet run --project samples/HelixScheduler.DemoWeb
```

The DemoWeb project is a read-only UI that calls the WebApi and renders availability results.

---

### Running both projects in Visual Studio

If using Visual Studio:

1. Right click the solution → **Set Startup Projects...**
2. Select **Multiple startup projects**
3. Set:
   - `HelixScheduler.WebApi` → **Start**
   - `HelixScheduler.DemoWeb` → **Start**
4. Click **OK**
5. Press **F5**


---

# API Examples

For current HTTP examples, see [docs/api-examples.md](docs/api-examples.md).

Use `POST /api/availability/compute` for the full availability surface, including `propertyFilterGroups`, `ancestorFilters`, slot chunking, and explanation output.
Use `GET /api/availability/slots` for the minimal query-string adapter over range, `resourceIds`, `orGroups`, and ancestor expansion.
Use `docs/management/webapi.md` for the management HTTP surface.
`/health` is the readiness endpoint, while `/api/demo/*` and `/api/diag/*` are ancillary endpoints rather than the main public API surface.

---

# Demo Application

HelixScheduler includes a DemoWeb application that showcases the engine in action.

The demo is intentionally:

- read-only
- free of scheduling logic in the frontend
- based exclusively on WebApi endpoints

This demonstrates that the engine is completely separated from the interface.

---

## Explorer

The Explorer page allows you to:

- navigate resources
- view hierarchical relations
- explore assigned properties
- understand the domain structure

It uses only catalog endpoints:

- `GET /api/catalog/resource-types`
- `GET /api/catalog/resources`
- `GET /api/catalog/properties`

![Explorer](assets/screenshots/explorer.jpeg)

---

## Availability Search

The Availability page allows you to:

- select a time range (UTC)
- set slot duration (`slotMinutes`)
- combine resources (AND / OR)
- apply grouped property filters, including descendant-aware matching (`includePropertyDescendants`)
- apply ancestor-aware constraints (`includeResourceAncestors`)
- include or exclude remainder slots

It calls:

`POST /api/availability/compute`

It shows:

- resulting slots
- deterministic behavior as parameters change
- the effect of capacity, unavailability, and busy slots

![Availability](assets/screenshots/availability.jpeg)

---

## Interaction Flow

UI → WebApi → Application → Core → normalized result → UI

The demo contains no computation logic.
All availability is produced by the engine.

---

# Project Status

The current version establishes a cleaner and more complete architectural baseline for HelixScheduler, including a canonical Core surface and a more coherent Application/Infrastructure boundary.

The model now includes:

- deterministic availability computation
- grouped AND / OR resource selection
- grouped property filters with OR-within-group and AND-between-group semantics
- ancestor-aware constraints
- recurring, single-date, range, monthly, and repeating rules
- capacity-aware busy handling
- explainability
- multi-tenant isolation

The project is designed to be integrated, extended, and maintained over time without coupling the scheduling engine to domain-specific application logic.

---

# Tests

```bash
 dotnet test
```

---

# License

Apache-2.0. See [LICENSE](LICENSE).




