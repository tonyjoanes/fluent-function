# 0004: Identity-based connections only

**Status:** Accepted

## Decision

- Each app has one user-assigned managed identity per environment. It is created before the app, so role assignments exist when the app first starts.
- Every connection is identity-based: host storage (`AzureWebJobsStorage__accountName`), the Flex Consumption deployment container, Service Bus (`ServiceBusConnection__fullyQualifiedNamespace`), Key Vault, and Application Insights ingestion.
- Shared-key access is disabled on storage, local auth on Service Bus and Application Insights, and FTP/SCM basic auth on the Function app.
- The identity gets the narrowest built-in roles that work: Storage Blob Data Owner, Storage Queue Data Contributor and Storage Table Data Contributor (host storage needs all three), Azure Service Bus Data Receiver and Sender, Key Vault Secrets User, Monitoring Metrics Publisher.
- Locally, `DefaultAzureCredential` uses the developer's own sign-in. Host storage uses Azurite.

## Consequences

- Nothing to rotate or leak. The pipeline deploys with workload identity federation, so it holds no secrets either.
- The deploying principal needs rights to create role assignments (Owner, or Contributor plus Role Based Access Control Administrator).
- Role assignments can take a few minutes to apply after the first deployment; first-start errors in that window are expected.
