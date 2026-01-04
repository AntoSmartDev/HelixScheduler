# Result - T300 Explainability (v1)

## Stato
DONE

## Sintesi
Explainability v1 per availability: parametro explain, risposta estesa con slots
e explanations, logica in application layer senza modifiche al core.

## File toccati
- HelixScheduler/Controllers/AvailabilityController.cs
- HelixScheduler/Services/AvailabilityApplicationService.cs
- HelixScheduler/Services/AvailabilityExplainResponse.cs
- HelixScheduler.WebApi.Tests/AvailabilityControllerTests.cs
- docs/adr/ADR-0004-EXPLAINABILITY-V1.md
- docs/codex/T300-explainability-v1.md

## Decisioni
- Explainability best-effort in WebApi, core invariato
- Reason basata su applicabilita regole/negative/busy nel range
- ADR: docs/adr/ADR-0004-EXPLAINABILITY-V1.md
- Log: docs/codex/T300-explainability-v1.md

## Rischi / Note
- La reason e' conservativa: se busy e negative coesistono si privilegia busy
- Explainability solo per risultato vuoto (v1)

## Come verificare
- dotnet test
- GET /api/availability/slots?...&explain=true
