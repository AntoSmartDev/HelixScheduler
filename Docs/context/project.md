# HelixScheduler — Project Overview

HelixScheduler è un motore di scheduling agnostico dal dominio che calcola
slot di disponibilità su una o più risorse applicando:
- regole positive e negative
- eventi di dominio normalizzati
- gerarchie di risorse
- filtri per proprietà

Non è un calendario, non è un gestionale, non è un framework UI.

## Obiettivi
- core deterministico e testabile
- riutilizzabile in domini diversi
- performance prevedibili
- modello semplice ed estensibile

## Non-obiettivi
- persistenza eventi di dominio
- logica di business applicativa
- pre-calcolo o cache aggressive
