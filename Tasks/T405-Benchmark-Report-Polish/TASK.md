# Task T405 — Benchmark Report Polish

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** in root come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md**.

Riferimenti canonici:
- Docs/context/PERFORMANCE.md
- Docs/context/PIPELINE.md

## Obiettivo
Raffinare l’output del lavoro T400 per renderlo:
- più leggibile
- più confrontabile nel tempo
- più utile per individuare regressioni

Senza introdurre ottimizzazioni del core.

## Scope
### IN
- Aggiungere percentili (p50/p95) o output equivalente dal runner benchmark
- Introdurre un concetto di "baseline scenario" stabile
- Aggiornare `REPORT.md` con sezione regressioni e come confrontare run diversi
- Eventuale salvataggio output raw BDN in file (opzionale)

### OUT
- Refactor/ottimizzazioni del motore
- Nuovi scenari complessi
- Modifiche architetturali

## Deliverables
- Aggiornamenti a `HelixScheduler.Benchmarks` per generare output più informativo
- Aggiornamento `Tasks/T400-Benchmark-Profiling/REPORT.md` (o nuovo report dedicato)
- `Tasks/T405-Benchmark-Report-Polish/RESULT.md` completo

## Vincoli
- Benchmark deterministici (seed fisso)
- Report breve e leggibile (focus su confronto)
- Nessun cambiamento funzionale al core

## Note
L’obiettivo è avere un formato “auditabile” per:
- vedere regressioni tra commit/versioni
- decidere se aprire task di ottimizzazione
