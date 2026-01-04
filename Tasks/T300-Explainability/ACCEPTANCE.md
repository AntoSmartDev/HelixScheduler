# Acceptance Criteria — T300 Explainability (v1)

## Non regressione
- Con `explain=false`:
  - risposta identica a prima (schema e contenuto)
  - performance non peggiorata in modo evidente

## Funzionale (explain=true)
- `GET /api/availability/slots?...&explain=true` ritorna un oggetto:
  - `slots` (array)
  - `explanations` (array)
- Se `slots` è vuoto, `explanations` contiene almeno 1 entry coerente:
  - `NoPositiveRule` oppure `FullyBlockedByBusy` oppure `FullyBlockedByNegativeRule`
  - con `message` leggibile

## Validazione
- Se la richiesta è invalida (es. range > 31 giorni):
  - 400 come prima
  - (opzionale v1) una explanation `Validation` NON è richiesta (basta il 400 standard)

## Test/Verifica
- `dotnet build`
- `dotnet test` (se presenti test)
- test manuale:
  - caso con risultati (explain=true → slots presenti)
  - caso senza risultati (explain=true → explanations presenti)
