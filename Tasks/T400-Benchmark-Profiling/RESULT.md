# Result - T400 Benchmark & Profiling

## Stato
DONE

## Sintesi
Creato progetto BenchmarkDotNet per il core availability con scenari deterministici
e report con numeri (tempo/allocazioni) e osservazioni.

## File toccati
- HelixScheduler.Benchmarks/HelixScheduler.Benchmarks.csproj
- HelixScheduler.Benchmarks/Program.cs
- HelixScheduler.Benchmarks/AvailabilityBenchmarks.cs
- HelixScheduler.slnx
- Tasks/T400-Benchmark-Profiling/REPORT.md

## Risultati (numeri)
Vedi report: Tasks/T400-Benchmark-Profiling/REPORT.md
CSV run archiviato: Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.csv
Report HTML archiviato: Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.html
Report Markdown archiviato: Tasks/T405-Benchmark-Report-Polish/artifacts/AvailabilityBenchmarks-report-2026-01-02.md

## Hotspot / Osservazioni
- Tempo/allocazioni crescono con window size e complessita' OR
- Busy density alta aumenta sottrazioni/normalizzazioni
- Scenario D31_R2_OR2_BusyMed ~0.9 ms/op (mean)

## Raccomandazioni
- Misurare regressioni >20% prima di ottimizzare
- Profiling mirato su Normalize/Intersect/Union se necessario

## Come eseguire
- dotnet run -c Release --project HelixScheduler.Benchmarks
