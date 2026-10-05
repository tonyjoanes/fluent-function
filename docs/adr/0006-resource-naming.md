# 0006: Resource naming

**Status:** Accepted

## Decision

Names follow the [Cloud Adoption Framework abbreviations](https://learn.microsoft.com/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations): `{abbreviation}-{app}-{environment}`, plus a 6-character hash of the resource group id for globally unique names.

| Resource | Pattern | Example |
|---|---|---|
| Resource group | `rg-{app}-{env}` | `rg-orders-dev` |
| Managed identity | `id-{app}-{env}` | `id-orders-dev` |
| Log Analytics | `log-{app}-{env}` | `log-orders-dev` |
| Application Insights | `appi-{app}-{env}` | `appi-orders-dev` |
| Flex Consumption plan | `asp-{app}-{env}` | `asp-orders-dev` |
| Function app | `func-{app}-{env}-{hash}` | `func-orders-dev-k3x9q2` |
| Storage account | `st{app}{env}{hash}` (max 24) | `stordersdevk3x9q2` |
| Key Vault | `kv-{app}-{env}-{hash}` (max 24) | `kv-orders-dev-k3x9q2` |
| Service Bus | `sbns-{app}-{env}-{hash}` | `sbns-orders-dev-k3x9q2` |
| Service connection (Azure DevOps) | `sc-{app}-{env}` | `sc-orders-dev` |
| Environment (Azure DevOps) | `{app}-{env}` | `orders-dev` |

- `{app}` is 2–12 lowercase letters or digits. The template derives it from `--name` and it can be changed in the IaC parameters and the pipeline's `appName` variable.
- `{env}` is `dev`, `test` or `prod`.
- Every resource is tagged `application`, `environment` and `managed-by`.

Bicep and Terraform compute the hash differently (`uniqueString` vs `sha1`), so the same app deployed with each gets different global names. Don't switch flavour on an existing environment.
