# Result — T493 Search Availability + UX clarity

## Stato
DONE

## Search Availability (/search)
- Aggiunta pagina `search.html` con query builder type-driven.
- Ogni requirement supporta: tipo, risorsa specifica (opzionale), filtri properties coerenti per tipo, include descendants.
- Preview candidates per blocco con conteggio + lista limitata.
- Compute usa `POST /api/availability/compute` con OR groups per “Any of type”; risultati mostrati con calendario + busy/rules (demo summary).
- Prev/Next settimana ricalcola automaticamente la ricerca.
- Tooltip slot in search allineati all’Explain (solo quando attivo).
- Explain availability abilitato di default.
- Include descendants spiegato con tooltip info (UI).

## UX improvements (Availability page)
- Query Summary box sotto “Availability (UTC)” con risorse/properties/range/explain.
- Micro-copy più esplicita su availability vs busy.
- Risorse raggruppate per tipo con pill tipo (TypeLabel/Key).
- Explain availability abilitato di default.
- Tooltip slot: testo ASCII (no caratteri speciali) per evitare problemi di encoding.
- Link “Search Availability” senza stato visited e “Reset Demo” meno in evidenza.

## Endpoint /api/catalog usati o aggiunti
- Usati: `/api/catalog/resource-types`, `/api/catalog/resources`, `/api/catalog/properties`.
- Nessun endpoint nuovo aggiunto.

## Note compatibilità
- Nessuna modifica a Core o semantica availability.
- Search page usa cataloghi read-only + availability compute esistente.
- Availability compute gestisce query con solo OR groups (no required resourceIds).

## Come verificare
- Avvia DemoWeb e WebApi.
- Apri `https://localhost:7040/search.html` e verifica:
  - aggiunta requirements e preview candidates per tipo
  - filtri properties coerenti per tipo
  - compute availability mostra calendario + busy/rules
- Apri `https://localhost:7040/index.html` e verifica Query Summary + raggruppamento per tipo.


