# Result - T405 Benchmark Report Polish

## Stato
DONE

## Piano sintetico
- Aggiungere percentili (P50/P95) nel runner benchmark.
- Dichiarare una baseline scenario stabile nel report.
- Documentare come confrontare run diversi.

## File toccati
- HelixScheduler.Benchmarks/AvailabilityBenchmarks.cs
- Tasks/T400-Benchmark-Profiling/REPORT.md
- Tasks/T405-Benchmark-Report-Polish/RESULT.md
- RESULTS.md

## Output aggiunto
- Tabelle report con Mean/P50/P95/Allocazioni.
- Baseline scenario dichiarata: D7_R2_OR1_BusyMed.
- Istruzioni di confronto tra run con CSV BDN.
- Artifact archiviati: Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.csv
- Artifact archiviati: Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.html
- Artifact archiviati: Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.md

## Impatto sul modello
Nessun impatto sul modello core; solo benchmark/report.

## Rischi / edge case
- Percentili sensibili al rumore: confrontare run su macchina comparabile.
- CSV path dipende da output BDN: usare percorso documentato nel report.

## Come verificare
- dotnet build
- dotnet test
- dotnet run -c Release --project HelixScheduler.Benchmarks
