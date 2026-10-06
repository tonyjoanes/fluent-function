using Microsoft.Extensions.Logging;

namespace FluentFunctions.Functions.Adapters;

/// <summary>
/// Source-generated log messages. Structured fields only: no message bodies, secrets or personal data.
/// </summary>
internal static partial class Log
{
#if (UseOrders)
    [LoggerMessage(LogLevel.Information, "Order {OrderId} accepted with {LineCount} lines")]
    public static partial void OrderAccepted(ILogger logger, Guid orderId, int lineCount);

    [LoggerMessage(LogLevel.Information, "Order {OrderId} processed")]
    public static partial void OrderProcessed(ILogger logger, Guid orderId);

#endif
#if (UseHttp)
    [LoggerMessage(LogLevel.Warning, "Request rejected: {ErrorCodes}")]
    public static partial void Rejected(ILogger logger, string errorCodes);

#endif
#if (UseServiceBus)
    [LoggerMessage(LogLevel.Warning, "Message {MessageId} dead-lettered: {ErrorCodes}")]
    public static partial void DeadLettered(ILogger logger, string messageId, string errorCodes);

#endif
#if (UseTimer)
    [LoggerMessage(LogLevel.Information, "Clean-up removing records older than {Cutoff:o} in batches of {BatchSize}")]
    public static partial void CleanupPlanned(ILogger logger, DateTimeOffset cutoff, int batchSize);
#endif
}
