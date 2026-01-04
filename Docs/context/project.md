# HelixScheduler � Project Overview

HelixScheduler � un motore di scheduling agnostico dal dominio che calcola
slot di disponibilit� su una o pi� risorse applicando:
- regole positive e negative
- eventi di dominio normalizzati
- gerarchie di risorse
- filtri per propriet�

Non � un calendario, non � un gestionale, non � un framework UI.

## Obiettivi
- core deterministico e testabile
- riutilizzabile in domini diversi
- performance prevedibili
- modello semplice ed estensibile
- API surface chiara e coerente (vedi docs/context/API_SURFACE.md)

## Non-obiettivi
- persistenza eventi di dominio
- logica di business applicativa
- pre-calcolo o cache aggressive
