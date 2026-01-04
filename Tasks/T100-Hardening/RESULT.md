# Result - T100 Hardening & Validation

## Stato
DONE

## Sintesi
Hardening mirato su input validation e determinismo dell'endpoint availability,
con test di integrazione WebApi dedicati.

## Attivita svolte
- Rafforzate validazioni su resourceIds/propertyIds (solo interi positivi)
- Reso deterministico l'ordine risorse in query via sorting
- Aggiunti test WebApi per ID non-positivi

## File toccati
- HelixScheduler/Controllers/AvailabilityController.cs
- HelixScheduler/Services/AvailabilityApplicationService.cs
- HelixScheduler.WebApi.Tests/AvailabilityControllerTests.cs

## Decisioni
- Rifiutare ID non-positivi per evitare input non valido e DoS logici
- Ordinare le risorse per garantire output deterministico

## Rischi / Edge cases
- Client che inviavano ID <= 0 ora ricevono 400
- Ordine resourceIds in output ora stabilizzato (ascending)

## Come verificare
- dotnet test
