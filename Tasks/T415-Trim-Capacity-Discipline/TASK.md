# Task T415 — Trim / Capacity discipline (scratch lists)

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** in root come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md**.

Riferimenti canonici:
- Docs/context/PERFORMANCE.md
- Docs/context/PIPELINE.md
- Docs/context/ARCHITECTURE.md

---

## Contesto
Dopo T410 (Allocations Reduction), il core ha ottenuto un miglioramento CPU significativo
nel worst-case, con un lieve aumento delle allocazioni totali.

È plausibile che alcune **liste scratch interne**:
- crescano molto in scenari pessimi (range lunghi + OR + busy medio/alto)
- mantengano una `Capacity` elevata anche dopo il completamento del calcolo
- contribuiscano a **retained memory** non necessaria tra invocazioni successive

---

## Obiettivo
Introdurre una **disciplina minima e leggibile** per evitare che
le liste scratch interne mantengano capacity eccessive nel tempo.

L’obiettivo **non è ridurre allocazioni nel hot-path**,
ma limitare la memoria trattenuta dopo casi pessimi.

---

## Vincoli (non negoziabili)
- Nessuna modifica semantica:
  - availability invariata
  - OR semantics invariata
  - Capacity (v1) invariata
- Nessuna modifica algoritmica:
  - no caching
  - no pooling
  - no parallelismo
- Nessuna modifica alle API pubbliche
- Interventi ammessi solo se:
  - locali
  - facili da leggere
  - giustificabili con 1–2 righe di commento

---

## Approccio (obbligatorio)
1) Identificare **1–2 scratch list** interne al Core che:
   - possono crescere molto
   - vengono riutilizzate tra chiamate o cicli
2) Applicare una **disciplina post-uso**:
   - lasciare crescere liberamente durante il calcolo
   - **a fine operazione**, se la `Capacity` supera una soglia:
     - re-inizializzare la lista con una capacity piccola
3) Usare **una sola soglia globale** (es. 4096 o 8192 elementi)

---

## Strategia consigliata
**Recreate-on-threshold (pattern semplice e leggibile)**

Esempio concettuale (non vincolante):

```csharp
if (scratch.Capacity > MaxScratchCapacity)
{
    scratch = new List<TimeRange>(DefaultScratchCapacity);
}
