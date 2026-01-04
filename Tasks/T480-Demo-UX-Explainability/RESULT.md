# Result - T480 Demo UX Explainability

## Stato
DONE

## Piano sintetico
- Aggiunta micro-copy e pannello selezione per spiegare il modello rules → slot → busy.
- UX interattiva: tooltip explain sugli slot, highlight su click rule/busy, tag risorse.
- Nessun impatto su Core/API; solo rendering DemoWeb.

## Modifiche UX
- TODO: UI select per properties richiede endpoint API read-only per elenco proprieta' e associazioni (fuori scope T480).
- Filtro Properties: input Property IDs + chips con include descendants.
- Tooltip sugli slot (attivo con Explain availability) con rules compatibili e busy overlapping.
- Click su rule: evidenzia slot del calendario compatibili, offusca le altre regole.
- Click su busy: evidenzia slot del calendario impattati.
- Tag UI per risorse (Doctor/Room/Other) al posto del testo lineare.
- Micro-copy esplicativa sotto Availability e Busy Calendar.

## File toccati
- `HelixScheduler.DemoWeb/wwwroot/index.html`
- `HelixScheduler.DemoWeb/wwwroot/styles.css`
- `HelixScheduler.DemoWeb/wwwroot/app.js`

## Impatto sul modello
- Nessuno. Nessuna modifica a Core/Application/Infrastructure/WebApi.

## Rischi / edge case
- Tooltip e highlight basati su matching client-side (best-effort) usando i dati API.
- Regole non weekly/range vengono mostrate senza logica specifica avanzata.

## Come verificare
- Avviare WebApi + DemoWeb.
- Attivare Explain availability e fare hover su slot.
- Click su regole e busy slot per highlight.


