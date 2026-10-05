# 0007: Bicep and Terraform on Azure Verified Modules, at parity

**Status:** Accepted

## Context

Teams are split between Bicep and Terraform. Offering only one would push the other half to hand-roll infrastructure.

## Decision

- Offer both, selected with `--iac`. Both use [Azure Verified Modules](https://aka.ms/avm) pinned to exact versions, so behaviour is Microsoft-maintained and upgrades are explicit.
- Both deploy the same resources, settings and role assignments. [docs/iac-parity.md](../iac-parity.md) is the checklist; any infrastructure PR updates both flavours and the checklist.
- Both target an existing resource group per environment (the pipeline creates it), and take per-environment values from `main.<env>.bicepparam` or `<env>.tfvars`.
- Terraform state is in Azure Storage with Entra ID auth, one container per environment.
- Renovate raises AVM upgrades. Upgrade both flavours in the same release.

## Consequences

- Two implementations to maintain. The parity checklist and the template CI (which lints/validates both) keep the cost visible.
- AVM interfaces differ between Bicep and Terraform (e.g. Flex Consumption is `functionAppConfig` in Bicep and `function_app_uses_fc1` plus `fc1_*` in Terraform). Parity is about the deployed result, not identical code.
