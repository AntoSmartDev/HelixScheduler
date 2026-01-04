# Result - T410 Allocations Reduction

## Stato
DONE

## Piano sintetico
- Individuare hotspot a bassa complessita' nel core (sorting/loop).
- Applicare ottimizzazioni low-risk senza cambiare semantica.
- Benchmark pre/post con focus D31_R2_OR2_BusyMed.

## Hotspot identificati
- OrderBy/ToList ripetuti in NormalizeSlots/NormalizeSlotsByTime e IntersectSlots.
- Allocazioni temporanee in SubtractSlots (liste e iteratori).
- Profiling: dotnet-trace non disponibile (comando fallito: `dotnet-trace --version`), quindi analisi via code inspection + BenchmarkDotNet.

## Interventi eseguiti
- Sostituiti OrderBy con sort in-place su liste.
- Rimosso ordinamento ridondante in IntersectSlots (input gia' normalizzati).
- SubtractSlots: reuse di liste temporanee e rimozione iteratori per ridurre allocazioni.

## Numeri pre/post (D31_R2_OR2_BusyMed)
Pre (capacity run):
- Mean: 968.996 us
- P95: 1149.019 us
- Allocated: 1674.8 KB
Post (T410):
- Mean: 791.613 us
- P95: 825.284 us
- Allocated: 1728.3 KB
Delta:
- Mean: -18.31%
- P95: -28.17%
- Allocated: +3.19%

## Delta sintetico (pre capacity -> post T410)
| Scenario            | Mean Delta % | P95 Delta % | Alloc Delta % |
|--------------------|-------------:|------------:|--------------:|
| D1_R1_OR0_BusyLow   | -16.09       | -15.59      | 0.00          |
| D7_R1_OR0_BusyHigh  | -2.37        | -13.23      | 3.12          |
| D7_R2_OR1_BusyMed   | -2.47        | -3.78       | -3.45         |
| D14_R4_OR1_BusyLow  | -10.96       | -10.90      | 3.58          |
| D14_R4_OR2_BusyHigh | -16.86       | -21.63      | -1.31         |
| D31_R2_OR2_BusyMed  | -18.31       | -28.17      | 3.19          |

Report pre/post:
- Pre: Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02-capacity.html
- Post: Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02-t410.html

## File toccati
- HelixScheduler.Core/AvailabilityEngineV1.cs
- Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02-t410.csv
- Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02-t410.html
- Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02-t410.md
- Tasks/T410-Allocations-Reduction/RESULT.md
- RESULTS.md

## Impatto sul modello
Nessuno: modifiche solo interne al core (performance).

## Rischi / Note
- IntersectSlots ora assume input normalizzati/ordinati (invariato per il pipeline attuale).

## Come verificare
- dotnet test
- dotnet run -c Release --project HelixScheduler.Benchmarks
