# T460 – WebApi Thin Controllers (Availability)

## Obiettivo
Ripulire i controller della WebApi in ottica **Clean Architecture** e **SOLID principles**,
rendendo i controller:
- sottili (orchestrazione HTTP בלבד)
- coerenti nelle route
- privi di parsing, validazioni procedurali e logica applicativa
- più leggibili come esempio OSS / reference architecture

⚠️ Nessuna modifica semantica:
- stessi input accettati
- stessi output
- stessi vincoli logici

⚠️ Nessuna modifica a HelixScheduler.Core.

---

## Contesto
`AvailabilityController` contiene oggi:
- parsing manuale (CSV, orGroups)
- validazioni procedurali
- gestione errori ripetitiva
- route incoerenti (absolute vs relative)

Questo rende il controller:
- troppo responsabile
- poco didattico
- non allineato a Clean Architecture

---

## Ambito
- HelixScheduler.WebApi
- (eventuale) HelixScheduler.Application **solo** per validazioni applicative riusabili
- Nessun impatto sul core

Nota clean architecture:
- Evitare riferimenti diretti a `DbContext` nel layer WebApi.
- Bootstrap (migrate/seed) deve passare da un servizio Application.

---

## Interventi richiesti

### 1) Controller thin (Single Responsibility)
`AvailabilityController` deve:
- ricevere input già validato
- mappare input → request applicativa
- invocare `IAvailabilityService`
- restituire `IActionResult`

Il controller **non deve**:
- parsare CSV
- iterare su collezioni per validare
- contenere regole di business

---

### 2) Input Model tipizzato
Creare un input model (WebApi layer), ad esempio:
`AvailabilitySlotsQuery`

Responsabilità:
- rappresentare la query HTTP
- delegare parsing/normalizzazione a componenti dedicati

Campi (equivalenti a quelli attuali):
- From / To
- ResourceIds
- PropertyIds
- OrGroups
- IncludeDescendants
- Explain

---

### 3) Parsing e normalizzazione separati
Il parsing oggi nel controller va spostato in:
- helper dedicato (`AvailabilityQueryParser`)
  oppure
- model binder custom

Scelta libera, ma:
- codice semplice
- testabile
- leggibile

---

### 4) Validazione separata
Le validazioni attuali **devono rimanere** (equivalenza funzionale):
- date obbligatorie e valide
- resourceIds obbligatori, positivi, deduplicati
- limiti quantitativi (resource <= 20, orGroups <= 5, ecc.)
- propertyIds positivi
- orGroups coerenti

La validazione:
- NON deve stare nel controller
- può stare nel WebApi layer o Application (validator dedicato)

---

### 5) Route coerenti (NO alias)
Uniformare le route eliminando percorsi assoluti incoerenti.

Schema finale:
- GET  /api/availability/slots
- POST /api/availability/compute

❌ Non mantenere route legacy
❌ Nessun alias
❌ Nessuna duplicazione

Questa è una **scelta architetturale intenzionale**.

---

### 6) Error handling minimale e pulito
Ridurre:
- try/catch duplicati
- stringhe hardcoded nel controller

Soluzioni ammesse:
- metodo helper privato nel controller
- exception specifica + filter locale

Scegliere la soluzione **più semplice**.

---

## Vincoli
- Non introdurre CQRS, MediatR, pipeline behaviors
- Non introdurre nuove dipendenze
- Non modificare HelixScheduler.Core
- Semplicità > “architettura perfetta”

---

## Riferimenti canonici
- Docs/context/PROJECT.md
- Docs/context/ARCHITECTURE.md
- Docs/context/DOMAIN_MODEL.md
- Docs/context/SCHEDULING_MODEL.md
- Docs/context/QUERY_MODEL.md
- Docs/context/PIPELINE.md
- Docs/context/PERFORMANCE.md
- Docs/context/SECURITY_MODEL.md
- Docs/context/GLOSSARY.md
