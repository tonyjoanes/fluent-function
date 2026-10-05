namespace FluentFunctions.Core.Results;

/// <summary>
/// What went wrong, in terms the trigger adapters can act on. Handlers choose the kind;
/// adapters decide what it means for their transport (HTTP status, dead-letter, retry).
/// </summary>
public enum ErrorKind
{
    /// <summary>The input breaks a business rule. Retrying the same input will fail again.</summary>
    Validation,

    /// <summary>Something the input refers to does not exist.</summary>
    NotFound,

    /// <summary>The input clashes with the current state, e.g. a duplicate.</summary>
    Conflict,

    /// <summary>A dependency failed in a way that may succeed on retry.</summary>
    Transient,
}

/// <summary>
/// A failure that a handler expected and can describe. Unexpected failures stay as exceptions.
/// </summary>
/// <param name="Code">Stable, machine-readable code, e.g. <c>order.lines.empty</c>. Safe to log and return.</param>
/// <param name="Message">Human-readable description. Must never contain secrets or personal data.</param>
/// <param name="Kind">How adapters should treat the failure.</param>
/// <param name="Target">The input field the error relates to, if any.</param>
public sealed record Error(string Code, string Message, ErrorKind Kind, string? Target = null)
{
    public static Error Validation(string code, string message, string? target = null) =>
        new(code, message, ErrorKind.Validation, target);

    public static Error NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorKind.Conflict);

    public static Error Transient(string code, string message) => new(code, message, ErrorKind.Transient);
}
