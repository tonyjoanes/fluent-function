using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FluentFunctions.Core.Orders;
using FluentFunctions.Core.Results;
using FluentFunctions.Functions.Adapters;
using FluentFunctions.Functions.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentFunctions.Functions.ServiceBus;

/// <summary>
/// Thin adapter for the orders queue. Settlement follows the error kind:
/// success completes the message, a permanent failure dead-letters it (retrying won't help),
/// and a transient failure throws so Service Bus redelivers it.
/// </summary>
public sealed class ProcessOrderFunction(
    IOptionsMonitor<OrdersOptions> options,
    TimeProvider clock,
    ILogger<ProcessOrderFunction> logger
)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Function("ProcessOrder")]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBus:QueueName%", Connection = "ServiceBusConnection", AutoCompleteMessages = false)]
            ServiceBusReceivedMessage message,
        ServiceBusMessageActions actions,
        CancellationToken cancellationToken
    )
    {
        var result = Deserialize(message)
            .Bind(order => OrderHandlers.Process(order, clock.GetUtcNow(), options.CurrentValue.ToRules()));

        if (result.IsSuccess)
        {
            Log.OrderProcessed(logger, result.Value.OrderId);
            await actions.CompleteMessageAsync(message, cancellationToken);
            return;
        }

        if (result.Errors.Any(e => e.Kind == ErrorKind.Transient))
        {
            // Let the message lock lapse; Service Bus redelivers it up to the queue's MaxDeliveryCount.
            throw new InvalidOperationException($"Transient failure processing message: {result.Errors.Codes()}");
        }

        Log.DeadLettered(logger, message.MessageId, result.Errors.Codes());
        await actions.DeadLetterMessageAsync(
            message,
            deadLetterReason: result.Errors[0].Code,
            deadLetterErrorDescription: result.Errors.Codes(),
            cancellationToken: cancellationToken
        );
    }

    private static Result<OrderAccepted> Deserialize(ServiceBusReceivedMessage message)
    {
        try
        {
            return message.Body.ToObjectFromJson<OrderAccepted>(Json) is { } order
                ? order
                : Error.Validation("message.body.missing", "The message has no body.");
        }
        catch (JsonException)
        {
            return Error.Validation("message.body.invalid", "The message body is not a valid order.");
        }
    }
}
