# Acceptance Criteria — T485 Properties read-only API + Demo filter

## Architettura
- Nessuna modifica a `HelixScheduler.Core`
- Application layer usato per DTO e servizi read-only
- Infrastructure implementa una porta read-only (no N+1, query efficienti)
- WebApi controller thin

## API
- `GET /demo/resources` disponibile e restituisce risorse con properties associate
- (Opzionale se implementato) `GET /demo/properties` restituisce catalogo properties/gerarchia
- Endpoint read-only, documentati brevemente nel README o in RESULT

## DemoWeb UI
- Alla selezione di una risorsa è chiaramente visibile l’elenco delle sue properties
- Le properties sono presentate in modo leggibile (badge/tag, grouping se necessario)
- Filtro properties presente e utilizzabile

## Funzionalità
- Nessun CRUD
- Filtering properties dimostra includeDescendants (se gerarchia disponibile)
- Availability e busy continuano a funzionare come prima (solo filtri lato UI/query)

## Qualità
- `dotnet build` verde
- `dotnet test` verde
- UX non degradata; rendering fluido
- RESULT.md aggiornato
- Entry in RESULTS.md aggiornata
