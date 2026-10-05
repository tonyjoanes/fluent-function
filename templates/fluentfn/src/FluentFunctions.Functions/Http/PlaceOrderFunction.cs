using FluentFunctions.Core.Orders;
using FluentFunctions.Functions.Adapters;
using FluentFunctions.Functions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentFunctions.Functions.Http;

/// <summary>
/// Thin adapter: read the request, call the pure handler, map the result to HTTP. No business rules here.
/// </summary>
public sealed class PlaceOrderFunction(
    IOptionsMonitor<OrdersOptions> options,
    TimeProvider clock,
    ILogger<PlaceOrderFunction> logger
)
{
    [Function("PlaceOrder")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequest request,
        CancellationToken cancellationToken
    )
    {
        var body = await request.ReadBodyAsync<PlaceOrder>(cancellationToken);
        var result = body.Bind(order =>
            OrderHandlers.Place(order, Guid.NewGuid(), clock.GetUtcNow(), options.CurrentValue.ToRules())
        );

        return result.Match<IActionResult>(
            accepted =>
            {
                Log.OrderAccepted(logger, accepted.OrderId, accepted.Lines.Count);
                return new CreatedResult($"/api/orders/{accepted.OrderId}", accepted);
            },
            errors =>
            {
                Log.Rejected(logger, errors.Codes());
                return errors.ToProblem();
            }
        );
    }
}
