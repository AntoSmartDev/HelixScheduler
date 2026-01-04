# Acceptance Criteria — T440 Repo hygiene + DemoWeb + seed + Application layer

## Non regressione
- `dotnet build` verde
- `dotnet test` verde
- Core invariato in semantica (availability invariata)

---

## Architettura / Layering
- `HelixScheduler.Core` non referenzia EF Core o DB
- `HelixScheduler.Application` dipende solo da Core
- `HelixScheduler.Infrastructure` implementa interfacce definite in Application (dipende da Application + Core)
- `HelixScheduler.WebApi` usa controller thin e chiama servizi Application
- Nessun CQRS / Mediator / pipeline behaviors

---

## WebApi (integration sample)
- Endpoint canonical: `POST /api/availability/compute` con body JSON
- Endpoint reset demo: presente solo in Development
- Controller `AvailabilityController` privo di logica di dominio (solo binding + call service)

---

## DemoWeb (read-only)
- UI con calendario settimanale navigabile
- Visualizzazione regole e busy slots
- Toggle explain (se abilitato)
- DemoWeb chiama WebApi (non usa EF direttamente)

---

## Seed
- Seed “relative to install” (niente date fisse)
- BaseDateUtc persistita per stabilità
- Reset demo data disponibile solo in Development
- 4 scenari seed implementati

---

## DB provider
- SQLite quickstart funzionante (default)
- SQL Server opt-in funzionante tramite configurazione
- README con entrambe le procedure

---

## Repo hygiene
- README + LICENSE + SECURITY.md + CONTRIBUTING.md + CHANGELOG.md + ROADMAP.md presenti e coerenti
- Istruzioni quickstart riproducibili

---

## Output
- `RESULT.md` aggiornato con struttura, scelte, e note operative
- Entry aggiornata in `RESULTS.md`

