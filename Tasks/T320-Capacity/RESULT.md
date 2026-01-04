# Result - T320 Capacity (v1)

## Stato
DONE

## Piano sintetico
- Aggiungere Capacity alle risorse (DB + entity + config).
- Passare capacity al core e applicarla ai busy overlap.
- Coprire casi capacity=1, >1 e OR groups con test.

## File toccati
- HelixScheduler.Core/AvailabilityInputs.cs
- HelixScheduler.Core/AvailabilityEngineV1.cs
- HelixScheduler.Core.Tests/AvailabilityEngineV1CapacityTests.cs
- HelixScheduler.Infrastructure/Persistence/Entities/Resources.cs
- HelixScheduler.Infrastructure/Persistence/Configurations/ResourcesConfiguration.cs
- HelixScheduler.Infrastructure/Persistence/Repositories/IResourceRepository.cs
- HelixScheduler.Infrastructure/Persistence/Repositories/ResourceRepository.cs
- HelixScheduler.Infrastructure/ServiceCollectionExtensions.cs
- HelixScheduler.Infrastructure/Migrations/20260102213000_AddResourceCapacity.cs
- HelixScheduler.Infrastructure/Migrations/20260102213000_AddResourceCapacity.Designer.cs
- HelixScheduler.Infrastructure/Migrations/SchedulerDbContextModelSnapshot.cs
- HelixScheduler/Services/AvailabilityApplicationService.cs
- HelixScheduler.WebApi.Tests/CustomWebApplicationFactory.cs
- HelixScheduler.WebApi.Tests/AvailabilityControllerTests.cs
- docs/context/DOMAIN_MODEL.md
- docs/context/SCHEDULING_MODEL.md
- docs/codex/T320-capacity-v1.md
- Tasks/T320-Capacity/RESULT.md
- RESULTS.md

## Output
- Capacity per-resource con default 1 e vincolo >= 1.
- Availability v1 blocca solo segmenti con occupancy >= capacity.
- OR groups compatibili (union calcolata per resource con capacity).

## Impatto sul modello
- Estesa Resource con Capacity (int, default 1).
- Busy events consumano 1 unita' di capacity.

## Rischi / Note
- Confronti manuali richiedono risorsa con capacity > 1 e busy sovrapposti.
- Missing capacity in DB defaulta a 1 nel core.

## Come verificare
- dotnet build
- dotnet test
- Test manuale: impostare una resource con Capacity=2 e creare due busy sovrapposti; lo slot deve risultare non disponibile solo nel tratto di doppio overlap.
