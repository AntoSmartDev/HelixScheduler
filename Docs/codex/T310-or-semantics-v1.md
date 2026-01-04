# T310 OR semantics v1

## Sintesi
Supportati gruppi OR di risorse via parametro `orGroups` nell'API.
La query calcola union per gruppo e intersezione finale con required.

## Implementazione
- AvailabilityQuery include `resourceOrGroups` e `allResourceIds`
- AvailabilityEngine/V1 gestiscono OR (union per gruppo + intersection)
- WebApi parse/validazioni `orGroups`

## Test
- Core: 1 gruppo OR, required+OR, 2 gruppi OR
- WebApi: orGroups valido + validazioni
- `dotnet test`
