# Result – T460 WebApi Thin Controllers

## Stato
DONE

## Obiettivo
Pulizia AvailabilityController in ottica Clean Architecture e SOLID.

## Interventi eseguiti
- `AvailabilityController` ridotto a orchestration + error handling minimale.
- Input model tipizzato: `AvailabilitySlotsQuery`.
- Parsing separato: `AvailabilityQueryParser`.
- Validazione/normalizzazione separata: `AvailabilityQueryValidator`.
- Route coerenti: `GET /api/availability/slots`, `POST /api/availability/compute`.
- Nessun alias legacy mantenuto.
- `DiagController` ora usa `IDiagnosticsService` (Application) invece di `DbContext` diretto.
- Bootstrap migrate/seed via `IStartupInitializer` (Application) invece di `DbContext` in WebApi.
- Convenzione unificata: endpoint catalog/demo ora sotto prefisso `/api/`.

## Note
- Nessun alias legacy previsto
- Nessun impatto su HelixScheduler.Core
- Semantica invariata
- DemoWeb aggiornato a `/api/availability/compute`.
- Nessun accesso diretto a `SchedulerDbContext` nel layer WebApi (bootstrap via Application).
- Health resta su `/health` per compatibilita` monitoring.

## Verifica
- `GET /api/availability/slots` con gli stessi parametri di prima.
- `POST /api/availability/compute` con body invariato.
- Controller senza parsing/loop complessi.
