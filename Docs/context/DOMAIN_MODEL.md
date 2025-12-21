# Domain Model (concettuale)

## Resource
Entità schedulabile o contenitore.
- Id stabile
- può avere più padri
- può avere proprietà

Esempi:
- Medico
- Stanza
- Sede

## Gerarchia
- DAG (multi-parent)
- nessun ciclo
- usata per filtrare e delimitare query

## ResourceProperty
- albero gerarchico
- filtro semantico
- includeDescendants supportato
- match SOLO per ID (no LIKE)
