# Task T310 — OR semantics (v1)

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** in root come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md** per evitare duplicazioni o regressioni.

Documenti canonici di riferimento:
- Docs/context/PROJECT.md
- Docs/context/ARCHITECTURE.md
- Docs/context/DOMAIN_MODEL.md
- Docs/context/SCHEDULING_MODEL.md
- Docs/context/QUERY_MODEL.md
- Docs/context/PIPELINE.md
- Docs/context/PERFORMANCE.md
- Docs/context/SECURITY_MODEL.md
- Docs/context/GLOSSARY.md

## Obiettivo
Introdurre la semantica **OR** nella query availability, permettendo di esprimere:
- disponibilità per alternative di risorse (es. Doctor A **oppure** Doctor B)
- mantenendo compatibilità con il comportamento attuale (**AND/Intersection**) quando non richiesto OR

L’obiettivo è una v1 semplice e non ambigua.

## Scope
### IN
- Estensione del modello di query per supportare gruppi OR
- Aggiornamento del motore (Core) per calcolare OR in modo deterministico
- Aggiornamento WebApi per accettare il nuovo input e costruire la query corretta
- Test unitari (Core) e almeno un test di integrazione API

### OUT
- OR nidificato arbitrariamente (v2)
- UI complessa in Samples
- Ottimizzazioni avanzate (prima misuriamo in D)

## Specifica (v1): Gruppi OR di risorse
La richiesta availability può includere:
- `requiredResourceIds` (AND come oggi)
- `resourceOrGroups`: lista di gruppi, dove ogni gruppo contiene una lista di resourceIds alternative

Semantica:
- Una soluzione è valida se:
  - include **tutte** le risorse obbligatorie (requiredResourceIds)
  - per ogni gruppo OR, soddisfa **almeno una** risorsa del gruppo
- Il risultato finale è l’intersezione tra:
  - disponibilità delle risorse obbligatorie
  - e la **union** delle disponibilità delle risorse alternative di ciascun gruppo

Formalmente:
- A = ∩ Availability(requiredResources)
- Per ogni group Gᵢ = {r1, r2, ...}:
  - Oᵢ = ∪ Availability(r ∈ Gᵢ)
- Result = A ∩ O₁ ∩ O₂ ∩ ... ∩ Oₙ

Nota: se non ci sono gruppi OR, il comportamento resta quello attuale.

## API input (v1)
Estendere l’endpoint:
`GET /api/availability/slots`

Nuovo parametro:
- `orGroups` (string) opzionale

Formato consigliato (v1, semplice e url-safe):
- gruppi separati da `|`
- risorse nel gruppo separate da `,`
Esempio:
- `orGroups=7,8|10,11,12`
= (7 OR 8) AND (10 OR 11 OR 12)

Compatibilità:
- `resourceIds` esistente resta AND obbligatorio (come oggi)
- `orGroups` è un “add-on”

Validazioni:
- max gruppi: 5
- max risorse per gruppo: 10
- totale risorse (resourceIds + orGroups): max 20
- no duplicati (normalizzare)

## Deliverables
- Core: supporto OR semantics v1 con test unitari chiari
- WebApi: parsing e validazioni `orGroups`, mapping su AvailabilityQuery v1
- Test WebApi: caso OR che produce risultati attesi
- (Se necessario) ADR che formalizzi la scelta input/semantica

## Vincoli
- Core non deve dipendere da Web/EF
- No refactor distruttivi: estensione incrementale
- Mantenere performance ragionevoli: union/intersection su slot normalizzati
