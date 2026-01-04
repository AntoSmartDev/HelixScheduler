# Result — T492 Core ResourceType integration

## Stato
DONE

## Modifiche Core
- Introdotto `ResourceTypeId` come strong id nel Core.
- Aggiunta `ResourceDefinition` (ResourceId + TypeId) senza impatto sull’algoritmo.

## Modifiche DB/Infrastructure
- Nuove entità: `ResourceTypes`, `ResourceTypeProperties`.
- `Resources.TypeId` obbligatorio con FK → `ResourceTypes`.
- Migrations ripulite: baseline unica + snapshot aggiornato.
- Seed demo: resource types (Site/Room/Doctor), mapping tipo → property definition, `SeedVersion=2`.

## Modifiche Application/WebApi
- DTO resource type-aware (`typeId`, `typeKey`, `typeLabel`).
- Nuovi servizi read-only: `IResourceTypeCatalogService`, `IPropertySchemaService`.
- Validazione properties type-aware prima della compute availability.
- Nuovi endpoint catalogo: `/api/catalog/resources`, `/api/catalog/resource-types`, `/api/catalog/properties`.

## Modifiche DemoWeb
- Usa endpoint `/api/catalog/*` (non più `/demo/*`).
- Tipo risorsa visibile in lista e nella sezione “Properties of … (Type: …)”.
- Property filter basato su schema type-aware (definitions/nodes) e filtrato per tipi selezionati.

## Compatibilità
- Breaking change: rimosse API demo `/demo/resources` e `/demo/properties` (ora `/api/catalog/*`).
- Schema DB richiede migrazione e `Resources.TypeId` non nullo.
- Validazione properties rifiuta filtri incompatibili con i tipi risorsa selezionati.

## Come verificare
- Eseguire `dotnet build` dalla root.
- Avviare WebApi e DemoWeb, verificare:
  - `/api/catalog/resource-types` e `/api/catalog/properties` restituiscono i cataloghi.
  - `/api/catalog/resources` include `typeId/typeKey/typeLabel`.
  - DemoWeb mostra il tipo risorsa e filtra properties per tipo.
