# HelixScheduler — Agent Instructions (AGENTS.md)

## 0. Obiettivo
Questo repository contiene **HelixScheduler**, un motore di scheduling ottimizzato per scenari con decine/centinaia di risorse e range di giorni/settimane.
Obiettivi primari:
- correttezza delle disponibilità e vincoli (risorse, regole, conflitti)
- performance (CPU/RAM) e prevedibilità dei costi
- separazione netta tra **motore di calcolo** e **eventi/regole di dominio**
- codebase pulita, modulare, testabile

## 1. Contesto canonico (obbligatorio)
Prima di qualunque modifica strutturale o decisione architetturale, considera questi documenti come fonte di verità:
- docs/context/PROJECT.md
- docs/context/ARCHITECTURE.md
- docs/context/DOMAIN.md
- docs/context/PERFORMANCE.md
- docs/context/SECURITY_MODEL.md
- docs/context/GLOSSARY.md

Se un task tocca uno degli argomenti sopra, **rileggi** i documenti pertinenti.

## 2. Regole non negoziabili
- Non introdurre dipendenze pesanti senza motivazione e impatto misurabile.
- Evita allocazioni inutili, LINQ in hot-path, enumerazioni multiple: preferisci approcci efficienti.
- Mantieni separazione tra:
  - Scheduler Core (algoritmi, calcolo slot, risoluzione conflitti)
  - Domain Events/Adapters (trasformazione eventi di dominio in input normalizzati per lo scheduler)
- Non cambiare API pubbliche senza motivazione e senza aggiornare documentazione + changelog.
- Ogni modifica significativa deve includere test (unit o integration) e aggiornamento docs se necessario.

## 3. Output atteso per ogni task
Quando proponi cambiamenti, segui questo formato:
1) **Piano breve** (2–6 punti)
2) **Modifiche** (file toccati + rationale)
3) **Rischi/edge cases**
4) **Come verificare** (comandi/test, esempi)
5) Se il task è “grande”: crea una nota in docs/adr/ o un log in docs/codex/

## 4. Convenzioni di lavoro
- Lavorare su branch dedicato (es. feature/*, refactor/*, fix/*).
- Preferire PR piccole e revisionabili.
- Se un task richiede una scelta architetturale, creare ADR:
  - docs/adr/ADR-XXXX-<titolo>.md (vedi docs/adr/ADR-TEMPLATE.md)

## 5. Standard qualità
- Test: aggiungere o aggiornare test quando si cambia comportamento.
- Naming: chiaro, consistente, coerente con il dominio.
- Logging: solo dove utile e senza inquinare hot-path.
- Sicurezza: non loggare dati sensibili, validare input, evitare DoS accidentali.

## 6. Incertezza
Se manca un’informazione essenziale nel contesto canonico, non inventare:
- fai la migliore assunzione ragionevole
- rendila esplicita nel piano
- aggiungi un TODO nei documenti (o un ADR)
- con “da confermare”

