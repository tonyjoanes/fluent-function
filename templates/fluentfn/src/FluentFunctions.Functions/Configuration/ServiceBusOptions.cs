using System.ComponentModel.DataAnnotations;

namespace FluentFunctions.Functions.Configuration;

/// <summary>
/// App settings under <c>ServiceBus__*</c>. The connection itself is identity-based and lives under
/// <c>ServiceBusConnection__fullyQualifiedNamespace</c>; there is no connection string.
/// </summary>
public sealed class ServiceBusOptions
{
    public const string Section = "ServiceBus";

    [Required]
    public string QueueName { get; init; } = string.Empty;
}
