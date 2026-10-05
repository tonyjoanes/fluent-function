# 0002: Expected failures are values, mapped per transport

**Status:** Accepted

## Context

Exceptions for validation failures turn every adapter into try/catch blocks, and lose the difference between "this input is wrong" and "a dependency was down".

## Decision

- Handlers return `Result<T>`, which holds a value or a list of `Error(Code, Message, Kind, Target)`.
- Validation collects every problem (`Result.Validate`) instead of stopping at the first.
- `ErrorKind` decides what the adapter does:

  | Kind | HTTP | Service Bus | Timer |
  |---|---|---|---|
  | `Validation` | 400, problem details with field errors | dead-letter | fail the run |
  | `NotFound` | 404 | dead-letter | fail the run |
  | `Conflict` | 409 | dead-letter | fail the run |
  | `Transient` | 503 | throw, so the message is redelivered | fail the run |

- `Code` is stable, dotted, lower-case (`order.lines.empty`) and safe to log. `Message` and `Target` may echo input, so they are returned to the caller but not logged.
- Exceptions remain for bugs and failures nobody planned for. The host logs and retries them as usual.

## Consequences

- Clients see every validation problem in one response.
- Dead-lettered messages carry the error code as the dead-letter reason, so the DLQ is searchable.
- A small `Result` type is maintained in each generated app. It's ~80 lines and deliberately not a package, so teams can change it.
