# Acceptance Criteria — T420 API Surface Review

## Non regressione
- `dotnet build` verde
- `dotnet test` verde
- Output availability invariato (nessun cambiamento di semantica/contratti)

---

## API / Compatibilità
- Nessuna modifica semantica a:
  - OR semantics
  - Capacity (v1)
  - Explainability
- Breaking change evitati; se presenti:
  - devono essere minimi, motivati e documentati in RESULT.md
  - preferire overload/alias/Obsolete rispetto a rimozioni

---

## Performance
- Rieseguibili benchmark T400/T405 senza modifiche
- Nessun peggioramento CPU > 5% su Mean o P95 nei benchmark esistenti
- Allocazioni comparabili a T410 (± tolleranza minima)

---

## Qualità
- Modifiche localizzate e leggibili
- Nessuna nuova dipendenza
- XML docs solo dove aggiungono chiarezza (API pubbliche principali)

---

## Output
- `RESULT.md` aggiornato con:
  - lista cambi API e motivazione
  - file toccati
  - note su compatibilità
  - come verificare (comandi)
