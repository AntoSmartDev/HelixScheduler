# Performance & Cost Model

## Scenario target
- decine/centinaia di risorse
- range giorni/settimane
- hot-path: calcolo slot per query utente

## Regole pratiche
- evitare LINQ in percorsi caldi se crea allocazioni o enumerazioni multiple
- preferire strutture dati efficienti (liste ordinate di intervalli, sweep-line, ecc.)
- ridurre oggetti temporanei: valutare pooling solo se misurato
- ogni ottimizzazione deve essere accompagnata da benchmark o almeno test prestazionali ripetibili

## Definition of Done (perf)
- Non peggiorare memoria e tempo in modo evidente
- Se introduce complessità, documentare tradeoff (ADR)