# T400 Benchmark & Profiling Report

## Ambiente
- OS: Windows 11 (10.0.26200)
- CPU: 12th Gen Intel(R) Core(TM) i7-1255U (10C/12T)
- RAM: 16 GB
- .NET SDK: 10.0.101
- Runtime: .NET 10.0.1, x64 RyuJIT AVX2

## Comandi
- Benchmark: `dotnet run -c Release --project HelixScheduler.Benchmarks`
- Profiling (opzionale):
  - `dotnet-counters monitor --process-id <pid>`
  - `dotnet-trace collect --process-id <pid> --providers Microsoft-DotNETCore-SampleProfiler`

## Scenari
Nome = D{days}_R{required}_OR{groups}_Busy{density}
- D1_R1_OR0_BusyLow
- D7_R1_OR0_BusyHigh
- D7_R2_OR1_BusyMed
- D14_R4_OR1_BusyLow
- D14_R4_OR2_BusyHigh
- D31_R2_OR2_BusyMed

## Baseline
Baseline scenario: D7_R2_OR1_BusyMed

## Risultati (BenchmarkDotNet)
| Scenario            | Mean (us) | P50 (us) | P95 (us) | Alloc (KB) |
|--------------------|-----------:|---------:|---------:|-----------:|
| D1_R1_OR0_BusyLow   | 1.824      | 1.831    | 1.853    | 6.16       |
| D7_R1_OR0_BusyHigh  | 13.739     | 13.816   | 14.558   | 28.70      |
| D7_R2_OR1_BusyMed   | 28.751     | 28.820   | 31.651   | 67.60      |
| D14_R4_OR1_BusyLow  | 90.757     | 89.924   | 98.224   | 189.67     |
| D14_R4_OR2_BusyHigh | 288.617    | 283.646  | 321.688  | 576.63     |
| D31_R2_OR2_BusyMed  | 790.675    | 779.852  | 879.610  | 1658.16    |

Note: valori da BenchmarkDotNet (mean). Alloc include allocazioni managed per operazione.

## Hotspot / Osservazioni
1) Tempo e allocazioni crescono con la finestra (days) e con la complessita' OR.
2) Busy density alta aumenta il lavoro di sottrazione/normalizzazione.
3) Scenario piu' pesante (31 giorni, OR2) resta sotto 1 ms per operazione.
4) Alcuni scenari mostrano distribuzioni multi-modali (da report BDN).

## Raccomandazioni
1) Misurare dopo ogni feature che aumenta risorse/orGroups per evitare regressioni >20%.
2) Se necessario, indagare allocazioni in Normalize/Intersect/Union con dotnet-trace.

## Confronto tra run
1) Eseguire benchmark con comando standard.
2) Salvare CSV da `BenchmarkDotNet.Artifacts/results/AvailabilityBenchmarks-report.csv`.
3) Confrontare Mean/P50/P95 con la baseline (D7_R2_OR1_BusyMed) e segnalare delta >20%.
4) Artifact run archiviati:
   - Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.csv
   - Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.html
   - Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.md
