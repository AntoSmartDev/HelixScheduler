# AGENTS.md — HelixScheduler

## Ruolo dell’agente
Stai lavorando su **HelixScheduler**, un motore di scheduling **agnostico dal dominio**.
Il tuo compito è implementare un core deterministico, testabile e performante,
aderendo rigorosamente al modello concettuale documentato.

NON devi:
- introdurre logica di dominio applicativo
- interpretare casi d’uso specifici (medico, prenotazioni, ecc.)
- costruire UI, calendari o flussi applicativi

DEVI:
- rispettare il modello concettuale
- mantenere separazione netta motore ↔ dominio
- privilegiare chiarezza e prevedibilità

---

## Documenti canonici (OBBLIGATORI)
Prima di qualsiasi modifica, considera **fonte di verità**:

- docs/context/PROJECT.md
- docs/context/ARCHITECTURE.md
- docs/context/DOMAIN_MODEL.md
- docs/context/SCHEDULING_MODEL.md
- docs/context/QUERY_MODEL.md
- docs/context/PIPELINE.md
- docs/context/PERFORMANCE.md
- docs/context/SECURITY_MODEL.md
- docs/context/GLOSSARY.md

Se una modifica è in conflitto → fermati e proponi ADR.

---

## Regole non negoziabili

- Tutto il core lavora **solo in UTC**
- Nessuna dipendenza da:
  - EF / DB
  - HTTP / Web
  - dominio applicativo
- Regole scheduler: poche, strutturali
- Eventi di dominio: molti, sempre normalizzati come BusySlot
- Disponibilità calcolata:
  1. per singola risorsa
  2. poi intersezione

Un evento multi-risorsa NON è mai monolitico.

---

## Semantica fondamentale

Per ogni risorsa R:

Disponibilità(R) =
  regole positive(R)
  − regole negative(R)
  − BusySlot(R)

Solo dopo si applica l’intersezione tra risorse richieste.

---

## Performance
- target: decine/centinaia risorse
- range: giorni/settimane (max ~3 mesi)
- evitare algoritmi “furbi”
- no cache / no pre-calcolo globale
- pipeline leggibile, step-by-step

---

## Output richiesto per ogni task
1. Piano sintetico
2. File toccati
3. Impatto sul modello
4. Rischi / edge case
5. Come verificare

Task strutturali → ADR o log in docs/codex/
