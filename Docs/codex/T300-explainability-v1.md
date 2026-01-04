# T300 Explainability v1

## Sintesi
Introdotta explainability v1 su availability via parametro `explain`.
Quando attivo, la risposta include `slots` e `explanations` per i casi
con risultato vuoto. La logica resta nella WebApi, senza modifiche al core.

## Implementazione
- Nuovo DTO risposta: AvailabilityExplainResponse
- AvailabilityController gestisce explain=true/false
- AvailabilityApplicationService calcola una reason best-effort

## Test
- WebApi: explain=false invariato, explain=true con empty result
- `dotnet test`
