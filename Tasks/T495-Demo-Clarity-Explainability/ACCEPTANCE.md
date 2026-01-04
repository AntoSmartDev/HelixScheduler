# Acceptance Criteria – T495

## Funzionali
- La demo rende chiaro:
  - come vengono calcolati gli slot
  - che uno slot può dipendere da più rules
  - che le rules contribuiscono agli slot
- Query Summary descrive l’intento della query in modo leggibile
- Explainability ON fornisce motivazioni comprensibili
- Properties raggruppate per ResourceType (non mischiate)
- Coerenza visiva Explorer/Search sul modello Type → Properties

## Architetturali
- Nessuna modifica a HelixScheduler.Core
- Nessuna modifica agli algoritmi
- UI-only / Demo-only
- Semantica invariata

## UX
- Un utente che apre la demo per la prima volta può:
  - capire il flusso di calcolo in < 1 minuto
  - distinguere Explorer e Search
  - intuire come usare HelixScheduler nel proprio progetto

## Qualità
- Codice UI leggibile e minimale
- Nessuna duplicazione concettuale
- Nessuna pezza o workaround

## Non Obiettivi
- Non introdurre assegnazione/ottimizzazione
- Non introdurre CQRS / mediator
- Non introdurre configurazioni avanzate
