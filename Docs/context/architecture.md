# Architecture

## Componenti principali

### Scheduler Core\r
- calcolo disponibilitï¿½
- gestione regole e BusySlot
- intersezioni multi-risorsa
- completamente agnostico dal dominio

\n### Domain (Core)\n- il dominio del motore coincide con HelixScheduler.Core\n- modello + invarianti + algoritmo deterministico\n- nessuna dipendenza da EF/HTTP o dominio applicativo\n\r
### Application Layer
- servizi applicativi (availability, demo read/seed)
- dipende solo dal Core
- definisce le interfacce per accesso ai dati

### Domain Adapter (esterno)
- trasforma eventi reali in BusySlot
- filtra e seleziona risorse
- NON vive nel core

### Infrastructure / API
- DB, EF, Web API
- completamente separati

### Convenzioni API
- endpoint applicativi sotto prefisso `/api/*` (availability, catalog, demo, diag)
- health check esposto a `/health` per integrazioni di monitoring

## Boundary
Il core NON conosce:
- DB
- HTTP
- Identity
- UI

