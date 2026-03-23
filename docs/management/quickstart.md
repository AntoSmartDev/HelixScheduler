# Management Layer Quickstart

## Goal
This quickstart shows the minimum logical journey for adopting the HelixScheduler management layer
starting from an empty tenant.

The goal is not to document every DTO or every endpoint. The goal is to provide the correct order
of operations.

## Start From Zero

### 1. Create Or Select A Tenant
Start by establishing the tenant boundary that will own the scheduler catalog.

Outcome:
- the administrative scope exists

### 2. Create Resource Types
Define the stable categories of schedulable resources.

Outcome:
- the catalog of resource types exists

### 3. Create Resources
Create the concrete schedulable resources and link each one to a resource type.

Outcome:
- the scheduler catalog has concrete resources

### 4. Build Hierarchy
Add parent-child relations between resources where structural hierarchy matters.

Outcome:
- the resource graph is usable and structurally valid

### 5. Define The Property Catalog
Create the property catalog and, when needed, the property tree.

Outcome:
- the structural vocabulary for filtering and configuration exists

### 6. Configure Type-Aware Property Compatibility
Define which property definitions are valid for each resource type.

Outcome:
- type-aware property compatibility is explicit

### 7. Assign Properties To Resources
Assign the catalog properties to the concrete resources.

Outcome:
- resources become structurally filterable and type-consistent

### 8. Define Rules
Configure the structural availability rules for the relevant resources.

Outcome:
- the scheduler model has structural availability

### 9. Register Busy Events
Register operational occupancy that subtracts availability from the structural baseline.

Outcome:
- the model reflects real operational load

### 10. Validate The Model
Run validation and inspect legacy diagnostics where needed.

Outcome:
- inconsistencies are visible before relying on compute

### 11. Use Read Snapshots For Troubleshooting
Use catalog and resource-configuration snapshots when you need to understand what was configured.

Outcome:
- the model is readable and diagnosable

### 12. Move To Compute
Only after the previous steps should the compute layer be used as the operational availability
surface.

Outcome:
- availability is computed on top of a governed and validated model

Typical next step:
- `POST /api/availability/compute`

## Minimal Mental Model
The management layer governs the model.
The read side explains the model.
The compute layer answers availability questions based on that model.

## HTTP Surface
When consumed through `HelixScheduler.WebApi`, the management commands live under:

- `/api/management/*`

Read snapshots and validation or legacy diagnostics also remain under the management HTTP surface,
but they should still be understood as read and troubleshooting capabilities rather than write
commands.

Tenant context is resolved through the WebApi tenancy middleware. When consuming the HTTP surface,
use `X-Helix-Tenant` or `X-Tenant` when you need a tenant other than `default`.

## Important Limits
This quickstart intentionally does not:
- document every endpoint in detail
- replace OpenAPI examples
- describe domain-specific workflows
- claim a separate public package surface that does not yet exist
