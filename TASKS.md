# TASKS.md — Protocollo operativo HelixScheduler

## Regole globali (sempre)
1) Leggere e seguire `AGENTS.md` (root).
2) Considerare canonici i documenti elencati in `AGENTS.md` (docs/context/*).
3) Prima di iniziare un task, leggere `RESULTS.md` per evitare regressioni e duplicazioni.

## Flusso per ogni task
Per un task `Tasks/Txxx-Name/`:

1) Leggere:
   - `AGENTS.md`
   - `RESULTS.md`
   - `Tasks/Txxx-Name/TASK.md`
   - `Tasks/Txxx-Name/ACCEPTANCE.md`

2) Produrre un piano breve:
   - cosa farai
   - file che toccherai
   - rischi/edge

3) Implementare.

4) Verificare:
   - build
   - test (unit/integration se previsti)
   - endpoint/diagnostica se previsti

5) Scrivere/aggiornare:
   - `Tasks/Txxx-Name/RESULT.md` (obbligatorio)
   - eventualmente ADR in `docs/adr/` se cambia modello/architettura

## Divieti
- Non duplicare `AGENTS.md` nei task.
- Non introdurre dipendenze nel Core.
- Non cambiare il modello canonico senza ADR.