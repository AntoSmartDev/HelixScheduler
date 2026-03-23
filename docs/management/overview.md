# Management Layer Overview

## What It Is
HelixScheduler exposes two official product surfaces:

- compute layer
- management layer

The compute layer answers availability questions.
The management layer governs the scheduler model that the compute layer consumes.

Use the management layer to:
- create and maintain the scheduler catalog
- shape hierarchy and properties
- define structural rules
- register operational busy events
- validate configuration before relying on availability results

## What It Is Not
The management layer is not:
- a direct projection of database tables
- a replacement for the compute layer
- a generic read-only catalog

It is primarily a command and governance surface, with a small set of read and validation
capabilities that support onboarding and troubleshooting.

## Layer Distinction

### Management Layer
Purpose:
- govern the scheduler model
- execute administrative commands
- validate structural consistency

Examples:
- create tenant
- create resource type
- create resource
- assign properties
- create rule
- register busy event

### Read / Query Side
Purpose:
- expose snapshots
- support troubleshooting
- make configuration visible

Examples:
- scheduler catalog snapshot
- resource configuration snapshot
- validation and legacy consistency reports

The read side helps operators understand the model, but it does not replace management commands.

### Compute Layer
Purpose:
- calculate availability
- consume the canonical scheduling model

The compute layer comes after management and validation, not before.

## Supported Management Capabilities
Today the management layer includes:

- tenant management
- resource type management
- resource management
- hierarchy management
- property catalog management
- resource-property assignment management
- resource-type to property-definition management
- rule management
- busy event management
- management validation
- legacy consistency diagnostics and cleanup
- catalog snapshots for onboarding and troubleshooting

These capabilities are available as application services and are also exposed through the WebApi
adapter under `/api/management/*`.

## Consumption Modes
Current consumption modes are:

- embedded .NET consumption through the application layer
- HTTP consumption through `HelixScheduler.WebApi`

The current package layout remains the real solution layout:

- `HelixScheduler.Core`
- `HelixScheduler.Application`
- `HelixScheduler.Infrastructure`
- `HelixScheduler.WebApi`

This is the correct shape to document today. A separate public management package family is not yet
formalized.

## Operating Principle
The correct adoption principle is:

1. build the scheduler model through the management layer
2. validate the model
3. use the compute layer

In short:

`management first, compute after`
