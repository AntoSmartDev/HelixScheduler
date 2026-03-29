# HelixScheduler.Infrastructure

`HelixScheduler.Infrastructure` provides the shared EF Core persistence substrate for the HelixScheduler application stack.

It contains the common persistence-side implementations used by `HelixScheduler.Application`, including:
- `SchedulerDbContext`
- EF entities and configurations
- query services
- stores
- tenancy runtime support

## What The Package Contains

The package contains the common infrastructure substrate shared across provider integrations:
- `SchedulerDbContext`
- persistence entities and mappings
- query services for availability, catalog, and property schema reads
- management and tenant stores
- shared runtime persistence support

## What The Package Does Not Contain

This package does not define:
- SQL Server provider registration
- SQL Server migrations or model snapshot
- WebApi adapters
- demo seed, startup bootstrap, or diagnostics as the primary package value

SQL Server-specific concerns live in `HelixScheduler.Infrastructure.SqlServer`.

## Typical Usage

Use this package when you want to:
- reuse the common EF Core substrate behind the HelixScheduler application layer
- build on the shared persistence model before choosing a concrete provider package
- keep provider-specific concerns separate from the common infrastructure boundary

## Install

```powershell
dotnet add package HelixScheduler.Infrastructure
```

For the current SQL Server runtime integration, also install `HelixScheduler.Infrastructure.SqlServer`.
