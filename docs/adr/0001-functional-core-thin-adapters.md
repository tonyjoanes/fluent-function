# 0001: Functional core, thin trigger adapters

**Status:** Accepted

## Context

Function apps tend to mix trigger plumbing, Azure SDK calls and business rules in one method. That makes the rules hard to test, because a test needs a Functions host, emulators or mocks.

## Decision

- Business logic lives in `*.Core` as static, pure handler functions. A handler receives everything it needs as arguments (the input, the current time, generated ids, rules taken from configuration) and returns `Result<T>`.
- `*.Core` has no package references: no Azure SDK, no Functions SDK, no logging.
- Each trigger has an adapter in `*.Functions` that deserialises the input, reads options and the clock, calls the handler, and maps the result to its transport. Adapters hold no business rules.
- Messages are immutable `record`s without transport attributes.

## Consequences

- Handlers are unit tested with plain values. No mocks.
- I/O that a handler depends on (e.g. "does this customer exist?") is done by the adapter first and passed in, or the handler returns a decision that the adapter acts on (see `CleanupPlan`). A handler that needs several dependent round-trips is the signal to introduce a small interface in Core, implemented in Functions.
- Some duplication between adapters. It's accepted to keep them obvious.
