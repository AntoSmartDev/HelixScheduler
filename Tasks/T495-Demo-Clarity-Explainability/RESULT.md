# Result — T495 Demo Clarity & Explainability

## Stato
DONE

## Implementazione
- Query Summary: intent in linguaggio naturale (Explorer e Search).
- Mental model copy sotto “Availability (UTC)”: flusso rules → intersezione → busy.
- Slot ↔ Rules: tooltip “Contributing rules” e conteggio slot per rule selezionata.
- Explainability: testo tooltip più esplicito e ASCII-safe (MATCH/BLOCK).
- Explorer vs Search: micro-copy in header + box informativo.
- Properties UI: property definitions raggruppate per ResourceType (Explorer + Search).
- Properties copy: “Properties are defined per resource type.” e label “Doctor/Room properties”.
- DemoWeb hardening: rendering DOM safe (no innerHTML per dati API).
- Navigazione: menu esplicito con stato attivo, footer dedicato e reset demo ridotto.
- UI polish: font più leggibile e layout footer compatto.

## Screenshot / note UX
- Explorer: “Looking for availability where Doctor = Doctor 7 AND Room = Room 1 …”
- Search: “Looking for availability where Doctor = any AND Specialization = Cardiology …”
- Tooltip slot mostra tutte le rules che contribuiscono.
- Properties panel: gruppi per tipo (Doctor properties / Room properties).
- Footer: HelixScheduler Demo + reset inline + copyright.

## Come verificare
1) Apri Availability Explorer: verifica mental model + query summary.
2) Clicca una rule: evidenzia slot correlati e mostra “contributes to N slots”.
3) Clicca uno slot: mostra elenco “contributing rules” (multi-rule).
4) Attiva Explain: verifica che la spiegazione sia leggibile e coerente.
5) Apri Search Availability: verifica distinzione Explorer/Search e query summary narrativo.
6) Properties: verifica gruppi per tipo e chip/filtri invariati.
7) Navigation: tab attivo evidenziato e footer coerente.
8) Security: nessun testo API interpretato come HTML (tooltip e card safe).
