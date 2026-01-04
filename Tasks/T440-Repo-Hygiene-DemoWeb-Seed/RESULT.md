# Result - T440 Repo hygiene + DemoWeb + seed + Application layer

## Stato
DONE

## Piano sintetico
- Creato Application layer con servizi/porte (availability + demo) e DTO.
- Spostata logica dai controller a Application/Infrastructure; controller thin.
- Aggiunta DemoWeb read-only che chiama WebApi e seed dinamico.
- Completata repo hygiene (README, LICENSE, SECURITY, CONTRIBUTING, CHANGELOG, ROADMAP).

## Struttura finale (layering)
- Core: algoritmo e modello scheduling, UTC only.
- Application: use cases + DTO + interfaces (data source, demo seed/store, clock).
- Infrastructure: EF Core + repository + data source + seed.
- WebApi: thin controllers + DI + HTTP endpoints.
- DemoWeb: UI read-only che chiama WebApi.

## Modifiche principali
- Nuovo progetto `src/HelixScheduler.Application` con AvailabilityService, DemoReadService, DTO e porte.
- Infrastructure implementa `IAvailabilityDataSource` e `IDemoScenarioStore`, seed persistente con BaseDateUtc.
- WebApi: endpoint `POST /api/availability/compute`, `POST /demo/summary`, `POST /demo/reset` (dev only), GET slots legacy.
- DemoWeb: calendario settimanale + rules/busy + explain toggle.
- Provider DB: SQLite default, SQL Server opt-in via config.
  - Nota: SQLite disabilitato temporaneamente (vedi README).
- Repo hygiene: README, LICENSE, SECURITY, CONTRIBUTING, CHANGELOG, ROADMAP.

## Seed / Scenari
- BaseDateUtc persistita in DemoScenarioStates e riusata per run successive.
- Scenario 1: base AND (rules positive + busy su risorsa).
- Scenario 2: multi-risorsa non monolitico (busy su singola risorsa non blocca altre).
- Scenario 3: filtro property subtree (Specialization/RoomFeature con discendenti).
- Scenario 4: OR group + AND con altra risorsa.

## Quickstart
- `dotnet run --project HelixScheduler`
- `dotnet run --project HelixScheduler.DemoWeb`
- Demo: `https://localhost:7040`
- SQL Server: `HelixScheduler__DatabaseProvider=SqlServer` + `ConnectionStrings__SchedulerDb=...`

## File / Progetti toccati
- `src/HelixScheduler.Application/*`
- `HelixScheduler.Infrastructure/*` (DbContext, data source, seed, migrations)
- `HelixScheduler/*` (controllers, DI, appsettings)
- `HelixScheduler.DemoWeb/*`
- `Docs/context/ARCHITECTURE.md`
- `Docs/context/API_SURFACE.md`
- `Docs/context/PROJECT.md`
- `README.md`, `LICENSE`, `SECURITY.md`, `CONTRIBUTING.md`, `CHANGELOG.md`, `ROADMAP.md`
- `HelixScheduler.slnx`

## Impatto sul modello
- Nessun cambiamento alla semantica del Core.

## Note (Application layer)
- Candidati utili futuri (senza CQRS):
  - Input normalization/validation helpers (es. AvailabilityRequestValidator).
  - Mappers DTO -> Core (se si vuole disaccoppiare UtcSlot).
  - Read services per diagnostica/demo (resources, rules, busy).
  - Seed scenario catalogs (in-memory templates) per demo/test.

## Rischi / Note
- SQLite disabilitato temporaneamente per mismatch schema/locking in dev.
- Demo seed usa migrazioni automatiche in WebApi; in ambienti restrittivi serve permesso DB.
- DemoWeb dipende da `DemoWeb:ApiBaseUrl`; se la WebApi usa porte diverse serve override.

## Come verificare
- `dotnet build`
- `dotnet test`
## Aggiornamenti successivi
- CORS: abilitato accesso DemoWeb (https://localhost:7040) verso WebApi.
- DemoWeb: calendario separato per busy slots, label week spostata accanto al titolo, slot con risorse mostrate.
- Fix timezone: busy events serializzati in UTC (Z) + fallback client-side.
- SQL Server: fix DemoScenarioStore (no Id for identity).
- SQLite: supporto temporaneamente disabilitato per mismatch/locking in dev.\r\n

