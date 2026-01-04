# Task T400 — Benchmark & Profiling

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** in root come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md** per evitare duplicazioni o regressioni.

Documenti canonici di riferimento:
- Docs/context/ARCHITECTURE.md
- Docs/context/PIPELINE.md
- Docs/context/PERFORMANCE.md
- Docs/context/SECURITY_MODEL.md
- Docs/context/SCHEDULING_MODEL.md

## Obiettivo
Misurare prestazioni reali del calcolo availability (Core) e del percorso API (WebApi),
prima di introdurre ottimizzazioni o nuove feature invasive (es. capacity).

Il risultato deve produrre:
- numeri riproducibili (tempo, allocazioni)
- scenari rappresentativi (taglie e finestre temporali)
- indicazioni su colli di bottiglia e priorità d’intervento

## Scope
### IN
- Benchmark del Core (AvailabilityEngine + OR semantics + explainability off)
- Profiling di hot-path (allocazioni / CPU)
- (Opzionale) mini-benchmark end-to-end API

### OUT
- Ottimizzazioni premature (si fanno solo se misure evidenziano problemi)
- Refactor architetturali
- Cambi al modello dati

## Scenari benchmark (minimo)
Definire un set di scenari deterministici con dati sintetici:
- Window: 1 giorno, 7 giorni, 14 giorni, 31 giorni
- Risorse richieste (AND): 1, 2, 4
- Gruppi OR: 0, 1, 2 (con 2–5 risorse per gruppo)
- Densità busy: bassa / media / alta
- Regole: weekly ricorrenti + negative (alcune) + single date (alcune)

Dati sempre UTC e sempre normalizzati.

## Strumenti
Preferito: BenchmarkDotNet (progetto dedicato) per:
- mean/p50/p95
- allocazioni
- confronto tra scenari

Profiling:
- dotnet-trace / dotnet-counters / Visual Studio Profiler (documentare comandi/usato)

## Deliverables
- Nuovo progetto benchmark (es. `HelixScheduler.Benchmarks`)
- Report risultati (markdown) con:
  - hardware/ambiente
  - comandi
  - numeri per scenario
  - osservazioni (hotspot)
  - raccomandazioni (se e dove ottimizzare)
- `RESULT.md` aggiornato con sintesi + link ai report

## Vincoli
- Non modificare il comportamento funzionale
- Benchmark riproducibili (seed deterministico)
- Explainability disattivata durante benchmark Core (misurare “fast path”)
- Se emergono regression >20% rispetto a baseline, aprire task separato di ottimizzazione (non farlo dentro T400)

## Note
Il valore di T400 è la misura: anche “va tutto bene” è un risultato valido se documentato.
