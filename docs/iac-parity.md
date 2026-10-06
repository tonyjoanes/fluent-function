# IaC parity checklist

Bicep (`infra/bicep/main.bicep`) and Terraform (`infra/terraform/main.tf`) must deploy the same thing. Any PR that changes one flavour updates the other and this table in the same PR. Reviewers check every row that the PR touches.

| Item | Bicep | Terraform |
|---|---|---|
| **Inputs** | | |
| App name (2–12 lowercase alphanumerics), default from `--name` | `appName` | `app_name` |
| Environment `dev`/`test`/`prod` | `environmentName` | `environment_name` |
| Region, defaults to resource group's | `location` | `location` |
| Max instances (default 100, prod 200) | `maximumInstanceCount` | `maximum_instance_count` |
| Instance memory 512/2048/4096 (default 2048) | `instanceMemoryMB` | `instance_memory_mb` |
| Log retention (default 30, prod 90) | `logRetentionDays` | `log_retention_days` |
| Extra tags | `tags` | `tags` |
| **Resources (AVM)** | | |
| User-assigned identity `id-{app}-{env}` | `managed-identity/user-assigned-identity` | `avm-res-managedidentity-userassignedidentity` |
| Log Analytics `log-{app}-{env}` | `operational-insights/workspace` | `avm-res-operationalinsights-workspace` |
| App Insights `appi-{app}-{env}`, workspace-based, local auth off | `insights/component` | `avm-res-insights-component` |
| Storage `st{app}{env}{hash}`, LRS, shared key off, TLS 1.2, no public blobs, `app-package` container | `storage/storage-account` | `avm-res-storage-storageaccount` |
| Key Vault `kv-{app}-{env}-{hash}`, RBAC, standard SKU, soft delete 90d, purge protection in prod | `key-vault/vault` | `avm-res-keyvault-vault` |
| Service Bus `sbns-{app}-{env}-{hash}` Standard, local auth off, `orders` queue (max delivery 10, lock 1m, DLQ on expiry) — Service Bus trigger only | `service-bus/namespace` | `avm-res-servicebus-namespace` |
| Flex Consumption plan `asp-{app}-{env}` (FC1, Linux) | `web/serverfarm` | `avm-res-web-serverfarm` |
| Function app `func-{app}-{env}-{hash}`, HTTPS only, TLS 1.2, HTTP/2, FTP/SCM basic auth off | `web/site` | `avm-res-web-site` |
| Flex: runtime `dotnet-isolated` 10.0, deployment container with UAI auth | `functionAppConfig` | `function_app_uses_fc1`, `fc1_*`, `storage_*` |
| **Role assignments for the identity** | | |
| Storage Blob Data Owner, Queue Data Contributor, Table Data Contributor | ✔ | ✔ |
| Monitoring Metrics Publisher on App Insights | ✔ | ✔ |
| Key Vault Secrets User | ✔ | ✔ |
| Service Bus Data Receiver and Sender (Service Bus trigger only) | ✔ | ✔ |
| **App settings** | | |
| `AzureWebJobsStorage__accountName/__credential/__clientId` | ✔ | ✔ |
| `AZURE_CLIENT_ID` | ✔ | ✔ |
| `APPLICATIONINSIGHTS_CONNECTION_STRING`, `APPLICATIONINSIGHTS_AUTHENTICATION_STRING` | ✔ | ✔ |
| `KeyVault__Uri` | ✔ | ✔ |
| `ServiceBusConnection__*`, `ServiceBus__QueueName` (Service Bus only) | ✔ | ✔ |
| `Orders__*` (HTTP or Service Bus) | ✔ | ✔ |
| `Cleanup__*` (Timer) | ✔ | ✔ |
| **Outputs** | | |
| Function app name | `functionAppName` | `function_app_name` |
| Identity client id | `identityClientId` | `identity_client_id` |
| Key Vault URI | `keyVaultUri` | `key_vault_uri` |

## Known differences

- The global-name hash: Bicep uses `uniqueString(resourceGroup().id)`, Terraform `sha1(resource group id)`. Names differ between flavours; don't switch flavour on a live environment.
- Bicep also outputs `functionAppHostName`; the Terraform web-site module has no equivalent output.
- Terraform reads the tenant id from the signed-in client for Key Vault; Bicep takes it from the subscription implicitly.
