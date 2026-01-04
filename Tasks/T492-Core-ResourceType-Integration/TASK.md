
# Task T492 — Native ResourceType integration (domain-correct, no hacks)

## Contesto globale (obbligatorio)
Seguire AGENTS.md come fonte di verità primaria.  
Seguire il protocollo operativo definito in TASKS.md.  
Leggere RESULTS.md prima di iniziare.

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

DB reference:
- SQL/script.sql (schema DB corrente)

---

## Contesto e problema
Il modello attuale gestisce:
- Resources
- Properties
- Availability rules
- Busy slots

Ma **manca un concetto fondamentale di dominio**:
> ogni Resource deve appartenere ad un ResourceType,  
> e le Properties devono essere definite *per tipo*, non globalmente.

Questa mancanza:
- rende ambigua la modellazione
- rende impossibile costruire un vero query builder guidato
- costringe la UI a workaround (property globali + filtri euristici)
- non rispecchia l’idea originaria del dominio

---

## Obiettivo
Integrare **ResourceType** in modo **nativo, definitivo e corretto**, come concetto di dominio di primo livello.

Il risultato deve essere:
- semanticamente corretto
- coerente con Clean Architecture
- stabile nel tempo
- pronto per Search Availability e NuGet packaging

---

## Decisioni architetturali (vincolanti)

### 1) ResourceType è **nel Core**
- Non è un concetto UI
- Non è un dettaglio DB
- È un concetto di dominio

Nel Core:
- esiste `ResourceTypeId`
- ogni `Resource` ha un `TypeId` **obbligatorio**
- il Core NON conosce stringhe tipo "Doctor", "Room"
- il Core NON contiene cataloghi, label o configurazioni

### 2) Property schema è **guidato dal tipo**
Concettualmente:
- `PropertyDefinition` (famiglia: Specialization, Capability, Feature)
- `PropertyNode` (valori/gerarchia: Ophthalmology, OCT, etc.)
- `ResourceTypeProperty` definisce **quali PropertyDefinition sono valide per un tipo**
- `ResourcePropertyAssignment` assegna valori alle singole risorse

Il Core **non** gestisce cataloghi o definizioni,
ma deve poter lavorare con:
- `PropertyDefinitionId`
- `PropertyNodeId`
in modo type-aware.

### 3) Nessuna pezza retro-compatibile
- Se il modello va corretto, va corretto
- Endpoint/DTO possono cambiare **se necessario**
- Ogni breaking change va documentato chiaramente

---

## Scope tecnico

### A) HelixScheduler.Core
Modifiche consentite e richieste:

- Introdurre `ResourceTypeId` (value object o strong id)
- Aggiornare `Resource`:
  - aggiungere `TypeId` (non nullable)
- Aggiornare eventuali modelli/query che manipolano risorse
  per propagare TypeId dove semanticamente corretto
- **NON** modificare l’algoritmo di availability
- **NON** introdurre cataloghi o stringhe nel Core

---

### B) Infrastructure (DB + EF)
Partendo dallo schema corrente:

- Introdurre tabella `ResourceTypes`
  - Id
  - Key (Doctor, Room, Nurse…)
  - Label
  - SortOrder
- Aggiornare `Resources`:
  - aggiungere `TypeId` (FK NOT NULL)
- Introdurre tabella `ResourceTypeProperties`
  - mapping Type ? PropertyDefinition (root)
- Aggiornare seed:
  - ogni Resource ha un TypeId
  - ogni Type ha property definitions coerenti
- Migrazioni pulite, senza colonne legacy inutilizzate

---

### C) HelixScheduler.Application
- Esporre DTO corretti e type-aware:
  - ResourceDto include `typeId`, `typeKey`, `typeLabel`
- Introdurre servizi:
  - `IResourceTypeCatalogService`
  - `IPropertySchemaService` (read-only)
- Validazioni:
  - una query non può associare proprietà non valide per il tipo selezionato
- Application dipende solo da Core

---

### D) HelixScheduler.WebApi
- Availability API restano **API di servizio**, non “demo”
- Endpoint catalogo (naming neutro):
  - `/api/catalog/resource-types`
  - `/api/catalog/resources`
  - `/api/catalog/properties`
- Endpoint demo-only (seed/reset) restano sotto `/demo/*`
- Controller possono essere modificati se serve coerenza semantica
  (non introdurre CQRS)

---

### E) DemoWeb
- UI aggiornata per riflettere il tipo risorsa:
  - etichette/gruppi Doctor / Room / Nurse
- Properties mostrate come:
  - "Properties of <Resource> (Type: Doctor)"
- Nessuna Search Availability in questo task

---

## Fuori scope esplicito
- Search Availability page
- Query builder UI
- CRUD
- Ottimizzazioni / planner / assignment
- Caching

---

## Deliverables
- Core aggiornato con ResourceTypeId e TypeId obbligatorio
- Schema DB corretto e coerente
- Seed aggiornato senza workaround
- DTO/API/UI allineati al nuovo modello
- RESULT.md aggiornato con:
  - modifiche dominio
  - note su breaking changes
  - come verificare

