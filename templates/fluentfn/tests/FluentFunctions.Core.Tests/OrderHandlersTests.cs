using FluentFunctions.Core.Orders;

namespace FluentFunctions.Core.Tests;

public class OrderHandlersTests
{
    private static readonly OrderRules Rules = new(MaxLines: 3, MaxQuantityPerLine: 10, MaxAge: TimeSpan.FromHours(1));
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrderId = Guid.Parse("8d4c3a43-36a1-4a83-a8a6-0d1e4a0f6a10");

    [Fact]
    public void Place_accepts_a_valid_order_and_totals_it()
    {
        var request = new PlaceOrder(" cust-1 ", [new OrderLine(" SKU-1 ", 2, 9.99m), new OrderLine("SKU-2", 1, 5m)]);

        var result = OrderHandlers.Place(request, OrderId, Now, Rules);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderId, result.Value.OrderId);
        Assert.Equal("cust-1", result.Value.CustomerId);
        Assert.Equal("SKU-1", result.Value.Lines[0].Sku);
        Assert.Equal(24.98m, result.Value.Total);
        Assert.Equal(Now, result.Value.AcceptedAt);
    }

    [Fact]
    public void Place_reports_every_header_problem_at_once()
    {
        var result = OrderHandlers.Place(new PlaceOrder(null, []), OrderId, Now, Rules);

        Assert.Equal(["order.customer.missing", "order.lines.empty"], result.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Place_reports_line_problems_with_their_position()
    {
        var request = new PlaceOrder("cust-1", [new OrderLine("SKU-1", 1, 1m), new OrderLine("", 11, -1m)]);

        var result = OrderHandlers.Place(request, OrderId, Now, Rules);

        Assert.Equal(
            ["Lines[1].Sku", "Lines[1].Quantity", "Lines[1].UnitPrice"],
            result.Errors.Select(e => e.Target)
        );
    }

    [Fact]
    public void Place_rejects_too_many_lines()
    {
        var lines = Enumerable.Range(0, 4).Select(i => new OrderLine($"SKU-{i}", 1, 1m)).ToList();

        var result = OrderHandlers.Place(new PlaceOrder("cust-1", lines), OrderId, Now, Rules);

        Assert.Contains(result.Errors, e => e.Code == "order.lines.too_many");
    }

    [Fact]
    public void Process_completes_a_fresh_order()
    {
        var order = new OrderAccepted(OrderId, "cust-1", [new OrderLine("SKU-1", 2, 3m)], 6m, Now);

        var result = OrderHandlers.Process(order, Now.AddMinutes(5), Rules);

        Assert.True(result.IsSuccess);
        Assert.Equal(6m, result.Value.Total);
    }

    [Fact]
    public void Process_rejects_an_expired_order()
    {
        var order = new OrderAccepted(OrderId, "cust-1", [new OrderLine("SKU-1", 1, 1m)], 1m, Now);

        var result = OrderHandlers.Process(order, Now.AddHours(2), Rules);

        Assert.Equal("order.expired", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Process_rejects_a_tampered_total()
    {
        var order = new OrderAccepted(OrderId, "cust-1", [new OrderLine("SKU-1", 1, 1m)], 100m, Now);

        var result = OrderHandlers.Process(order, Now, Rules);

        Assert.Equal("order.total.mismatch", Assert.Single(result.Errors).Code);
    }
}
