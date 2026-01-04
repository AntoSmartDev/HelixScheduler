# Acceptance Criteria — T493 Search Availability + UX clarity

## Architettura
- Nessuna modifica a HelixScheduler.Core
- Nessun CQRS/mediator/pattern pesanti
- Reuse /api/catalog/* e /api/availability/compute (aggiungere solo read-only se indispensabile)

## Search Availability (/search)
- Pagina disponibile e navigabile
- È possibile:
  - aggiungere 1..N blocchi “resource requirement”
  - selezionare un ResourceType per blocco
  - applicare filtri property coerenti col tipo (schema)
  - vedere preview candidates (N + lista limitata)
  - lanciare compute e vedere calendario risultati

## Type-aware properties
- I filtri mostrati per un tipo sono solo quelli consentiti dal mapping tipo→property definition
- include descendants funziona (se il property tree lo supporta)

## UI comprehension (pagina availability)
- Presente Query Summary box sotto “Availability (UTC)”
- Presente micro-copy che distingue availability vs busy
- UI mostra chiaramente il tipo delle risorse (tag o grouping)

## Qualità
- Build verde
- Test verdi
- Nessuna regressione UX nelle pagine esistenti
- RESULT.md aggiornato
- Entry in RESULTS.md aggiornata


