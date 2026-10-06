using System.Text.Json;
using FluentFunctions.Core.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FluentFunctions.Functions.Http;

internal static class HttpResults
{
    /// <summary>Reads a JSON body, turning a missing or malformed body into a validation error.</summary>
    public static async Task<Result<T>> ReadBodyAsync<T>(this HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var body = await request.ReadFromJsonAsync<T>(cancellationToken);
            return body is null
                ? Error.Validation("request.body.missing", "A JSON request body is required.")
                : Result.Success(body);
        }
        catch (JsonException)
        {
            return Error.Validation("request.body.invalid", "The request body is not valid JSON for this operation.");
        }
    }

    /// <summary>
    /// Maps handler errors to RFC 9457 problem details. Validation errors are grouped by field so
    /// clients can show every problem at once.
    /// </summary>
    public static IActionResult ToProblem(this IReadOnlyList<Error> errors)
    {
        var status = errors[0].Kind switch
        {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status503ServiceUnavailable,
        };

        if (status != StatusCodes.Status400BadRequest)
        {
            return new ObjectResult(new ProblemDetails { Status = status, Title = errors[0].Message, Type = errors[0].Code })
            {
                StatusCode = status,
            };
        }

        var fieldErrors = errors
            .GroupBy(e => e.Target ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());

        return new BadRequestObjectResult(
            new ValidationProblemDetails(fieldErrors)
            {
                Status = status,
                Title = "The request is invalid.",
                Extensions = { ["codes"] = errors.Select(e => e.Code).ToArray() },
            }
        );
    }
}
