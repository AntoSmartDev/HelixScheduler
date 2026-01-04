# Scheduling Model

## Scheduling Rules
Regole strutturali:
- positive o negative
- ricorrenti o puntuali
- associate a N risorse

Sono poche e stabili.
### DaysOfWeekMask (regole ricorrenti settimanali)
Le regole weekly usano una bitmask per indicare i giorni attivi.
Mapping (coerente con System.DayOfWeek):
- 1 = Sunday
- 2 = Monday
- 4 = Tuesday
- 8 = Wednesday
- 16 = Thursday
- 32 = Friday
- 64 = Saturday

Esempi:
- 10 = 2 + 8 = Monday + Wednesday
- 18 = 2 + 16 = Monday + Thursday

Motivazione:
- compatto in storage (un int)
- filtri veloci (AND bitwise)
- semplice serializzazione/interop

## BusySlot
Eventi di dominio:
- sempre negativi
- multi-risorsa
- forniti già normalizzati
- temporanei
- consumano capacity (1 unita' per busy)

Il core li applica e li dimentica.

## Principio chiave
Una regola o evento multi-risorsa genera
occupazioni indipendenti per ogni risorsa.

