# Task T440 — Repo hygiene + DemoWeb read-only + seed (WebApi) + Application layer

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** in root come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md**.

Riferimenti canonici:
- Docs/context/PROJECT.md
- Docs/context/ARCHITECTURE.md
- Docs/context/DOMAIN_MODEL.md
- Docs/context/SCHEDULING_MODEL.md
- Docs/context/QUERY_MODEL.md
- Docs/context/PIPELINE.md
- Docs/context/PERFORMANCE.md
- Docs/context/SECURITY_MODEL.md
- Docs/context/GLOSSARY.md

---

## Contesto
Il Core è stabile e misurato (T400/T405/T410) e l’API surface è stata ripulita (T420).
Ora vogliamo rendere il repository "adottabile" e dimostrabile rapidamente con una demo visiva,
senza introdurre nuove semantiche o complessità (no CQRS, no mediator).

---

## Obiettivi
1) **Repo hygiene** per adozione (OSS-ready)
2) **Demo Web read-only** con UI visiva (calendario settimanale + pannello regole/busy + explain toggle)
3) **HelixScheduler.WebApi** come integration sample che:
   - espone endpoint canonical `POST /api/availability/compute`
   - gestisce seed read-only lato server
4) **Doppio DB provider** per WebApi:
   - SQLite (default, quickstart)
   - SQL Server (opt-in, enterprise)
5) Creare il nuovo progetto **`src/HelixScheduler.Application`** per Clean Architecture:
   - senza CQRS / mediator
   - per spostare logica dai controller (thin controllers)
   - Application dipende solo da Core
   - Infrastructure implementa porte definite in Application

---

## Vincoli (non negoziabili)
- Nessuna modifica semantica del Core (availability invariata)
- Nessun pattern pesante (CQRS, Mediator, pipeline behaviors)
- Nessuna dipendenza EF/DB nel Core
- Controller WebApi devono essere **thin**
- Seed deve essere:
  - dinamico "relative to install"
  - stabile nel tempo (baseDate persistita)
  - resettabile solo in Development

---

## Scope tecnico

### A) Nuovo progetto: `HelixScheduler.Application`
Creare `src/HelixScheduler.Application` e definire:
- Use cases / servizi applicativi (es. Availability)
- DTO request/response per WebApi (non esporre direttamente tipi Core se non necessario)
- Interfacce (porte) per accesso dati implementate da Infrastructure

Minimo richiesto:
- `IAvailabilityService` + `AvailabilityService`
- `AvailabilityComputeRequest` / `AvailabilityComputeResponse` (DTO)
- `IAvailabilityDataSource` (porta verso Infrastructure)
- `IDemoSeedService` (orchestrazione) + `IDemoScenarioStore` (persistenza stato seed)
- eventuale `IClock` (per testabilità) se utile

Richiesta esplicita:
- Codex deve proporre (in RESULT.md) altre classi/concetti che hanno senso in Application
  nel contesto attuale del repo, mantenendo semplicità (no CQRS).

### B) Infrastructure (EF Core) + provider DB
In `HelixScheduler.Infrastructure`:
- EF Core DbContext e entity per demo (read-only scenario)
- supporto provider SQLite e SQL Server (stesso modello)
- implementazioni delle interfacce Application:
  - `IAvailabilityDataSource`
  - `IDemoScenarioStore`

### C) WebApi
In `HelixScheduler.WebApi`:
- endpoint canonical:
  - `POST /api/availability/compute` (body JSON)
- endpoint dev-only:
  - `POST /demo/reset` (solo Development) per rigenerare seed
- DI wiring Application + Infrastructure
- Controllers sottili: solo input/output + chiamata service

### D) Demo Web UI
Creare nuovo progetto `HelixScheduler.DemoWeb` (read-only) che:
- chiama `HelixScheduler.WebApi`
- visualizza:
  - calendario settimanale navigabile (prev/next week)
  - lista regole e busy slots attive per scenario
  - toggle explain (usa explainability v1 se disponibile via API)

### E) Repo hygiene
Aggiungere o completare file minimi:
- README.md (posizionamento + quickstart SQLite + quickstart SQL Server + come avviare demo)
- LICENSE (da scegliere in root)
- SECURITY.md (minimo)
- CONTRIBUTING.md (minimo)
- CHANGELOG.md (minimo) + note “preview”
- ROADMAP.md (minimo)

---

## Seed “relative to install” (obbligatorio)
- Alla prima inizializzazione, calcolare `BaseDateUtc` e persistirla (scenario).
- Generare regole/busy usando offset rispetto a `BaseDateUtc`.
- Non usare date fisse.
- A reset (dev-only), rigenerare scenario e aggiornare BaseDateUtc/SeedVersion.

Scenari seed richiesti (minimo 4):
1) Base AND: risorsa + busy slot
2) Multi-risorsa non monolitico (busy su una risorsa non blocca altre non coinvolte)
3) Property filter subtree (es. specializzazione/attrezzatura)
4) OR group (una qualunque risorsa tra un gruppo) + AND con altre risorse

---

## Deliverables
- Nuovo progetto `HelixScheduler.Application`
- Nuovo progetto `HelixScheduler.DemoWeb`
- WebApi con endpoint POST canonical + seed + reset dev-only
- Doppio provider DB (SQLite default + SQL Server opt-in)
- Repo hygiene completata
- RESULT.md aggiornato con:
  - struttura finale (layering)
  - elenco file/progetti toccati
  - note su seed e quickstart
  - proposta Codex: cosa altro mettere in Application (se opportuno)

