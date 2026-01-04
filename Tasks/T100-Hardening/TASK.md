# Task T100 — Hardening & Validation

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** in root come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md** per evitare duplicazioni o regressioni.

Documenti canonici di riferimento (come da AGENTS.md):
- Docs/context/PROJECT.md
- Docs/context/ARCHITECTURE.md
- Docs/context/DOMAIN_MODEL.md
- Docs/context/SCHEDULING_MODEL.md
- Docs/context/QUERY_MODEL.md
- Docs/context/PIPELINE.md
- Docs/context/PERFORMANCE.md
- Docs/context/SECURITY_MODEL.md
- Docs/context/GLOSSARY.md

## Obiettivo
Rafforzare la base del progetto HelixScheduler verificando:
- correttezza end-to-end (DB → Repo → Engine → API)
- robustezza su edge cases noti
- validazione input e non-regressioni
- chiarezza dei contratti pubblici

Il task **NON introduce nuove funzionalità**, ma stabilizza quanto già esistente.

## Scope
### IN
- Test di integrazione WebApi
- Validazioni input endpoint `/api/availability/slots`
- Edge cases del motore di scheduling
- Verifica separazione Core / Infrastructure / WebApi
- Pulizia minore se necessaria (senza refactor strutturali)

### OUT
- Nuove feature (capacity, OR semantics, explainability)
- Refactor architetturali
- Ottimizzazioni premature

## Deliverables
- Test di integrazione (o documentazione equivalente se non introdotti)
- Evidenza di validazione casi limite
- Eventuali fix minori documentati
- `RESULT.md` con sintesi chiara del lavoro svolto

## Vincoli
- Nessuna dipendenza EF o Web nel Core
- Nessuna logica di scheduling nei controller
- Tutto in UTC
- Ogni decisione strutturale → ADR

## Note
Questo task serve a “congelare” una **baseline stabile** prima di passare a:
- B) Samples.Medical
- C) Evoluzione funzionale
- D) Benchmark