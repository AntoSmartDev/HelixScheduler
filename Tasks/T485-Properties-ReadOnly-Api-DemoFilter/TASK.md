# Task T485 â€” Properties read-only API + Demo filter (no Core impact)

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** come fonte di veritÃ  primaria.  
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
La DemoWeb visualizza risorse, availability, busy, rules e explainability (T480).
Manca perÃ² la possibilitÃ  di:
- vedere in modo chiaro le **properties** associate a ciascuna risorsa
- filtrare la demo per properties (read-only) in modo che si comprenda la flessibilitÃ  del motore

---

## Obiettivi
1) Esporre in **HelixScheduler.WebApi** endpoint read-only per:
   - elenco risorse e relative properties
   - (opzionale) catalogo properties e loro gerarchie (se presenti)
2) Integrare in **HelixScheduler.DemoWeb** una UI che:
   - mostri le properties **per risorsa** in modo visivamente chiaro
   - alla selezione di una risorsa, mostri sotto (o in pannello) le properties associate
3) Aggiungere un filtro demo read-only basato su properties, per guidare lâ€™utente
4) Nessun impatto sul Core: solo API/Application/Infrastructure/DemoWeb

---

## Vincoli (non negoziabili)
- Nessuna modifica a `HelixScheduler.Core`
- Nessuna modifica semantica dei risultati di availability (si usa solo filtering giÃ  supportato dal modello)
- Nessun CRUD
- Endpoint solo read-only
- UI read-only
- SemplicitÃ  > feature creep

---

## Scope tecnico

### A) WebApi â€” endpoint read-only properties
Aggiungere endpoint (nomi definitivi a discrezione purchÃ© coerenti e REST-ish):

1) `GET /demo/resources`
   - Ritorna elenco risorse con:
     - id, name, type (se presente), parentId (se presente)
     - properties associate (id, key/name, path/parent se utile)
   - Deve essere sufficiente per UI e filtro.

2) (Opzionale, solo se utile) `GET /demo/properties`
   - Ritorna catalogo properties disponibili (con gerarchia/subtree se esistente)
   - Utile se il filtro UI deve mostrare una select â€œper property treeâ€.

Nota: mantenere payload piccoli e DTO stabili (Application layer).

### B) Application layer
In `HelixScheduler.Application`:
- DTO:
  - `ResourceDto` + `ResourcePropertyDto`
  - `GetResourcesResponse` (+ eventuale `GetPropertiesResponse`)
- Servizio:
  - `IResourceCatalogService` / `ResourceCatalogService` (read-only)
- Porta verso Infrastructure:
  - `IResourceCatalogDataSource` (read-only)

### C) Infrastructure
In `HelixScheduler.Infrastructure`:
- Implementazione `IResourceCatalogDataSource` (EF read-only)
- Query efficienti (inclusioni controllate, niente N+1)
- Nessuna modifica alle migrazioni se non necessaria (si riusa schema esistente)

### D) DemoWeb UI
Aggiungere un pannello â€œPropertiesâ€ che sia chiaramente associato alla risorsa:

- Quando selezioni una risorsa (checkbox/list item):
  - sotto lâ€™item (collapsible) mostra le properties di quella risorsa
  - oppure mostra in un pannello laterale â€œProperties of <ResourceName>â€
- Rendere evidente che sono â€œproprietÃ  di quella risorsaâ€, non globali:
  - intestazione con nome risorsa
  - grouping per key/category se applicabile
  - tag/badge leggibili

### E) Demo filter (read-only)
Aggiungere un filtro properties in DemoWeb:
- modalitÃ  semplice consigliata:
  - una select â€œPropertyâ€ (dal catalogo o derivata dalle resources)
  - una select â€œValue/Nodeâ€ (se gerarchia)
  - toggle â€œInclude descendantsâ€ (riusa quello giÃ  presente)
- Il filtro deve influenzare:
  - la lista risorse (mostra solo quelle compatibili) e/o
  - la query availability inviata alla WebApi (preferred: filtra i candidates, poi compute)

Obiettivo: far percepire â€œproperty/subtree filteringâ€ in modo immediato.

---

## Fuori scope
- CRUD properties
- editing resources
- nuove semantiche di scheduling
- caching/pooling/ottimizzazioni

---

## Deliverables
- Endpoint read-only in WebApi per resources+properties (e catalogo se utile)
- Application service + DTO + porta data source
- Infrastructure data source EF read-only
- DemoWeb:
  - UI properties per risorsa (collapsible/panel)
  - filtro properties read-only
- RESULT.md aggiornato con:
  - endpoint aggiunti
  - UX screenshot/descrizione
  - conferma â€œno Core impactâ€
---

## Note aggiuntive (implementate)
- Health endpoint: `GET /health` con status/utc/version, usato da DemoWeb per wait-for-api.
- DemoWeb: status bar wait-for-api con retry/backoff, nascosta quando API ready, warning/error quando offline.
- WebApi homepage: solo link a OpenAPI JSON e Health.
- Rimosso `WeatherForecastController`.
- Normalizzazione property Key su reset (child.Key = parent.Key) + cleanup duplicati properties/links.
- DemoWeb: properties mostrate come "<ParentLabel>: <Label>" e banner "Properties of resource:".\r\n
