# Roadmap

The plan for FluentFunctions, about 9–11 weeks part-time. Ticked items are done in this repository.

The template no longer depends on FluentAzure. Configuration uses the built-in options pattern (`ValidateDataAnnotations` + `ValidateOnStart`), which still fails fast on startup and doesn't tie releases to another package. Phase 0 is now only about conventions.

## Phase 0: Conventions (1 week)

- [x] Record the conventions the template enforces as ADRs: [docs/adr](adr).
  - [x] Functional core and thin adapters ([0001](adr/0001-functional-core-thin-adapters.md))
  - [x] Errors as values, mapped per transport ([0002](adr/0002-errors-as-values.md))
  - [x] Configuration: one validated options section per feature ([0003](adr/0003-validated-configuration.md))
  - [x] Identity-based connections only ([0004](adr/0004-identity-based-connections.md))
  - [x] Logging and telemetry ([0005](adr/0005-logging-and-telemetry.md))
  - [x] Resource naming ([0006](adr/0006-resource-naming.md))
  - [x] Two IaC flavours on AVM, kept at parity ([0007](adr/0007-iac-flavours.md))
- [x] Semantic versioning and a changelog for the template package ([CHANGELOG.md](../CHANGELOG.md)).

## Phase 1: Reference app (2–3 weeks)

- [x] .NET 10 isolated worker on Flex Consumption.
- [x] Records for messages, `Result<T>` and `Error`, pure handlers behind thin trigger adapters.
- [x] Typed, validated configuration that stops the host at startup.
- [x] User-assigned managed identity and identity-based connections. No connection strings.
- [x] OpenTelemetry to Application Insights with Entra ID auth.
- [x] HTTP, Service Bus and Timer starter triggers.
- [x] Unit tests for the handlers.
- [ ] Run it end to end in a sandbox subscription: HTTP 201/400, a Service Bus message completed and one dead-lettered, a timer run, and traces in Application Insights.
- [ ] Integration tests for the adapters (HTTP status mapping, Service Bus settlement) once the sandbox run has shaped them.

## Phase 2: Infrastructure (2 weeks)

- [x] Bicep with AVM: Function app (Flex Consumption), storage, Application Insights, Log Analytics, Key Vault, user-assigned identity, role assignments, and Service Bus when that trigger is chosen.
- [x] Terraform with AVM: the same resources and settings.
- [x] Parity checklist: [docs/iac-parity.md](iac-parity.md).
- [ ] Deploy both flavours to a sandbox and compare the resulting resources and app settings.
- [ ] Decide on network isolation (private endpoints, VNet integration) as an option for teams that need it.

## Phase 3: Pipelines (1 week)

- [x] Azure DevOps YAML: build, test, IaC validation (`what-if` / `terraform plan`) and deployment with environment approvals.
- [ ] Run the pipeline in a real Azure DevOps project against the sandbox.

## Phase 4: Templatise (1–2 weeks)

- [x] `dotnet new fluentfn` with `--trigger`, `--iac` and `--pipeline` options and conditional content.
- [x] Pack as `FluentFunctions.Templates`; CI publishes to the internal feed on a `v*` tag.
- [x] Template CI that generates every option combination, then builds, tests and validates the IaC.
- [ ] Set `NUGET_FEED_URL` and `NUGET_API_KEY` on the `release` environment and cut `v0.1.0`.

## Phase 5: Pilot and launch (2 weeks)

- [ ] Pilot with one product team. Track what they change in the generated code; that's the backlog.
- [x] Draft the "zero to deployed" guide: [docs/zero-to-deployed.md](zero-to-deployed.md). Finish it with the pilot team's timings.
- [ ] Measure time-to-first-deploy (template install to first successful prod deploy). Record it per team.

## Phase 6: Keep it alive (ongoing)

- [x] Renovate for NuGet packages, AVM Bicep modules and Terraform modules/providers, in this repo and in generated apps.
- [ ] Release cadence: monthly, or sooner for security fixes. Each release has a changelog entry saying what teams should copy across.
