# Agent Instructions

## Build & Test

```bash
dotnet restore
dotnet build
dotnet test --no-build --logger 'console;verbosity=detailed'
```

## Important Constraints

- **Platform**: Always target `x64`. The Cosmos DB SDK includes `ServiceInterop.dll` (Windows x64 only) for local query plan generation.
- **Release build**: Use `Release` configuration when running against productionCosmos DB for performance optimizations.
- **Connection mode**: Use `Direct` for Azure resources; `Gateway` for emulator (bulk execution not supported by emulator).

## CLI Usage

```bash
# Dry-run mode (debugging)
dotnet run --project Cosmos.BulkOperation.CLI/Cosmos.BulkOperation.CLI.csproj -- --dry-run

# Run with specific strategy
dotnet run --project Cosmos.BulkOperation.CLI/Cosmos.BulkOperation.CLI.csproj -- --strategy SampleRecordsInsertionStrategy
```

## Emulator for Testing

```bash
podman run -p 37125:8081 -p 37126:1234 --name azcdb -d \
  -e "AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true" \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview
```

The `vnext-preview` image starts faster and doesn't require SSL cert imports. Use Gateway connection mode.

## Adding New Strategies

1. Create a new class in `Cosmos.BulkOperation.CLI/Strategies/` implementing `IBulkOperationStrategy`
2. Add `[SettingsKey("YourConfigSection")]` attribute to the class
3. Add the corresponding config section in `appsettings.json`:

```json
{
  "YourConfigSection": {
    "ContainerName": "YourContainer",
    "Query": {
      "Value": "select c.id from c where c.type = 'something'"
    }
  }
}
```

Run with `--strategy YourStrategyName` to bypass the interactive prompt.

## Code Standards

Most standards are enforced via `.editorconfig` and Roslyn analyzers (Roslynator, Sonar). Build will fail if violated.

Notable repo-specific conventions:
- **Indentation**: 4 spaces for C#, 2 for JSON/XML/CSProj
- **File-scoped namespaces**: Use `namespace X.Y;` style
- **Interface naming**: Prefix with `I` (e.g., `IBulkOperationStrategy`)

## Architecture

- `Cosmos.BulkOperation.CLI`: Main CLI application using Spectre.Cli
- `Cosmos.BulkOperation.Samples`: Sample strategies with fake data (Bogus)
- `Cosmos.BulkOperation.IntegrationTests`: Requires emulator running
- Extensibility: Implement `IBulkOperationStrategy` in new classes under `Strategies/`
