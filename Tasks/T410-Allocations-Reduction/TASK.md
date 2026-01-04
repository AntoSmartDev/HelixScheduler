# Task T410 — Allocations Reduction (profiling-driven)

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** in root come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md**.

Riferimenti canonici:
- Docs/context/PERFORMANCE.md
- Docs/context/PIPELINE.md
- Docs/context/ARCHITECTURE.md

## Obiettivo
Ridurre allocazioni e overhead nel calcolo availability (Core), soprattutto nei casi “lunghi”
(es. 31 giorni con OR e busy medio/alto), senza cambiare alcun comportamento funzionale.

Target prioritario:
- scenario peggiorato dopo Capacity: `D31_R2_OR2_BusyMed`

## Vincoli (non negoziabili)
- Nessuna modifica semantica:
  - OR semantics invariata
  - Capacity invariata
  - availability ≠ assignment
- Nessun refactor algoritmico “grande”:
  - no nuove architetture
  - no caching
  - no parallelismo
- Interventi ammessi solo se:
  - migliorano misure pre/post
  - mantengono test verdi

## Approccio (obbligatorio)
1) Profiling leggero per identificare 2–3 hotspot:
   - dotnet-trace / VS Profiler (documentare comandi)
2) Interventi **low-complexity**:
   - rimozione LINQ nel hot-path
   - riduzione liste temporanee (riuso buffer)
   - normalizzazione/merge in-place
   - evitare materializzazioni ripetute
3) Validazione numerica:
   - rieseguire benchmark T400/T405 (pre/post)
   - confrontare almeno: Mean, P95, Allocated

## Checklist interventi consentiti (preferenza)
- Sostituire LINQ con loop `for`
- Preallocare `List<T>` con capacity stimata quando noto
- Riusare “scratch lists” dentro il metodo (Clear + reuse)
- Ridurre ToArray/ToList intermedi
- Usare struct per range se non già presente (solo se non complica)
- Evitare boxing/allocazioni di enumerator in loop caldi

## Deliverables
- Patch nel Core con ottimizzazioni low-risk
- Documento `RESULT.md` con:
  - hotspot trovati
  - cosa cambiato e perché
  - numeri benchmark pre/post (D31 obbligatorio)
- Se necessario, mini ADR solo se introduci un pattern tecnico nuovo (es. pooling)

## Note
Se dopo il profiling non emergono hotspot “facili”, il task può essere chiuso con esito:
“nessun intervento utile senza aumentare complessità”.
