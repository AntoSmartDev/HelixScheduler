# Result - T415 Trim / Capacity discipline

## Stato
REMOVED

## Piano sintetico
- Task eliminato: le modifiche sono state annullate e si mantiene la baseline T410.

## Interventi eseguiti
- Ripristinato il codice al comportamento T410, rimuovendo il trim della capacity scratch.
- Rimossi gli artifacts di benchmark specifici T415.

## Impatto misurato
- N/A (task rimosso; confronto prestazioni effettuato su T410 restore).

## File toccati
- `HelixScheduler.Core/AvailabilityEngineV1.cs`
- `Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02-t415.csv`
- `Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02-t415.html`
- `Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02-t415.md`

## Impatto sul modello
- Nessuno: task rimosso, nessuna modifica al modello.

## Rischi / Note
- Nessuno.

## Come verificare
- Confrontare benchmark T410 vs T410-restore (artifacts in Tasks/T405-Benchmark-Report-Polish/artifacts).
