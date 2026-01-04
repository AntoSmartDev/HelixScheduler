# ADR-0004 — Explainability v1 per availability

## Status
Accepted

## Context
Serve supporto debug per capire perche' una query availability restituisce
risultato vuoto, senza modificare il core o degradare le performance.

## Decision
Introdurre un parametro `explain` sull'endpoint availability. Se true,
la WebApi restituisce un oggetto con `slots` e `explanations`. La logica
di explainability vive nell'application layer e usa solo dati gia'
caricati (rules/busy) con valutazioni best-effort.

## Consequences
- Pro: supporto debug rapido senza dipendenze nel core.
- Contro: reason conservativa, copertura limitata al caso empty (v1).
- Rischi: interpretazioni parziali quando busy e negative coesistono.

## Alternatives
- Calcolo spiegazioni nel core (scartato: violazione separazione).
- Explainability completa per ogni slot scartato (rimandata a v2).

## Validation
- `dotnet test`
- `GET /api/availability/slots?...&explain=true`
