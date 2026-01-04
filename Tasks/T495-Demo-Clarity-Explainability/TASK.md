# T495 – Demo Clarity & Explainability

## Obiettivo
Migliorare la comprensibilità e la capacità esplicativa della demo web di HelixScheduler,
rendendo immediato per l’utente:
- come viene calcolata la disponibilità
- perché uno slot esiste o non esiste
- come le regole contribuiscono alla generazione degli slot
- la differenza tra “Explorer” e “Search”

⚠️ Questo task è **UI / Demo only**.
⚠️ Nessuna modifica al core di HelixScheduler.
⚠️ Nessuna modifica semantica ai risultati.

---

## Ambito
- HelixScheduler.DemoWeb
- (eventuali DTO/UI helper già esistenti)
- Nessun impatto su:
  - HelixScheduler.Core
  - HelixScheduler.Application (logica)
  - Algoritmi di scheduling

---

## Contesto funzionale
- Uno slot può essere generato da **più rules**
- Le rules **contribuiscono** agli slot, non li producono singolarmente
- Availability = intersezione delle regole applicabili − busy intervals
- Explainability è già disponibile a livello concettuale

---

## Interventi richiesti

### 1. Query Summary (rafforzamento semantico)
Nelle pagine:
- Availability Explorer
- Search Availability

Il box “Current availability query” deve:
- descrivere l’intento della query in linguaggio naturale
- NON elencare le rules
- riflettere ResourceType + Property filters

Esempio:
- “Looking for availability where Doctor = any AND Specialization = Cardiology”

---

### 2. Spiegazione del modello di calcolo
Subito sotto il titolo “Availability (UTC)” aggiungere una breve sezione testuale:

- Descrizione sintetica del flusso:
  - Rules → finestre candidate
  - Intersezione delle regole
  - Sottrazione busy intervals
  - Produzione slot finali

Testo breve, statico, UI-only.

---

### 3. Relazione Slot ↔ Rules (multi-rule aware)
Aggiornare le interazioni UI per riflettere che:

- uno slot può derivare da più rules
- una rule può contribuire a più slot

Comportamenti richiesti:
- Hover / click su slot → elenco “Contributing rules”
- Hover / click su rule → “This rule contributes to N slots in the selected range”

⚠️ Solo testo/UI, nessuna nuova logica di calcolo.

---

### 4. Explainability (copy + micro UX)
Quando Explain è ON:
- chiarire perché uno slot esiste (rules applicate)
- chiarire perché uno slot è escluso (busy o mancanza di intersezione)

Formato libero, ma leggibile e coerente con l’attuale UI.

---

### 5. Distinzione Explorer vs Search
Rendere esplicito il ruolo delle due pagine:

- Availability Explorer → esplorazione risorse specifiche
- Search Availability → ricerca per tipo e proprietà

Bastano sottotitoli o micro copy, senza aggiungere navigazione complessa.

---

## Requisiti aggiuntivi (UI-only)

### 6. Properties UI (Opzione A — Group by Resource Type)
- La sezione “Properties” NON deve mostrare proprietà mischiate in un unico elenco.
- Le property definitions vanno raggruppate per ResourceType (Doctor, Room, ...).
- Mostrare SOLO le property definitions applicabili a quel tipo (type-aware).
- Rendere evidente che “Properties are defined per resource type”.
- Le selezioni continuano a generare i chip/filtri attuali (nessun cambio semantico).
- Explorer: properties read-only sotto la risorsa restano, ma con framing coerente (“Doctor properties / Room properties”) e stile uniforme.

### 7. Micro-copy di chiarezza (minimo)
- Search Availability: sotto “Properties” (o sopra i gruppi) aggiungere:
  “Properties are defined per resource type.”
- Explorer: evitare wording “DB-like”; preferire “Doctor properties / Room properties” con label semplice.

### 8. Nessuna nuova logica
- Solo rendering/UI.
- Nessuna modifica a Core o semantica.

---

## Vincoli
- Nessuna modifica al core
- Nessun pattern nuovo
- Nessuna animazione complessa
- Nessun wizard
- Semplicità > estetica

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
