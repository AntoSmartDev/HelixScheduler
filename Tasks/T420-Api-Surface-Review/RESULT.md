# Result - T420 API Surface Review

## Stato
DONE

## Piano sintetico
- Mappata la surface pubblica Core e identificati foot-gun migliorabili senza refactor.
- Aggiunte XML docs mirate per chiarire UTC, periodi inclusivi, e semantica AND/OR.
- Inseriti guardrail cheap per null entries in AvailabilityQuery.

## Modifiche API (pubbliche)
- XML docs per entry point principali e tipi core (query/inputs/result/slot/range/rule).
- AvailabilityQuery: validazione esplicita di `propertyFilters` e `resourceOrGroups` per entry null.
- Nuovo documento `docs/context/API_SURFACE.md` con principi e changelog.
- Link aggiunto in `docs/context/PROJECT.md`.
- AvailabilityInputs: validazione esplicita di `rules` e `busySlots` per entry null.

## Compatibilita / Breaking changes
- Nessun breaking change su input validi.
- Nuove eccezioni con messaggi chiari in caso di liste contenenti null (comportamento gia non supportato).

## Impatto misurato (benchmark)
- Eseguito benchmark singolo post-change; risultati in `BenchmarkDotNet.Artifacts/results/AvailabilityBenchmarks-report.html`.
- Nessun impatto atteso sul hot-path (solo docs + guardrail in constructor).

## File toccati
- `HelixScheduler.Core/AvailabilityEngine.cs`
- `HelixScheduler.Core/AvailabilityEngineV1.cs`
- `HelixScheduler.Core/AvailabilityInputs.cs`
- `HelixScheduler.Core/AvailabilityMode.cs`
- `HelixScheduler.Core/AvailabilityQuery.cs`
- `HelixScheduler.Core/AvailabilityResult.cs`
- `HelixScheduler.Core/BusySlot.cs`
- `HelixScheduler.Core/BusySlotModel.cs`
- `HelixScheduler.Core/DatePeriod.cs`
- `HelixScheduler.Core/PropertyFilter.cs`
- `HelixScheduler.Core/RuleModel.cs`
- `HelixScheduler.Core/SchedulingRule.cs`
- `HelixScheduler.Core/TimeRange.cs`
- `HelixScheduler.Core/UtcSlot.cs`

## Impatto sul modello
- Nessuno.

## Rischi / Note
- Guardrail aggiuntivi possono far emergere input null invalidi prima (messaggio piu esplicito).

## Come verificare
- `dotnet build`
- `dotnet test`
- `dotnet run -c Release --project HelixScheduler.Benchmarks`
