# Acceptance Criteria — T200 Samples.Medical

## Funzionale
- È possibile selezionare una data e avviare la ricerca
- La UI chiama correttamente:
  `GET /api/availability/slots`
- Gli slot restituiti vengono visualizzati correttamente

## Comportamento atteso
- Caso con risultati:
  - vengono mostrati tutti gli slot ordinati
- Caso senza risultati:
  - messaggio “Nessuna disponibilità”
- Caso errore (400):
  - messaggio chiaro per l’utente

## Architetturale
- Samples.Medical non contiene logica di dominio
- Nessuna duplicazione del motore di scheduling
- Comunicazione solo via HTTP API

## Verifica
- Avvio progetto Samples.Medical
- Chiamata API manuale di confronto
- Verifica visiva output coerente con API
