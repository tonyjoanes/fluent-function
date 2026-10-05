using FluentFunctions.Core.Results;

namespace FluentFunctions.Core.Orders;

/// <summary>
/// Pure order logic. Everything a handler needs (the clock, ids, rules) arrives as an argument,
/// so the handlers are deterministic and can be unit tested without mocks or a Functions host.
/// </summary>
public static class OrderHandlers
{
    public static Result<OrderAccepted> Place(PlaceOrder request, Guid orderId, DateTimeOffset now, OrderRules rules) =>
        Result
            .Validate(
                request,
                r => string.IsNullOrWhiteSpace(r.CustomerId)
                    ? Error.Validation("order.customer.missing", "A customer id is required.", nameof(r.CustomerId))
                    : null,
                r => r.Lines is null || r.Lines.Count == 0
                    ? Error.Validation("order.lines.empty", "An order needs at least one line.", nameof(r.Lines))
                    : null,
                r => r.Lines?.Count > rules.MaxLines
                    ? Error.Validation(
                        "order.lines.too_many",
                        $"An order can have at most {rules.MaxLines} lines.",
                        nameof(r.Lines)
                    )
                    : null
            )
            .Bind(r => ValidateLines(r.Lines!, rules).Map(lines => (CustomerId: r.CustomerId!.Trim(), Lines: lines)))
            .Map(valid => new OrderAccepted(orderId, valid.CustomerId, valid.Lines, TotalOf(valid.Lines), now));

    public static Result<OrderProcessed> Process(OrderAccepted order, DateTimeOffset now, OrderRules rules)
    {
        if (now - order.AcceptedAt > rules.MaxAge)
        {
            return Error.Validation(
                "order.expired",
                $"The order was accepted more than {rules.MaxAge} ago and can no longer be processed."
            );
        }

        if (TotalOf(order.Lines) != order.Total)
        {
            return Error.Validation("order.total.mismatch", "The order total does not match its lines.");
        }

        return new OrderProcessed(order.OrderId, order.Total, now);
    }

    private static Result<IReadOnlyList<OrderLine>> ValidateLines(IReadOnlyList<OrderLine> lines, OrderRules rules)
    {
        var errors = lines
            .SelectMany(
                (line, index) =>
                    new[]
                    {
                        string.IsNullOrWhiteSpace(line.Sku)
                            ? Error.Validation("order.line.sku.missing", "Each line needs a SKU.", $"Lines[{index}].Sku")
                            : null,
                        line.Quantity < 1 || line.Quantity > rules.MaxQuantityPerLine
                            ? Error.Validation(
                                "order.line.quantity.out_of_range",
                                $"Quantity must be between 1 and {rules.MaxQuantityPerLine}.",
                                $"Lines[{index}].Quantity"
                            )
                            : null,
                        line.UnitPrice < 0
                            ? Error.Validation(
                                "order.line.price.negative",
                                "Unit price cannot be negative.",
                                $"Lines[{index}].UnitPrice"
                            )
                            : null,
                    }
            )
            .OfType<Error>()
            .ToList();

        return errors.Count == 0
            ? Result.Success<IReadOnlyList<OrderLine>>(lines.Select(l => l with { Sku = l.Sku!.Trim() }).ToList())
            : Result<IReadOnlyList<OrderLine>>.Failure(errors);
    }

    private static decimal TotalOf(IEnumerable<OrderLine> lines) => lines.Sum(l => l.Quantity * l.UnitPrice);
}
