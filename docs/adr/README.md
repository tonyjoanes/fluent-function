# Architecture decision records

Each ADR records one convention the template enforces, why, and what it costs. To change a convention, add a new ADR that supersedes the old one, then change the template.

| ADR | Decision |
|---|---|
| [0001](0001-functional-core-thin-adapters.md) | Functional core, thin trigger adapters |
| [0002](0002-errors-as-values.md) | Expected failures are values, mapped per transport |
| [0003](0003-validated-configuration.md) | One validated options section per feature, checked at startup |
| [0004](0004-identity-based-connections.md) | Identity-based connections only |
| [0005](0005-logging-and-telemetry.md) | OpenTelemetry and structured, source-generated logging |
| [0006](0006-resource-naming.md) | Resource naming |
| [0007](0007-iac-flavours.md) | Bicep and Terraform on Azure Verified Modules, at parity |
