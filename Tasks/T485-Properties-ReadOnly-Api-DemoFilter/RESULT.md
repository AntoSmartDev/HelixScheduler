# Result — T485 Properties read-only API + Demo filter

## Stato
DONE

## Endpoint aggiunti
- `GET /demo/resources?onlySchedulable=true` → risorse con properties associate.
- `GET /demo/properties` → catalogo properties (id, key, label, parentId, sortOrder).

## Modifiche DemoWeb (UX)
- Select properties con catalogo e chips selezione.
- Include descendants applicato al filtro proprietà (client-side subtree).
- Lista risorse filtrata per property; properties visibili sotto la risorsa selezionata.

## Modifiche Application/Infrastructure
- Application: `ResourceCatalogService` + DTO `ResourceWithPropertiesDto`/`ResourcePropertyDto`.
- Porta read-only: `IResourceCatalogDataSource`.
- Infrastructure: query EF read-only (3 query, no N+1) + `ResourceCatalogDataSource`.

## Note prestazionali
- 3 query aggregate: resources, property links, properties. Nessun N+1.

## Impatto architetturale
- Core: none
- Application: read-only services/DTO
- Infrastructure: read-only queries
- WebApi: read-only endpoints

## Come verificare
- Stop WebApi/DemoWeb per sbloccare i binari.
- `dotnet build`
- `dotnet test`
- Avviare WebApi + DemoWeb, selezionare una property dalla select e verificare filtro/props.\r\n\n## Fix successivi\r\n- Normalizzazione property Key su reset (child.Key = parent.Key) per evitare duplicati logici.\r\n- Cleanup duplicati su ResourceProperties/ResourcePropertyLinks.\r\n\r\n\n## Note architettura\r\n- Domain = Core (note esplicite in Docs/context/ARCHITECTURE.md e README.md).\r\n\r\n\n## Aggiornamenti UI/Api\r\n- DemoWeb: status bar API nascosto quando ready, warning quando offline.\r\n- WebApi: rimosso WeatherForecast, homepage ridotta a OpenAPI JSON + Health.\r\n
## Traccia modifiche extra
- Health endpoint /health (status/utc/version) usato dal wait-for-api in DemoWeb.
- DemoWeb status bar: retry/backoff, warning/error, hidden when ready.
- WebApi homepage: solo link OpenAPI JSON + Health.
- WeatherForecastController rimosso.
- Normalizzazione key property su reset + cleanup duplicati.
- Properties UI: prefisso parent label e banner "Properties of resource".

