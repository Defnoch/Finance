# Finance.BackgroundTasks

Dit project bevat generieke background/batch-processen voor Finance.

## Functionaliteit
- Uitvoer van verschillende taken zoals categorisatie, rapportage, cleanup, etc.
- Taken zijn los te implementeren en te registreren.
- Uitvoer via command line argumenten, config, of scheduler.

## Uitvoering
- `dotnet run --task categorization` voor categorisatie.
- Uitbreidbaar met nieuwe taken.

## Verantwoordelijkheden
- Geen API endpoints.
- Alleen batchverwerking en logging.

## Gebruik van de gegenereerde .NET API client
- De .NET API client wordt gegenereerd via het script in `../Finance.ApiClientGenerator/generate-dotnet-client.sh`.
- Gebruik altijd de gegenereerde client voor API calls in dit project.
- **Pas de client nooit handmatig aan!**
- Her-genereer de client bij elke API-wijziging.

## Voorbeeld gebruik
```csharp
using Finance.GeneratedApiClient;

var apiClient = new FinanceApiClient("http://localhost:5000/");
await apiClient.BatchAssignAsync(commands);
```
