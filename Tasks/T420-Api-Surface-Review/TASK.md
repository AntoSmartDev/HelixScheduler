# Task T420 — API Surface Review (release-ready)

## Contesto globale (obbligatorio)
Seguire **AGENTS.md** in root come fonte di verità primaria.  
Seguire il protocollo operativo definito in **TASKS.md**.  
Leggere **RESULTS.md**.

Riferimenti canonici:
- Docs/context/PROJECT.md
- Docs/context/ARCHITECTURE.md
- Docs/context/DOMAIN_MODEL.md
- Docs/context/SCHEDULING_MODEL.md
- Docs/context/QUERY_MODEL.md
- Docs/context/PIPELINE.md
- Docs/context/PERFORMANCE.md
- Docs/context/SECURITY_MODEL.md
- Docs/context/GLOSSARY.md

---

## Contesto
Il core è stabile e misurato (T400/T405/T410).  
Prima di introdurre nuove feature (Capacity v2, Explainability v2, ecc.), conviene rendere
la **surface pubblica** più “product-ready”:
- chiara
- coerente
- difficile da usare male
- senza cambiare semantica né hot-path

Questa attività è un “polish tecnico” mirato alla qualità della libreria.

---

## Obiettivo
Revisionare e migliorare la **API surface pubblica** di HelixScheduler (Core + eventuali facades pubbliche),
mantenendo:
- comportamento invariato
- performance invariata (o trascurabile)
- semplicità del codice

---

## Vincoli (non negoziabili)
- Nessuna modifica semantica:
  - output availability invariato
  - OR semantics invariata
  - Capacity (v1) invariata
  - Explainability invariata
- Nessun refactor algoritmico o cambi hot-path
- Nessuna nuova dipendenza
- Nessuna nuova feature funzionale (solo UX dell’API / ergonomia)
- Eventuali breaking change **solo se strettamente necessari** e devono essere:
  - minimizzati
  - motivati in RESULT
  - preferibilmente evitati tramite overload / obsolescence

---

## Approccio (obbligatorio)
1) Mappare API pubbliche (Core):
   - entry point principali
   - modelli input/output esposti
   - naming, nullability, overload, default
2) Identificare “foot-gun” tipici:
   - parametri ambigui (from/to, inclusivo/esclusivo)
   - duplicazioni o overload confusi
   - tipi troppo generici o poco espressivi
   - mancanza di guardrail (es. range invertito) dove opportuno
3) Applicare interventi low-risk:
   - rinomina locale (se interna) o alias pubblici (se necessario)
   - overload più chiari con default espliciti
   - aggiunta di XML docs minime per API pubbliche
   - piccoli helper/record per input (solo se già coerente col modello)
4) Verificare:
   - build/test
   - benchmark invariati (run singola ok)

---

## Checklist interventi consentiti (preferenza)
- Migliorare nomi di metodi/parametri (evitando breaking dove possibile)
- Rendere espliciti i default (es. opzioni query)
- Ridurre overload ridondanti o confusi
- Aggiungere XML docs mirate (non prolisse)
- Validazioni di input “cheap” (solo se non impattano hot-path; preferibilmente guardrail in entry point)

---

## Deliverables
- Patch minima e leggibile sull’API surface
- `RESULT.md` con:
  - elenco delle modifiche API (con rationale)
  - eventuali breaking (se presenti) e mitigazioni
  - impatto su performance (benchmark invariati)
  - file toccati

## Note
Se emergono cambi che impattano il modello canonico, fermarsi e proporre ADR.
Se non si trovano miglioramenti utili senza rischio breaking/complessità, chiudere task con esito:
“API già sufficientemente pulita per release v1”.
