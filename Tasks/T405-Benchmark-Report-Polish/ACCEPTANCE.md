# Acceptance Criteria — T405 Benchmark Report Polish

## Report
- Il report include:
  - baseline scenario identificato chiaramente
  - p50 e p95 (o equivalente) per almeno i principali scenari
  - istruzioni "come confrontare due run"
  - una tabella compatta (tempo + allocazioni)

## Bench runner
- L’esecuzione produce output riproducibile:
  - `dotnet run -c Release --project HelixScheduler.Benchmarks`
- Se viene salvato output raw, è in un path stabile e documentato

## Non regressioni
- `dotnet build` verde
- `dotnet test` verde
