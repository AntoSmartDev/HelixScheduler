# Acceptance Criteria — T415 Trim / Capacity discipline

## Non regressione
- `dotnet build` verde
- `dotnet test` verde
- Output availability invariato (nessun cambiamento di contratti o semantica)

---

## Semantica
- OR semantics invariata
- Capacity (v1) invariata
- Explainability invariata (on/off)
- Nessuna modifica a risultati, ordering o slicing degli slot

---

## Performance
- Rieseguibili senza modifiche i benchmark T400/T405
- Nessun peggioramento CPU > 5% su Mean o P95
- Allocazioni totali comparabili a T410 (± tolleranza minima)
- Nessun peggioramento evidente di throughput nei casi base

---

## Memoria
- Disciplina applicata **solo** a scratch list interne
- Presenza di **una sola soglia** di capacity
- Intervento **post-uso** (non nel hot-path)
- Nessun pooling o riuso globale di oggetti

---

## Complessità
- Nessuna nuova dipendenza
- Nessuna modifica alle API pubbliche
- Nessun refactor strutturale
- Codice locale, leggibile, con breve commento esplicativo

---

## Output
- `RESULT.md` aggiornato con:
  - punti di intervento
  - motivazione
  - osservazioni pre/post (anche qualitative)
