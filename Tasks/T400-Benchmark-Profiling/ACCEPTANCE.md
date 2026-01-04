# Acceptance Criteria — T400 Benchmark & Profiling

## Funzionale
- Esiste un progetto benchmark eseguibile da CLI (es. BenchmarkDotNet)
- Gli scenari minimi sono coperti (window, AND count, OR groups, busy density)
- I dati sono deterministici (seed fisso)

## Output richiesto
- Report markdown con:
  - ambiente (CPU/RAM/OS/.NET)
  - comandi per eseguire benchmark
  - tabella risultati per scenario (tempo + allocazioni)
  - 3–5 hotspot / osservazioni
  - raccomandazioni (solo proposte, niente refactor dentro T400)

## Non regressioni
- `dotnet build` verde
- `dotnet test` verde

## Verifica
- Esecuzione benchmark:
  - `dotnet run -c Release --project <Benchmarks>`
- (Opzionale) profiling:
  - comandi e output documentati
