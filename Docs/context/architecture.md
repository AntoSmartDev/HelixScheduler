# Architecture

## Macro-componenti
1) Scheduler Core
- Algoritmi di calcolo slot
- Intersezioni disponibilità risorse
- Risoluzione conflitti
- Output: lista slot disponibili (con metadati minimi)

2) Domain Adapters
- Trasforma eventi/entità di dominio in input scheduler
- Normalizzazione: indisponibilità, prenotazioni, vincoli

3) Persistence/Infrastructure (facoltativo)
- repository, caching, storage
- MAI “mescolare” query DB con hot-path del core

## Boundary
- Scheduler Core non conosce DB, HTTP, UI.
- Scheduler Core riceve dati già normalizzati e coerenti.

## Tipi di input
- Availability windows (per risorsa)
- Busy intervals (prenotazioni)
- Negative events (blocchi, ferie, manutenzione)
- Constraints (hard/soft) — definire evoluzione in ADR quando introdotti

## Evoluzione
Ogni scelta che cambia boundary o modello dei dati -> ADR.