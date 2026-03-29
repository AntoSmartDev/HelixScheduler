# HelixScheduler.Application

`HelixScheduler.Application` exposes the application-layer capabilities that sit directly above `HelixScheduler.Core`.

It provides orchestration and management-oriented services for:
- availability computation and request validation
- resource catalog and resource-type reads
- property schema resolution and validation
- scheduler model governance through management capabilities

## What The Package Contains

The package contains the application-layer capabilities that are meant to be reused in-process:
- availability orchestration
- resource catalog read services
- property schema services
- management capabilities for the scheduler model
- application contracts and abstractions required by those flows

## What The Package Does Not Contain

This package does not include:
- EF Core persistence implementations
- SQL Server provider registration or migrations
- HTTP or WebApi adapters
- demo, startup, or host-specific diagnostics concerns

Those concerns live in higher or adjacent layers of the HelixScheduler solution.

## Typical Usage

Use this package when you want to:
- compose HelixScheduler capabilities directly inside a .NET application
- consume management and read-side services above the canonical core
- keep application orchestration separate from persistence and HTTP hosting

## Install

```powershell
dotnet add package HelixScheduler.Application
```

For the persistence substrate used by the default stack, see `HelixScheduler.Infrastructure`.
