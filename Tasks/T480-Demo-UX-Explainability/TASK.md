# Task T480 — Demo UX Explainability (read-only, no core impact)

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md**.

Riferimenti canonici:
- Docs/context/PROJECT.md
- Docs/context/ARCHITECTURE.md
- Docs/context/DOMAIN_MODEL.md
- Docs/context/SCHEDULING_MODEL.md
- Docs/context/QUERY_MODEL.md
- Docs/context/QUERY_MODEL.md
- Docs/context/PIPELINE.md
- Docs/context/PERFORMANCE.md
- Docs/context/SECURITY_MODEL.md
- Docs/context/GLOSSARY.md

---

## Contesto
La DemoWeb mostra correttamente:
- disponibilità settimanali
- busy slots
- rules applicate
- filtri risorse

Il valore del motore è presente ma non ancora **immediatamente spiegabile visivamente**.
Questo task migliora la UX per rendere chiaro il *perché* degli slot,
senza introdurre nuove funzionalità né modificare il core.

---

## Obiettivi
1) Migliorare la **comprensione causa–effetto** (rule → slot → busy)
2) Rendere evidente il modello AND / OR / subtraction
3) Usare esclusivamente dati già forniti dall’API (explainability v1)
4) Nessun impatto su Core, Application o Infrastructure

---

## Vincoli (non negoziabili)
- Nessuna modifica a HelixScheduler.Core
- Nessuna modifica semantica API
- Nessuna nuova endpoint WebApi
- UI read-only
- Nessuna introduzione di CRUD o planner/optimizer
- Performance invariata (solo rendering UI)

---

## Scope UX (richiesto)

### A) Explain tooltip sugli slot (PRIORITÀ ALTA)
Quando l’opzione “Explain availability” è attiva:

- Hover su slot disponibile mostra:
  - elenco regole che lo generano
  - busy slots che lo riducono/spezzano (se presenti)
  - motivi sintetici (testuali, non tecnici)

Esempio:
> ✅ Slot disponibile  
> ✔ Rule #1 – Doctor 7 + Room 1  
> ❌ Rule #3 – Room only (resource mismatch)  
> ✔ No busy conflicts

---

### B) Relazione Rule → Slot
- Click su una rule nella lista:
  - evidenzia visivamente solo gli slot generati da quella rule
  - offusca le altre rules
  - mantiene calendario invariato (no ricalcolo)

---

### C) Relazione Busy → Slot
- Click su un busy slot:
  - evidenzia gli slot impattati (spezzati o rimossi)
  - mostra breve descrizione “This busy interval blocks X slots”

---

### D) Tag visivi per risorse
Sostituire testo “Resources: …” con tag UI:

- Doctor → tag blu
- Room → tag grigio
- (eventuali altri tipi → colore distinto)

Esempio:
[ Doctor 7 ] [ Room 1 ]


---

### E) Micro-copy esplicativa
Aggiungere micro-testi non invasivi:

- Sotto “Availability (UTC)”:
  > Slots computed from rules minus busy intervals

- Sotto “Busy Calendar”:
  > Busy intervals applied after rule expansion

---

## Fuori scope (esplicito)
- CRUD
- Drag & drop
- Editing regole
- Ottimizzazione / auto-assignment
- Nuove API
- Cambiamenti seed

---

## Deliverables
- Miglioramenti UX in `HelixScheduler.DemoWeb`
- Nessun file modificato in Core / Application / Infrastructure
- RESULT.md aggiornato con:
  - elenco miglioramenti UX
  - screenshot descrittivo (se applicabile)
  - conferma “no core impact”
