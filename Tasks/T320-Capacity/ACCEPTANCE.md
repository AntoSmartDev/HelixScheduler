# Acceptance Criteria — T320 Capacity (v1)

## Non regressione
- Con tutte le risorse a capacity=1:
  - output availability invariato rispetto a prima
  - test esistenti verdi

## DB/Metadata
- Esiste campo `Capacity` su Resource:
  - default 1
  - vincolo >= 1

## Funzionale (Core)
- Caso A (capacity=1):
  - busy overlap rende slot non disponibile come oggi
- Caso B (capacity=2):
  - 1 busy non blocca (occupancy=1 < 2) -> slot resta disponibile
  - 2 busy sovrapposti bloccano (occupancy=2 !< 2) -> slot non disponibile
- Caso C (capacity=3, busy denso):
  - availability corretta per segmenti

## OR semantics compatibile
- In presenza di orGroups:
  - ogni risorsa del gruppo calcola availability con la sua capacity
  - union del gruppo produce slot se esiste almeno una risorsa con capacity residua

## API
- Nessun breaking change negli endpoint
- (Se serve) endpoint che ritorna capacity non richiesto in v1

## Test/Verifica
- `dotnet build`
- `dotnet test`
- test manuale: scenario con risorsa capacity=2 e due busy sovrapposti
