# FluentFunctions

A `dotnet new` template for production-ready Azure Functions apps. One command gives a team a .NET 10 isolated Function app on Flex Consumption with a functional core, validated configuration, managed identity, OpenTelemetry, infrastructure as code and a deployment pipeline. Every option combination is built, tested and validated in CI.

```bash
dotnet new install FluentFunctions.Templates
dotnet new fluentfn --name Contoso.Orders --iac bicep --trigger servicebus
```

## What you get

| Area | Choice |
|---|---|
| Runtime | .NET 10 isolated worker, Flex Consumption plan |
| Code style | Records for messages, `Result<T>`/`Error` for expected failures, pure handlers behind thin trigger adapters |
| Configuration | One options class per feature, data-annotation validated, checked at startup (`ValidateOnStart`) |
| Security | One user-assigned managed identity; identity-based connections; shared keys and local auth disabled |
| Telemetry | OpenTelemetry from host and worker to Application Insights, with Entra ID auth |
| Triggers | HTTP, Service Bus (complete/dead-letter/retry by error kind), Timer |
| Infrastructure | Bicep or Terraform, both on Azure Verified Modules |
| Pipeline | Azure DevOps: build, test, `what-if` / `terraform plan`, gated deploy to dev, test and prod |

## Options

| Option | Values | Default |
|---|---|---|
| `--trigger` | `http`, `servicebus`, `timer`. Repeat it to pick several: `--trigger http --trigger timer`. | `http` |
| `--iac` | `bicep`, `terraform`, `none` | `bicep` |
| `--pipeline` | `azdo`, `none` | `azdo` |

Generated apps contain only what was chosen: no dead triggers, settings, infrastructure or pipeline steps.

## Repository layout

```text
templates/fluentfn/          The template. Also a working reference app (see "Working on the template").
FluentFunctions.Templates.csproj  Packs templates/ into the NuGet template package.
scripts/test-template.sh     Generates every option combination, then builds, tests and validates each.
.github/workflows/           Template CI: combinations, pack, publish to the internal feed on a v* tag.
docs/                        Roadmap, ADRs, IaC parity checklist, "zero to deployed" guide.
```

## Working on the template

The content under `templates/fluentfn` uses template conditionals (`#if (UseServiceBus)` and friends), so change it, then check the output rather than the source:

```bash
scripts/test-template.sh              # every combination (needs .NET 10; bicep and terraform for the IaC checks)
scripts/test-template.sh servicebus   # only combinations whose name contains "servicebus"
```

Output lands in `artifacts/template-tests/<combination>`, which you can open and run.

Conventions for the generated code are recorded as ADRs in [docs/adr](docs/adr). Changes to the infrastructure must keep Bicep and Terraform in step; see [docs/iac-parity.md](docs/iac-parity.md).

## Releasing

1. Update [CHANGELOG.md](CHANGELOG.md).
2. Tag `vX.Y.Z` and push the tag. CI tests every combination, packs, smoke-tests the package, and pushes it to the internal feed configured on the `release` environment (`NUGET_FEED_URL` variable, `NUGET_API_KEY` secret).

Teams upgrade with `dotnet new install FluentFunctions.Templates::X.Y.Z`. Existing apps are not changed; the changelog says what to copy across.

## Status

See [docs/roadmap.md](docs/roadmap.md).
