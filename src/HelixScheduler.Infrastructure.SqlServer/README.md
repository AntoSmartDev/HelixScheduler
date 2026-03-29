# HelixScheduler.Infrastructure.SqlServer

`HelixScheduler.Infrastructure.SqlServer` provides the concrete SQL Server provider layer for the HelixScheduler infrastructure stack.

It contains the SQL Server-specific pieces that sit on top of `HelixScheduler.Infrastructure`, including:
- SQL Server provider registration
- SQL Server migrations and model snapshot
- SQL Server-specific migration bootstrap
- SQL Server-specific EF model customization

## What The Package Contains

The package contains the provider-specific runtime layer for SQL Server:
- `UseSqlServer(...)` wiring for `SchedulerDbContext`
- SQL Server migrations
- SQL Server migration bootstrap
- SQL Server-only EF optimizations that do not belong in the common substrate

## What The Package Does Not Contain

This package does not replace the common infrastructure substrate.

You still need:
- `HelixScheduler.Infrastructure` for the shared EF/persistence layer
- `HelixScheduler.Application` for the application-layer capabilities above the core

## Typical Usage

Use this package when you want to:
- run the HelixScheduler stack on SQL Server
- reuse the common infrastructure substrate with the current supported provider
- keep provider-specific concerns isolated from the common persistence project

## Install

```powershell
dotnet add package HelixScheduler.Infrastructure.SqlServer
```

This package is meant to be used together with `HelixScheduler.Infrastructure`.
