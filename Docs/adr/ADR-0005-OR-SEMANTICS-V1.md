# ADR-0005 — OR semantics v1 per availability

## Status
Accepted

## Context
Serve supportare alternative di risorse (OR) mantenendo la semantica
AND esistente e senza dipendenze nel core.

## Decision
Estendere AvailabilityQuery con `resourceOrGroups` e calcolare:
Result = (AND required) ∩ (per-gruppo OR union).
Parsing e validazioni di `orGroups` avvengono in WebApi.

## Consequences
- Pro: semantica chiara, compatibile con behavior precedente.
- Contro: OR solo a livello di gruppi (no nesting).
- Rischi: output `resourceIds` rappresenta l'insieme di risorse coinvolte,
  non l'assegnazione effettiva (v1).

## Alternatives
- OR nidificato arbitrario (rimandato a v2).
- Gestione OR solo in WebApi (scartato: duplicazione logica).

## Validation
- `dotnet test`
- `GET /api/availability/slots?...&orGroups=7,8|10,11`
