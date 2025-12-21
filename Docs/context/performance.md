# Performance Model

Target:
- decine / centinaia di risorse
- range limitato (giorni/settimane)

Linee guida:
- evitare LINQ in hot-path
- evitare allocazioni inutili
- niente cache prematura
- pipeline misurabile e debug-friendly
