# Acceptance Criteria — T310 OR semantics (v1)

## Non regressione
- Senza `orGroups`, l’API risponde esattamente come prima (schema e comportamento)
- I test esistenti restano verdi

## Funzionale (Core)
- Implementata semantica:
  Result = (AND required) ∩ (per-gruppo OR union)
- Test unitari:
  - caso base: 1 gruppo OR (A or B) produce union corretta
  - caso con required + OR group: intersezione corretta
  - caso con 2 gruppi OR: A ∩ O1 ∩ O2

## Funzionale (API)
- Parametro `orGroups` supportato con formato:
  `7,8|10,11,12`
- Validazioni:
  - gruppi > 5 → 400
  - risorse per gruppo > 10 → 400
  - totale risorse > 20 → 400
  - parsing invalido → 400
- Output:
  - slots corretti e ordinati

## Test/Verifica
- `dotnet build`
- `dotnet test`
- test manuale:
  - chiamata con orGroups valida
  - chiamata con orGroups invalida (400)
