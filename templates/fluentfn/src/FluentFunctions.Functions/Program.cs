using Azure.Monitor.OpenTelemetry.Exporter;
using FluentFunctions.Functions.Configuration;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
#if (UseHttp)

// ASP.NET Core integration: HttpRequest/IActionResult in HTTP functions.
builder.ConfigureFunctionsWebApplication();
#endif

builder.Services.AddSingleton(TimeProvider.System);

// Typed, validated configuration. The host refuses to start if any of these are missing or invalid.
#if (UseOrders)
builder.Services.AddValidatedOptions<OrdersOptions>(OrdersOptions.Section);
#endif
#if (UseServiceBus)
builder.Services.AddValidatedOptions<ServiceBusOptions>(ServiceBusOptions.Section);
#endif
#if (UseTimer)
builder.Services.AddValidatedOptions<CleanupOptions>(CleanupOptions.Section);
#endif

// OpenTelemetry for traces, metrics and logs. Exported to Application Insights with Entra ID auth
// when a connection string is present; locally, nothing is exported unless you set one.
var telemetry = builder.Services.AddOpenTelemetry().UseFunctionsWorkerDefaults();
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    var credential = AzureCredentials.Create(builder.Configuration);
    telemetry.UseAzureMonitorExporter(options => options.Credential = credential);
}

await builder.Build().RunAsync();
