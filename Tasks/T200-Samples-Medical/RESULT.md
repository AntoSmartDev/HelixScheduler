# Result - T200 Samples.Medical

## Stato
DONE

## Sintesi
Pagina Razor minimale per interrogare l'API availability e mostrare gli slot
in una lista con gestione errori base.

## Attivita svolte
- Form con data e risorse (Doctor 7, Room 1)
- Chiamata HTTP a /api/availability/slots come client esterno
- Rendering lista slot e messaggi per errori/nessun risultato
- Link rapido alla chiamata API usata
- Pagina di istruzioni per avvio e verifica manuale
- Range di default per test 2026 + risorse aggiuntive (Doctor 8, Room 2)
- Selezione predefinita su Doctor 8 + Room 2 per demo 2026
- Pulsante "Use demo range" per impostare automaticamente i dati seed 2026
- Allineati gli ID risorse sample ai valori del seed corrente (Doctor 7=3, Room 1=2, Doctor 8=5, Room 2=4)
- Lista risorse caricata dinamicamente via endpoint API /api/resources
- Test WebApi per scenario 2026 (parametrizzati, single-resource, range 31gg, ordine resourceIds)

## File toccati
- HelixScheduler.Samples.Medical/Program.cs
- HelixScheduler.Samples.Medical/appsettings.json
- HelixScheduler.Samples.Medical/Pages/Index.cshtml
- HelixScheduler.Samples.Medical/Pages/Index.cshtml.cs
- HelixScheduler.Samples.Medical/Pages/Instructions.cshtml
- HelixScheduler.Samples.Medical/Pages/Instructions.cshtml.cs
- HelixScheduler.Samples.Medical/Pages/Shared/_Layout.cshtml
- HelixScheduler.Infrastructure/Persistence/Seed/SchedulerDbSeeder.cs
- HelixScheduler/Controllers/ResourcesController.cs

## Decisioni
- URL API configurabile via AvailabilityApi:BaseUrl
- Nessuna logica di dominio in UI; solo chiamata HTTP e rendering

## Rischi / Note
- Se l'API non e' disponibile, la pagina mostra errore di reachability
- L'uso di HTTP locale richiede che l'API giri su http://localhost:5063
- Seed 2026 visibili solo su DB con seed attivo in Development

## Come verificare
- Avviare HelixScheduler.WebApi
- Avviare HelixScheduler.Samples.Medical
- Selezionare data/risorse e confrontare output con chiamata manuale API
