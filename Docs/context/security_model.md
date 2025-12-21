# Security Model

## Threats principali
- Input malevolo che causa carico eccessivo (DoS logico)
- Range temporali enormi (richieste non limitate)
- Set risorse enormi (combinazioni esplosive)

## Mitigazioni
- Validazione input: range max, step minimo, max risorse per query
- Timeout/limiti sulle operazioni intensive (se applicabile nell'host)
- Non loggare dati sensibili o payload completi
- Testare behavior con input limite

## OSS
- SECURITY.md definisce disclosure e gestione vulnerabilità