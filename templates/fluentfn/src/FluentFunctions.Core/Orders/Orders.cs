namespace FluentFunctions.Core.Orders;

// Messages are immutable records. The same shapes travel over HTTP and Service Bus, so keep them
// free of transport concerns (no attributes from ASP.NET Core or the Functions SDK).

/// <summary>A request to place an order, as a caller sends it.</summary>
public sealed record PlaceOrder(string? CustomerId, IReadOnlyList<OrderLine>? Lines);

public sealed record OrderLine(string? Sku, int Quantity, decimal UnitPrice);

/// <summary>An order that passed validation and was accepted.</summary>
public sealed record OrderAccepted(
    Guid OrderId,
    string CustomerId,
    IReadOnlyList<OrderLine> Lines,
    decimal Total,
    DateTimeOffset AcceptedAt
);

/// <summary>The outcome of processing an accepted order.</summary>
public sealed record OrderProcessed(Guid OrderId, decimal Total, DateTimeOffset ProcessedAt);

/// <summary>Business limits for orders. Supplied from configuration by the adapters.</summary>
public sealed record OrderRules(int MaxLines, int MaxQuantityPerLine, TimeSpan MaxAge);
