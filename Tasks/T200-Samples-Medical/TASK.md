# Task T200 — Samples.Medical (UI di verifica)

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** in root come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md** per conoscere lo stato attuale del progetto ed evitare regressioni.

Documenti canonici di riferimento (come da AGENTS.md):
- Docs/context/PROJECT.md
- Docs/context/ARCHITECTURE.md
- Docs/context/DOMAIN_MODEL.md
- Docs/context/SCHEDULING_MODEL.md
- Docs/context/QUERY_MODEL.md
- Docs/context/PIPELINE.md
- Docs/context/PERFORMANCE.md
- Docs/context/SECURITY_MODEL.md
- Docs/context/GLOSSARY.md

## Obiettivo
Creare una **UI MVC minimale** (Samples.Medical) che consumi l’API
`GET /api/availability/slots` per **verificare visivamente** il comportamento
del motore di scheduling.

La UI ha solo scopo:
- di verifica funzionale
- di demo
- di supporto allo sviluppo

Non è una UI finale né un prodotto.

## Scope
### IN
- Utilizzo del progetto `HelixScheduler.Samples.Medical`
- Pagina MVC singola per ricerca disponibilità
- Chiamata HTTP all’API HelixScheduler
- Visualizzazione semplice degli slot restituiti

### OUT
- Nuove feature del motore
- Refactor Core / Infrastructure
- Gestione utenti / autenticazione
- UI avanzata (styling, calendari complessi)

## Funzionalità richieste
- Selezione:
  - data (singolo giorno)
  - risorse (Doctor, Room — anche hardcoded inizialmente)
- Pulsante “Cerca disponibilità”
- Chiamata a `/api/availability/slots`
- Rendering risultati:
  - lista ordinata di slot (es. 14:00–15:00, 16:00–18:00)
- Gestione errori base (400 → messaggio utente)

## Vincoli
- Samples.Medical usa l’API come **client esterno**
- Nessuna logica di scheduling nella UI
- Nessuna dipendenza diretta da Infrastructure o Core
- Nessuna modifica al DB o ai repository

## Deliverables
- Pagina MVC funzionante
- Codice chiaro e leggibile
- `RESULT.md` con sintesi e note operative
