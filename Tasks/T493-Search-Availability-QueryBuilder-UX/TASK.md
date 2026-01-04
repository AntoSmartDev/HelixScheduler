# Task T493 — Search Availability (type-driven query builder UI + candidates) + UX clarity

## Contesto globale (obbligatorio)
Seguire AGENTS.md come fonte di verità primaria.  
Seguire il protocollo operativo definito in TASKS.md.  
Leggere RESULTS.md.

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
Dopo T492 il dominio è corretto:
- ResourceType presente e obbligatorio
- property schema guidato dal tipo (data-driven)
- API catalogo sotto /api/catalog/*
- validate type-aware in Application

Ora possiamo costruire una pagina “Search Availability” che dimostri la flessibilità della query
senza workaround, e possiamo migliorare la comprensione della UI esistente con micro-interventi.

---

## Obiettivi
A) Nuova pagina `/search` (o tab “Search”) con:
- selezione type-driven delle risorse da intersecare (AND)
- filtri properties coerenti per tipo (schema)
- preview candidates per ciascun tipo (quanti e quali matchano)
- “Compute availability” che usa `POST /api/availability/compute`

B) Migliorare la pagina availability esistente per renderla più comprensibile:
- Query Summary box visibile
- micro-copy che distingue “calcolo” vs “visualizzazione”
- usare l’informazione di tipo per migliorare leggibilità (tag/gruppi)

---

## Vincoli (non negoziabili)
- Nessuna modifica a HelixScheduler.Core
- Nessuna modifica semantica del calcolo availability
- No CQRS/mediator/pattern pesanti
- UI read-only
- Semplicità > feature creep
- Reuse degli endpoint /api/catalog/* e /api/availability/compute

---

## Scope A — Search Availability (/search)

### A1) UI: Query Builder (type-driven)
Creare pagina `Search Availability` con layout semplice:

1) Selezione “Query Resources (AND)”
   - pulsante “+ Add resource type”
   - per ogni blocco aggiunto:
     - select ResourceType (Doctor, Room, Nurse…)
     - (opzionale) select “Specific resource” oppure “Any resource of this type”
       - default: Any (mostra potenza)

2) Filtri properties per blocco (solo quelle valide per quel tipo)
   - list/selector delle PropertyDefinitions roots consentite per quel tipo (da schema)
   - selezione nodi/valori (PropertyNode) con supporto “include descendants”
   - chips dei filtri selezionati per quel blocco

3) Candidates preview
   - per ciascun blocco:
     - “Matching resources: N”
     - elenco nomi (limitato, es. max 10 con “+N more”)
   - questo preview deve aggiornarsi quando cambiano i filtri

4) Compute Availability
   - selezione range (week navigation come pagina attuale, o reuse componente)
   - toggle Explain
   - bottone “Compute”
   - risultato: mostra calendario settimanale availability + busy come pagina principale
     (può riusare componenti esistenti)
   - mostra chiaramente quali “query resources” sono stati richiesti

### A2) API usage (no new endpoints required if possibile)
Usare:
- `GET /api/catalog/resource-types`
- `GET /api/catalog/resources`
- `GET /api/catalog/properties`
- `GET /api/catalog/property-schema` (o equivalente da T492)
- `POST /api/availability/compute`

Se manca un endpoint necessario (es. schema per tipo), aggiungerlo in WebApi/Application
ma SOLO read-only e coerente con /api/catalog/*.

### A3) Payload della compute
Generare una request coerente con l’attuale DTO di compute.
Il builder deve produrre:
- resource requirements (AND)
- per ciascun requirement: TypeId e (opzionale) specific ResourceId
- property filters per requirement, type-aware

Nota: se l’attuale DTO non supporta “Any-of-type”, introdurre un’estensione in Application/WebApi
ma senza cambiare l’engine: deve solo espandere a candidates prima della compute.

---

## Scope B — UX clarity sulla pagina Availability esistente

### B1) Query Summary box (PRIORITÀ ALTA)
Sotto “Availability (UTC)” aggiungere un box compatto:

Current availability query
- Resources: Doctor 7, Room 1 (mostrando anche il type)
- Properties: Specialization = Ophthalmology (e include descendants se attivo)
- Range: Week YYYY-MM-DD ? YYYY-MM-DD
- Explain: On/Off

Se la query deriva da `/search`, deve mostrare anche:
- “Requirements: [Doctor: Any] + [Room: Room 1] …”

### B2) Micro-copy “calcolo vs visualizzazione”
Aggiungere testi minimali:

Sotto Availability:
- “Computed from rules, after subtracting busy intervals.”

Sotto Busy Calendar:
- “Intervals that remove or split availability.”

### B3) Derived-from / Rule hint (se già disponibile)
Se la UI già mostra rule id nel tooltip, mantenerlo.
Se non esiste una info diretta, non inventare: restare best-effort.

### B4) Tipi risorsa in UI
Usare TypeLabel/Key per:
- raggruppare la lista risorse per tipo
- mostrare tag tipo accanto al nome

---

## Fuori scope
- CRUD
- drag/drop
- editing rules
- planner/optimizer
- caching

---

## Deliverables
- Pagina `/search` con query builder type-driven + candidates preview + compute
- Miglioramenti UI su pagina availability (Query Summary + micro-copy + type labeling)
- RESULT.md aggiornato con:
  - screenshot/descrizione UX
  - endpoint usati/aggiunti
  - note su compatibilità


