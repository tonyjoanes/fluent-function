# Architecture

## Functional core, thin adapters

```text
 trigger (HTTP / Service Bus / Timer)
        │  deserialise, read config and clock
        ▼
 adapter in FluentFunctions.Functions ──► handler in FluentFunctions.Core ──► Result<T>
        ▲                                                         │
        └──────────── map Result to the transport ◄───────────────┘
              HTTP status · complete / dead-letter / retry · log
```

- **Handlers** are static, pure functions. They take everything they need as arguments (input, clock value, generated ids, rules from configuration) and return `Result<T>`. They don't log, read configuration or call Azure.
- **Adapters** are the only place with I/O. Each one deserialises the input, calls a handler and translates the result for its transport. They contain no business rules.
- **Messages** are immutable `record`s with no transport attributes, so the same types travel over HTTP and Service Bus.

The core project has no package references. If a change needs one there, it probably belongs in an adapter.

## Errors

Handlers return expected failures as `Error(Code, Message, Kind, Target)`. Exceptions are left for bugs and for failures nobody planned for.

| `ErrorKind` | HTTP | Service Bus | Timer |
|---|---|---|---|
| `Validation` | 400 with every field error | dead-letter (retrying won't help) | fail the run |
| `NotFound` | 404 | dead-letter | fail the run |
| `Conflict` | 409 | dead-letter | fail the run |
| `Transient` | 503 | throw, so the message is redelivered | fail the run (retried next schedule) |

Error `Code`s are stable and safe to log. `Message` and `Target` may echo input, so they go back to the caller but are not logged.

## Configuration

Each feature has an options class bound to one section (`Orders`, `ServiceBus`, `Cleanup`), validated with data annotations, and checked when the host starts (`ValidateOnStart`). A bad setting stops the app at deployment time instead of failing an invocation hours later. Adapters read `IOptionsMonitor<T>.CurrentValue` and pass plain values to the handlers.

## Identity

One user-assigned managed identity per app per environment. It is used for host storage, the Flex Consumption deployment container, Service Bus, Key Vault and Application Insights ingestion. Shared keys and local auth are disabled. Locally, `DefaultAzureCredential` uses your own sign-in.

## Telemetry

The host and the worker emit OpenTelemetry (`telemetryMode: OpenTelemetry` in `host.json`, `UseFunctionsWorkerDefaults()` in the worker). Traces, metrics and logs go to Application Insights through the Azure Monitor exporter, authenticated with the managed identity. Logs use source-generated `[LoggerMessage]` methods in `Adapters/Log.cs` with structured fields only: no payloads, secrets or personal data.
