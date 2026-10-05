using System.ComponentModel.DataAnnotations;
using FluentFunctions.Core.Orders;

namespace FluentFunctions.Functions.Configuration;

/// <summary>App settings under <c>Orders__*</c>.</summary>
public sealed class OrdersOptions
{
    public const string Section = "Orders";

    [Range(1, 1000)]
    public int MaxLines { get; init; } = 50;

    [Range(1, 10_000)]
    public int MaxQuantityPerLine { get; init; } = 100;

    [Range(typeof(TimeSpan), "00:01:00", "30.00:00:00")]
    public TimeSpan MaxAge { get; init; } = TimeSpan.FromDays(1);

    public OrderRules ToRules() => new(MaxLines, MaxQuantityPerLine, MaxAge);
}
