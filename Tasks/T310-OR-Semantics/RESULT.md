# Result - T310 OR semantics (v1)

## Stato
DONE

## Sintesi
Supporto OR groups in availability: union per gruppo e intersezione finale,
con parsing/validazioni API e test Core/WebApi.

## File toccati
- HelixScheduler.Core/AvailabilityQuery.cs
- HelixScheduler.Core/AvailabilityEngine.cs
- HelixScheduler.Core/AvailabilityEngineV1.cs
- HelixScheduler/Controllers/AvailabilityController.cs
- HelixScheduler/Services/AvailabilityApplicationService.cs
- HelixScheduler.Core.Tests/AvailabilityEngineTests.cs
- HelixScheduler.Core.Tests/AvailabilityEngineV1Tests.cs
- HelixScheduler.WebApi.Tests/AvailabilityControllerTests.cs
- docs/context/QUERY_MODEL.md
- docs/adr/ADR-0005-OR-SEMANTICS-V1.md
- docs/codex/T310-or-semantics-v1.md

## Decisioni
- OR groups rappresentati come liste di resourceIds alternative
- Output resourceIds include l'insieme delle risorse coinvolte (ordinato)
- Parsing orGroups in WebApi con limiti di gruppo e totale
- ADR: docs/adr/ADR-0005-OR-SEMANTICS-V1.md
- Log: docs/codex/T310-or-semantics-v1.md

## Rischi / Note
- v1 non supporta OR nidificato
- output non indica quale risorsa del gruppo OR e' stata usata

## Come verificare
- dotnet test
- GET /api/availability/slots?...&orGroups=7,8|10,11
