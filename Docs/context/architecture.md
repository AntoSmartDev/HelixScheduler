# Architecture

## Componenti principali

### Scheduler Core
- calcolo disponibilità
- gestione regole e BusySlot
- intersezioni multi-risorsa
- completamente agnostico dal dominio

### Domain Adapter (esterno)
- trasforma eventi reali in BusySlot
- filtra e seleziona risorse
- NON vive nel core

### Infrastructure / API
- DB, EF, Web API
- completamente separati

## Boundary
Il core NON conosce:
- DB
- HTTP
- Identity
- UI
