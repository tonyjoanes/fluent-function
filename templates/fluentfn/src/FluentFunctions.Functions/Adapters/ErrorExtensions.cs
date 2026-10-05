using FluentFunctions.Core.Results;

namespace FluentFunctions.Functions.Adapters;

internal static class ErrorExtensions
{
    /// <summary>Error codes are safe to log; messages and targets may echo input, so they are not logged.</summary>
    public static string Codes(this IEnumerable<Error> errors) => string.Join(", ", errors.Select(e => e.Code));
}
