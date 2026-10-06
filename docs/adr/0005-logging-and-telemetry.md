# 0005: OpenTelemetry and structured, source-generated logging

**Status:** Accepted

## Decision

- `host.json` sets `telemetryMode: OpenTelemetry`, and the worker calls `UseFunctionsWorkerDefaults()`, so host and worker spans join into one trace.
- The Azure Monitor exporter is added when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set, and authenticates with the managed identity. Application Insights has local auth disabled.
- Logs use `[LoggerMessage]` source-generated methods kept in `Adapters/Log.cs`, with named, structured fields.
- Log ids, error codes and counts. Do not log message bodies, request bodies, secrets or personal data. Error `Message`s are not logged because they can echo input.
- Default levels: `Information` for the app, `Warning` for `Azure.Core`.

## Consequences

- One telemetry pipeline locally and in Azure; teams can add an OTLP exporter without touching the functions.
- Adding a log line means adding a method to `Log.cs`. That small friction is deliberate.
