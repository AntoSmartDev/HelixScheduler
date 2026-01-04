# Query Model

Una query chiede:
"Dammi gli slot in cui TUTTE queste condizioni sono soddisfatte"

Componenti:
- periodo (UTC)
- risorse richieste
- filtri per proprietà
- opzioni future

Default:
- AND logico (intersection)

## OR semantics (v1)
La query puo' includere gruppi di risorse alternative (OR) tramite orGroups.

Formato input (API):
- orGroups=7,8|10,11,12`r

Semantica:
- required: tutte le risorse obbligatorie (AND)
- per ogni gruppo OR: almeno una risorsa disponibile
- risultato = AND(required) n OR(group1) n OR(group2) ...

