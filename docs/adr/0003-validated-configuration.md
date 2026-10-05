# 0003: One validated options section per feature, checked at startup

**Status:** Accepted

## Context

A missing or malformed app setting usually shows up as a `null` or a parse error inside the first invocation that needs it, often hours after a deployment "succeeded". We considered building on FluentAzure for this, but decided the built-in options pattern covers what the template needs without another dependency to version.

## Decision

- Each feature has an options class bound to one section (`Orders`, `ServiceBus`, `Cleanup`), so app settings read `Orders__MaxLines`, `Cleanup__Schedule`, and so on.
- Properties carry data annotations (`[Required]`, `[Range]`) and sensible defaults where one exists.
- Registration is always `services.AddValidatedOptions<T>(section)`, which binds, validates annotations and calls `ValidateOnStart()`. An invalid setting stops the host, naming the setting.
- Adapters inject `IOptionsMonitor<T>` and pass plain values (e.g. `OrderRules`) to handlers. Handlers never see `IOptions`.
- Secrets live in Key Vault and reach the app as Key Vault references (`@Microsoft.KeyVault(...)`), resolved by the platform with the managed identity. They are never in source, pipeline variables or app settings as plain text.

## Consequences

- A bad deployment fails at startup, which the pipeline can see, instead of at first use.
- Trigger binding expressions (`%Cleanup:Schedule%`) and options read the same settings.
