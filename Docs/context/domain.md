# Domain Model (concettuale)

## Risorse
Una "risorsa" è un'entità schedulabile (medico, stanza, macchinario, ecc.).

## Vincoli
- Necessarie: devono essere tutte libere nello stesso slot
- Alternative: una tra N (es. macchinario A o B)
- Derivate: risorsa implicita (es. stanza associata al medico)

## Eventi di dominio
Il dominio genera eventi e stati (prenotazioni, assenze, manutenzioni).
Questi eventi vengono trasformati in:
- intervalli occupati
- intervalli indisponibili
- finestre di disponibilità

## Query supportate (high level)
- Disponibilità per combinazione di risorse
- Disponibilità per tipo prestazione (mappa -> risorse ammesse/necessarie)
- Range temporali: giorno/settimana con step configurabile