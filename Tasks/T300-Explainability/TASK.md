# Task T300 — Explainability (v1)

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** in root come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md** per evitare duplicazioni o regressioni.

Documenti canonici di riferimento:
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
Introdurre una **Explainability v1** per l’endpoint availability, in modo da
ottenere motivazioni utili in caso di:
- risultato vuoto
- (opzionale v1) slot parzialmente ridotti o “buchi” derivanti da busy/negative

L’obiettivo principale è migliorare **debug e supporto**, senza cambiare il comportamento
standard quando `explain=false`.

## Scope
### IN
- Nuovo parametro query: `explain=true|false` (default false)
- Nuovo output opzionale di spiegazione
- Logica di spiegazione concentrata nell’application layer (WebApi), senza contaminare il Core
- Test minimi per garantire non-regressione

### OUT
- Explainability completa per ogni candidate slot scartato (v2)
- UI avanzata o dashboard
- Refactor del modello regole

## Specifica funzionale (v1)
### API
Endpoint esistente:
`GET /api/availability/slots`

Nuovi parametri:
- `explain` (bool, default false)

Comportamento:
- `explain=false`: risposta invariata (array di slot)
- `explain=true`: risposta estesa con:
  - `slots`: come oggi
  - `explanations`: lista di motivazioni (può essere vuota se non applicabile)

### Modello spiegazione (v1)
Una explanation deve includere:
- `reason` (enum/string): `NoPositiveRule | FullyBlockedByBusy | FullyBlockedByNegativeRule | PartiallyBlocked | Validation`
- `resourceId` (int? se applicabile)
- `fromUtc`, `toUtc` (DateTime? se applicabile)
- `ruleId` (nullable) oppure `busyEventId` (nullable) se disponibile
- `message` (string breve e stabile)

### Logica v1 (minimo utile)
Quando `explain=true`:
1) Se `slots` è vuoto:
   - se non esistono regole positive applicabili → `NoPositiveRule`
   - altrimenti se busy copre tutto → `FullyBlockedByBusy`
   - altrimenti se negative coprono tutto → `FullyBlockedByNegativeRule`
   - altrimenti → `PartiallyBlocked` (fallback conservativo)
2) Se `slots` non è vuoto:
   - v1 può non produrre explanations oppure produrre un summary `PartiallyBlocked` (facoltativo)

## Vincoli
- Nessuna dipendenza EF/Web nel Core
- L’engine resta invariato o cambia solo per esporre minime info necessarie in modo neutro (preferibile: zero cambi)
- Performance: explainability deve essere “best-effort” e non trasformare l’endpoint in una query esplosiva

## Deliverables
- Output API esteso quando `explain=true`
- DTO di risposta versionato/stabile
- Test (unit o integration) per `explain=false` invariato e caso `explain=true` con risultato vuoto
- `RESULT.md` aggiornato

## Note
Se per implementare una reason attendibile serve distinguere busy vs negative vs no-positive,
usare i dati già caricati (rules + busy) nell’application service:
- conteggi e coverage rispetto al window richiesto
- nessun calcolo “candidate exhaustive”
