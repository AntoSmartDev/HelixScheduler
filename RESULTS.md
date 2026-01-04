# RESULTS.md - Indice risultati task

## T460 - WebApi Thin Controllers (Availability)
- Path: Tasks/T460-WebApi-Thin-Controllers/RESULT.md
- Stato: DONE
- Output: controller availability thin, input model + parser/validator separati, route coerenti `/api/availability/*` senza alias
- Note: refactor architetturale WebApi-only, nessun impatto su Core o semantica; DemoWeb allineato; DiagController su IDiagnosticsService; bootstrap seed/migrate via IStartupInitializer; catalog/demo sotto `/api/`


## T495 - Demo Clarity & Explainability
- Path: Tasks/T495-Demo-Clarity-Explainability/RESULT.md
- Stato: DONE
- Output: mental model sotto Availability, Query Summary narrativo, slot↔rules multi-rule aware, explainability più leggibile, distinzione Explorer vs Search esplicita, Properties grouped by ResourceType (Option A), nav/footer e hardening rendering demo
- Note: UI-only; nessun impatto su HelixScheduler.Core o semantica availability; wording “contributing rules” (slot può derivare da più rules); demo safe rendering (no innerHTML)


## T493 - Search Availability + UX clarity
- Path: Tasks/T493-Search-Availability-QueryBuilder-UX/RESULT.md
- Stato: DONE
- Output: pagina `/search.html` con query builder type-driven + preview candidates + compute; availability page con Query Summary e raggruppamento per tipo
- Note: reuse `/api/catalog/*` + `/api/availability/compute`; nessuna modifica al Core; explain default on, tooltip info include descendants, OR-only support

## T492 - Core ResourceType integration
- Path: Tasks/T492-Core-ResourceType-Integration/RESULT.md
- Stato: DONE
- Output: ResourceTypeId nel Core + TypeId obbligatorio; schema DB con ResourceTypes/ResourceTypeProperties; catalog endpoints `/api/catalog/*`; DemoWeb type-aware
- Note: nessuna stringa type nel Core; semantica availability invariata; niente CQRS/mediator; breaking change: /demo/resources e /demo/properties rimossi; migrations ripulite e baseline unica (script.sql aggiornato)

## T485 - Properties read-only API + Demo filter
- Path: Tasks/T485-Properties-ReadOnly-Api-DemoFilter/RESULT.md
- Stato: DONE
- Output: endpoints /demo/resources + /demo/properties, demo filter properties con includeDescendants, properties visibili per risorsa
- Note: no Core impact; cleanup properties/links + normalizzazione key su reset; wait-for-api in DemoWeb + /health; Domain=Core esplicitato in docs


## T480 - Demo UX Explainability
- Path: Tasks/T480-Demo-UX-Explainability/RESULT.md
- Stato: DONE
- Output: explainability visiva rule → slot → busy (tooltip, highlight interattivo, tag risorse, micro-copy)
- Note: nessun impatto su Core, Application, Infrastructure o WebApi; UX read-only, property select UI requires a read-only properties endpoint (out of scope for T480).

## T440 - Repo hygiene + DemoWeb read-only + seed (WebApi) + Application layer
- Path: Tasks/T440-Repo-Hygiene-DemoWeb-Seed/RESULT.md
- Stato: DONE
- Output: demo visiva (DemoWeb->WebApi->Core), seed dinamico stabile, provider DB SQL Server, struttura Clean Architecture (Application layer)
- Note: nessuna modifica semantica del Core; niente CQRS/mediator; controller WebApi thin - T440 update: CORS DemoWeb, busy UTC fix, DemoWeb calendar improvements, SQL Server identity fix, SQLite disabled.


## T420 - API Surface Review
- Path: Tasks/T420-Api-Surface-Review/RESULT.md
- Stato: DONE
- Output: XML docs mirati + guardrail null entries in AvailabilityQuery
- Note: nessun breaking change su input validi; benchmark singolo eseguito

## T410 - Allocations Reduction
- Path: Tasks/T410-Allocations-Reduction/RESULT.md
- Stato: DONE
- Output: riduzione overhead (D31_R2_OR2_BusyMed) con benchmark pre/post
- Note: time migliorato >10%, allocazioni +3.19% (vedi Tasks/T410-Allocations-Reduction/RESULT.md)

## T415 - Trim Capacity Discipline
- Path: Tasks/T415-Trim-Capacity-Discipline/RESULT.md
- Stato: REMOVED
- Output: task eliminato, ripristino a baseline T410
- Note: modifiche T415 annullate e artifacts T415 rimossi

## T320 - Capacity (v1)
- Path: Tasks/T320-Capacity/RESULT.md
- Stato: DONE
- Output: capacity per-resource (DB), occupancy da busy events, compatibile con OR groups
- Note: v1 senza assignment; fast-path capacity=1 (docs/codex/T320-capacity-v1.md)

## T405 - Benchmark Report Polish
- Path: Tasks/T405-Benchmark-Report-Polish/RESULT.md
- Stato: DONE
- Output: report benchmark con baseline + p50/p95 + guida confronto run
- Note: nessuna ottimizzazione, solo miglioramento misurazioni

## T400 - Benchmark & Profiling
- Path: Tasks/T400-Benchmark-Profiling/RESULT.md
- Stato: DONE
- Output: progetto BenchmarkDotNet + report numeri/allocazioni
- Report: Tasks/T400-Benchmark-Profiling/REPORT.md
- Note: baseline prima di ottimizzazioni/capacity (Tasks/T400-Benchmark-Profiling/REPORT.md); artifacts run: Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.csv, Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.html, Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.md

## T310 - OR semantics (v1)
- Path: Tasks/T310-OR-Semantics/RESULT.md
- Stato: DONE
- Output: supporto orGroups in availability + test Core/WebApi
- Note: v1 url-safe 7,8|10,11,12 (ADR-0005, docs/codex/T310-or-semantics-v1.md, docs/context/QUERY_MODEL.md)

## T300 - Explainability (v1)
- Path: Tasks/T300-Explainability/RESULT.md
- Stato: DONE
- Output: API availability con explain=true, explanations v1 e test dedicati
- Note: Feature di supporto/debug, nessuna regressione su explain=false (ADR-0004, docs/codex/T300-explainability-v1.md)

## T200 - Samples.Medical (UI di verifica)
- Path: Tasks/T200-Samples-Medical/RESULT.md
- Stato: DONE
- Output: UI Razor con range, link API, istruzioni, demo button e risorse via API
- Note: Task di validazione manuale del motore

## T100 - Hardening & Validation
- Path: Tasks/T100-Hardening/RESULT.md
- Stato: DONE
- Output: Hardening input validation e determinismo availability + test WebApi mirati
- Note: Task baseline completato prima di Samples.Medical










