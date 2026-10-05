# FluentFunctions

An Azure Functions app on .NET 10 (isolated worker), hosted on the Flex Consumption plan. Generated from the `fluentfn` template.

## Layout

```text
src/FluentFunctions.Core/         Pure logic: message records, Result/Error, handlers. No Azure dependencies.
src/FluentFunctions.Functions/    Trigger adapters, configuration, DI and telemetry.
tests/FluentFunctions.Core.Tests/ Unit tests for the handlers.
//#if (UseBicep)
infra/bicep/                      Bicep (Azure Verified Modules), one .bicepparam per environment.
//#endif
//#if (UseTerraform)
infra/terraform/                  Terraform (Azure Verified Modules), one .tfvars per environment.
//#endif
//#if (UseAzdo)
pipelines/                        Azure DevOps: build, test, validate infra, deploy dev -> test -> prod.
//#endif
```

See [docs/architecture.md](docs/architecture.md) for how the pieces fit and why.

## Run it locally

You need the .NET 10 SDK, [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local) and [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) for host storage.

```bash
azurite --silent &
cd src/FluentFunctions.Functions
func start
```

//#if (UseHttp)
Place an order:

```bash
curl -X POST "http://localhost:7071/api/orders" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"cust-1","lines":[{"sku":"SKU-1","quantity":2,"unitPrice":9.99}]}'
```

An invalid order returns `400` with every problem listed by field.

//#endif
//#if (UseServiceBus)
The Service Bus trigger connects with your own Azure sign-in (`az login`), not a connection string. Set `ServiceBusConnection__fullyQualifiedNamespace` in `local.settings.json` to a dev namespace where you have the **Azure Service Bus Data Receiver** role.

//#endif
Configuration is validated when the host starts. If a setting is missing or out of range, `func start` stops and names the setting.

## Test

```bash
dotnet test
```

The handlers are pure functions, so the tests need no mocks, emulators or Azure resources.

## Configuration

| Setting | Purpose |
|---|---|
| `AZURE_CLIENT_ID` | Client id of the user-assigned managed identity. Set by the infrastructure. |
| `AzureWebJobsStorage__accountName` | Host storage, identity-based. Locally, `AzureWebJobsStorage=UseDevelopmentStorage=true`. |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Enables the OpenTelemetry exporter. Telemetry is sent with Entra ID auth. |
| `KeyVault__Uri` | Key Vault for any secrets you add. Reference them from app settings with `@Microsoft.KeyVault(...)`. |
//#if (UseServiceBus)
| `ServiceBusConnection__fullyQualifiedNamespace` | Service Bus namespace, identity-based. |
| `ServiceBus__QueueName` | Queue the `ProcessOrder` function listens on. |
//#endif
//#if (UseOrders)
| `Orders__MaxLines`, `Orders__MaxQuantityPerLine`, `Orders__MaxAge` | Order rules. Validated at startup. |
//#endif
//#if (UseTimer)
| `Cleanup__Schedule`, `Cleanup__RetentionDays`, `Cleanup__BatchSize` | Clean-up schedule (NCRONTAB, UTC) and window. Validated at startup. |
//#endif

There are no connection strings or keys. Every Azure dependency is reached with the managed identity, and shared-key or local auth is switched off where Azure allows it.
//#if (UseAzdo)

## Pipeline setup

One-off, per environment (`dev`, `test`, `prod`):

1. Create an Azure Resource Manager service connection named `sc-fnappname-<env>` using workload identity federation. Give it **Owner** on the subscription or target resource group, because the infrastructure creates role assignments. **Contributor** plus **Role Based Access Control Administrator** also works.
2. Create an environment named `fnappname-<env>` under **Pipelines > Environments**, and add approvals and checks to `test` and `prod`.
//#if (UseTerraform)
3. Create the Terraform state storage account and its `tfstate-<env>` containers. Set `tfStateResourceGroup` and `tfStateStorageAccount` in `pipelines/azure-pipelines.yml`, and grant each service connection **Storage Blob Data Contributor** on the account.
//#endif

Then create the pipeline from `pipelines/azure-pipelines.yml`. Pull requests build, test and validate the infrastructure (`what-if` or `terraform plan`). Merges to `main` deploy to dev, then test, then prod.
//#endif
