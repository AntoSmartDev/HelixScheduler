# Acceptance Criteria – T460

## Controller
- AvailabilityController è thin:
  - nessun parsing CSV
  - nessuna validazione procedurale
  - nessun loop complesso
- Il controller orchestra e basta.

## Input & Validation
- Esiste un input model tipizzato per la query availability.
- Parsing e validazione sono separati dal controller.
- Le regole di validazione sono equivalenti a quelle precedenti.

## Routing
- Route coerenti e univoche:
  - /api/availability/slots
  - /api/availability/compute
- Nessuna route assoluta incoerente.
- Nessun alias legacy.

## Qualità
- Codice più leggibile e didattico.
- Responsabilità ben separate.
- Nessun impatto su HelixScheduler.Core.

## Non Obiettivi
- Nessuna modifica al formato delle risposte.
- Nessun cambiamento semantico.
- Nessuna introduzione di CQRS / Mediator.
