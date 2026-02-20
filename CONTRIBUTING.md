# Contributing

Thanks for considering a contribution to HelixScheduler.

## Scope
We welcome bug fixes, performance improvements, and documentation updates. For new features or behavior changes, open an issue first to discuss scope and approach.

## Requirements
- .NET 10 SDK
- SQL Server (default provider)

## Build
```bash
 dotnet build
```

## Tests
```bash
 dotnet test
```

## Database & migrations
```bash
 dotnet ef database update \
   --project src/HelixScheduler.Infrastructure \
   --startup-project src/HelixScheduler.WebApi
```

## Code style
Keep changes consistent with existing style. Avoid large unrelated refactors in the same PR.

## Pull requests
- Keep changes focused and reasonably small.
- Include tests or update existing ones when behavior changes.
- Update documentation when public behavior changes.
- Ensure `dotnet test` passes locally.

By submitting a pull request, you agree that your contribution is licensed under Apache-2.0.
