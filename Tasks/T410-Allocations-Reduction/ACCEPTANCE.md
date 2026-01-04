# Acceptance Criteria — T410 Allocations Reduction

## Non regressione
- `dotnet build` verde
- `dotnet test` verde
- output availability invariato (nessun cambiamento contratti)

## Misure (obbligatorie)
- Rieseguire benchmark e salvare report post-T410
- Per `D31_R2_OR2_BusyMed` ottenere almeno uno dei seguenti:
  - allocazioni ridotte >= 10%  **oppure**
  - tempo mean o p95 migliorato >= 10%
- Se non si ottiene nessun miglioramento, documentare perché e chiudere comunque.

## Complessità
- Nessuna nuova dipendenza heavy
- Nessun refactor esteso a più moduli non correlati
- Nessun nuovo livello architetturale

## Output
- `RESULT.md` aggiornato con numeri pre/post e link ai report
