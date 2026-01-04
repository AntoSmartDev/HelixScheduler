# Task T320 — Capacity (v1)

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** in root come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md**.

Documenti canonici di riferimento:
- Docs/context/DOMAIN_MODEL.md
- Docs/context/SCHEDULING_MODEL.md
- Docs/context/QUERY_MODEL.md
- Docs/context/PIPELINE.md
- Docs/context/PERFORMANCE.md
- Docs/context/SECURITY_MODEL.md
- Docs/context/ARCHITECTURE.md

## Decisioni v1 (vincolanti)
- Capacity è **per-resource** (non pool).
- Capacity è definita in **DB/metadata della resource** (non passata dal client).
- Occupancy che consuma capacity = **Busy events** (non regole negative).
- Availability calcola “esistenza di copertura con capacity residua”, **non** assegna risorsa specifica per slot.
- Fast path: se tutte le risorse coinvolte hanno capacity=1 (default), comportamento invariato.

## Obiettivo
Estendere il calcolo availability per considerare la capienza delle risorse:
uno slot è disponibile se, per ogni risorsa richiesta (AND) e per le risorse alternative nei gruppi OR,
l’occupancy dovuta ai busy events nel segmento è < capacity della risorsa.

## Scope
### IN
- Aggiunta campo capacity alla resource (DB + EF + model)
- Adeguamento repository per leggere capacity con le risorse
- Estensione engine/pipeline per applicare capacity su busy events
- Test unitari engine + almeno un test integrazione API
- Aggiornamento benchmark (se necessario) per misurare impatto (solo se minimale)

### OUT
- Allocazione/booking (chi ha preso lo slot)
- Pool capacity / global capacity
- Explainability v2 per capacity (si farà dopo se serve)

## Modello dati (v1)
Aggiungere a Resource una proprietà:
- `Capacity` (int, default 1, min 1, max ragionevole es. 100)

Busy events consumano 1 unità ciascuno (v1):
- più busy sovrapposti → occupancy cresce

## Semantica di calcolo (v1)
Per una resource r:
- availability window data da regole positive/negative come oggi
- busy events definiscono occupancy(t)
- un intervallo è disponibile se:
  - è dentro le finestre positive e non escluso da negative
  - e per ogni segmento S: occupancy(S) < capacity(r)

Per OR groups:
- per ogni gruppo si calcola la union delle availability delle risorse del gruppo,
  ma ogni resource nel gruppo deve rispettare la propria capacity quando calcola la sua availability.

## Validazioni e limiti
- Capacity in DB deve essere >= 1
- Se capacity non trovata -> default 1 (se compatibile con modello)
- Mantenere i limiti già esistenti su range e numero risorse

## Deliverables
- Migrazione SQL/EF: capacity su Resource
- Aggiornamento entity + DbContext + repository
- Engine: applicazione capacity su busy overlap
- Test: casi con capacity=1 invariato e capacity>1 con busy sovrapposti
- `RESULT.md` completo

## Note
Questa v1 deve restare semplice:
- busy event = 1 unità
- niente “peso” variabile
- niente assignment
