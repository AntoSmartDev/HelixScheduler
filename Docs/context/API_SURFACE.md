# API Surface

## Principi
- API core semplici, coerenti, difficili da usare male.
- Semantica invariata rispetto al modello canonico.
- Guardrail cheap su input invalidi (no hot-path changes).
- Tutti gli input temporali sono in UTC.
- Semantica query descritta in docs/context/QUERY_MODEL.md.

## Changelog

### T420 - API Surface Review
- XML docs mirate su tipi pubblici (query/inputs/result/slot/range/rule).
- Guardrail in `AvailabilityQuery` per liste contenenti null.
- Nessun cambiamento semantico o di performance.
