# 🌌 Azure Cosmos DB Bulk Operation CLI Tool

[![Build](https://github.com/SonnyRR/cosmos-bulk-operation-tool/actions/workflows/ci.yml/badge.svg)](https://github.com/SonnyRR/cosmos-bulk-operation-tool/actions/workflows/ci.yml)

Custom CLI tool, written in `C#`, for bulk updating/inserting & deleting Azure Cosmos Database records. It leverages the Azure Cosmos DB SDK, Bulk Executor context, Patch API and other performant configurations. It aims to serve as a showcase of best practices for working with the SDK for throughput performant workloads and as a starting point for any real-world bulk operation scenarios.

Motivation behind this project was the lack of a simple way of mutating a lot of records. Microsoft don't support
common create/update/delete operations with a SQL like syntax, so you're pretty much left with the Cosmos SDK, stored procedures (which have their own limitations and caveats) or the [Azure Cosmos DB Desktop Data Migration Tool](https://github.com/AzureCosmosDB/data-migration-desktop-tool) which can be extended with custom extensions.

The tool can be extended with custom strategies and utilized for real production scenarios.

> [!WARNING]
> When running the tool against an actual resource (PROD or NON-PROD) make sure
> that you run the `Release` build in order to utilize local query plan generation
> & all of the other performance optimizations by the SDK. Do not change the platform
> target away from `x64`. The SQL SDK includes a native ServiceInterop.dll to parse
> and optimize queries locally. ServiceInterop.dll is supported only on the Windows
> x64 platform.

## 🧩 Prerequisites

### 🌠 Emulator

In case you want to try this tool in an isolated environment you will need to spin up an instance of the `Azure Cosmos Emulator`.

> [!NOTE]
> When running the tool against an actual Azure Cosmos DB resource (PROD or NON-PROD), use the `Direct` connection mode for optimal performance.
> The legacy emulator supports Direct mode; the vnext-preview image supports only Gateway mode.

#### ✨ vnext-preview

The `vnext-preview` image starts up significantly faster and does not require SSL certificate imports.

> [!WARNING]
> This version only supports Gateway mode (see [feature support](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-linux#feature-support)).

> [!WARNING]
> The .NET SDK for Azure Cosmos DB doesn't support bulk execution in the emulator (see [limitations](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-linux#limitations)).

```shell
# Start a podman/docker container with the Azure Cosmos Emulator vnext-preview image.
podman run -p 37125:8081 -p 37126:1234 --name azcdb -d -e "AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true" mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview
```

Once running, you can connect using the following configuration:

```json
{
    "CosmosSettings": {
        "EndpointUrl": "http://localhost:37125/",
        "AuthorizationKey": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
        "ConnectionMode": "Gateway"
    }
}
```

Verify the emulator is running at https://localhost:37125/\_explorer/index.html.

#### 🗄️ latest (legacy)

The legacy emulator image is kept for historic reasons. It requires SSL certificate import and takes longer to start.

```shell
# Start a podman/docker container with the Azure Cosmos Emulator image.
podman run -p 4387:8081 -p 10250-10255:10250-10255 --name azcdb -d -e "AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true" mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest

# SSH into the machine and download the emulator's development certificate to the CA store.
podman machine ssh
sudo curl -k -o /etc/pki/ca-trust/source/anchors/azcdb-emulator.pem https://localhost:4387/_explorer/emulator.pem

# Add the certificates to the list of trusted CAs.
sudo update-ca-trust
```

💡 The certificate stores can differ in other linux distributions, the example above is for `RHEL` based distros.

After you've updated the list of trusted certificates on the podman machine, logout of the `ssh` section and download the same certificate to your own machine, in order to import it there as well. Otherwise, you won't be able to establish a successful connection from your host machine (i.e., running this CLI tool) to the podman container. See https://learn.microsoft.com/en-us/azure/cosmos-db/emulator?tabs=ssl-netstd21#import-emulator-certificate for more information.

```powershell
# Assuming the host machine runs Windows, download it and install it under the Trusted Root CA.
iwr https://localhost:4387/_explorer/emulator.pem -OutFile ~\Downloads\emulator.crt -SkipCertificateCheck
```

After you've spin up the container & trusted the development SSL cert, navigate to https://localhost:4387/\_explorer/index.html in order to verify that the emulator is up and running.

> ⚠️ The instructions above are for containerized instances. `Microsoft` also provides a native Windows installation, which you can find [here](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-release-notes).

Useful resources:

- <https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-develop-emulator?tabs=docker-linux%2Ccsharp&pivots=api-nosql>
- <https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-linux>
- <https://github.com/Azure/azure-cosmos-db-emulator-docker>

## 🔧 Configuration

The important configuration keys are part of the `appsettings.json` file (connection strings, container settings, logging)
The app also supports a couple of CLI parameters:

```powershell
.\Cosmos.BulkOperation.CLI.exe --help

USAGE:
    cosmos-bulk-operation-tool [OPTIONS]

EXAMPLES:
    cosmos-bulk-operation-tool --dry-run
    cosmos-bulk-operation-tool

OPTIONS:
    -h, --help        Prints help information
        --dry-run     Dry-run mode, allowing for changes to be scheduled, but not evaluated on the destination Cosmos database. Used for debugging
        --strategy    The name of the strategy to execute directly, bypassing the interactive prompt
```

## 🗒️ Logs

Logs are rolled on a daily basis by default to the current working directory with the filename pattern `bulk-operation-cli-tool-log-{date stamp}.txt`. You can configure the path in the `appsettings.json` file.

## 🔁 Retry policy

By default all Cosmos DB operations in this tool are wrapped around a fallback exponential retry policy. The default timespan intervals (in seconds) are: `2, 4, 8, 16 & 32`. Before kicking off those custom policies, the SDK is configured to automatically retry any throttled requests (by default 25 times).

## 🥏 Strategies

The application makes use of custom bulk update strategies in order to perform the data manipulation operations against a given container.
Currently, there are `4` base bulk operation strategies, that can be used for extension:

- `BaseBulkOperationStrategy.cs`
- `BulkDeleteOperationStrategy.cs`
- `BulkInsertOperationStrategy.cs`
- `BulkPatchOperationStrategy.cs`

Alongside with the base clasesses, I've included 3 sample strategies, that rely on fake data generated with `Bogus` and cover some common use case scenarios such as bulk insert, bulk update, bulk delete. These are the strategies that you can choose by default when you run the application.

- `SampleRecordsDeletionStrategy.cs`
- `SampleRecordsInsertionStrategy.cs`
- `SampleRecordsPatchStrategy.cs`

The strategies can be found under the `Cosmos.BulkOperation.CLI.Strategies` namespace.
