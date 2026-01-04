# Acceptance Criteria — T492 Native ResourceType integration

## Dominio (Core)
- `HelixScheduler.Core` contiene `ResourceTypeId`
- Ogni `Resource` ha `TypeId` non nullo
- Il Core NON contiene:
  - stringhe tipo
  - cataloghi
  - logica DB/UI
- Algoritmo di availability invariato (risultati identici)

---

## Schema dati
- Tabella `ResourceTypes` presente e popolata
- FK `Resources.TypeId` NOT NULL
- Mapping Type → PropertyDefinition esplicito
- Nessuna colonna legacy inutilizzata

---

## Application layer
- DTO espongono typeId + typeKey/typeLabel
- Validazione type-aware delle properties
- Nessuna dipendenza EF

---

## API
- Availability API restano API di servizio
- Catalog endpoints usano naming neutro (`/api/catalog/*`)
- Endpoint demo limitati a seed/reset

---

## DemoWeb
- Tipo risorsa chiaramente visibile
- Properties coerenti con il tipo
- Nessuna regressione UX

---

## Qualità
- Build verde
- Test verdi
- RESULT.md aggiornato
- Entry in RESULTS.md aggiornata

