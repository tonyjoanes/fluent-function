# Changelog

All notable changes to the `FluentFunctions.Templates` package. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/):

- **Major**: a change that needs teams to edit generated code to upgrade, or that removes a template option.
- **Minor**: a new option, trigger or capability.
- **Patch**: dependency bumps and fixes.

## [Unreleased]

### Added

- `fluentfn` template: .NET 10 isolated worker on Flex Consumption with a pure core library, `Result`/`Error` types and thin trigger adapters.
- `--trigger http|servicebus|timer` (repeatable), `--iac bicep|terraform|none` and `--pipeline azdo|none` options.
- Startup-validated options per feature, user-assigned managed identity with identity-based connections, and OpenTelemetry to Application Insights with Entra ID auth.
- Bicep and Terraform infrastructure on Azure Verified Modules, with a parity checklist.
- Azure DevOps pipeline: build, test, `what-if` / `terraform plan`, and gated deployment to dev, test and prod.
- Template CI that generates every option combination and builds, tests and validates each one.
