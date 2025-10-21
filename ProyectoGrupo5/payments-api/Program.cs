using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "payments-api";
var serviceVersion = "1.0.0";
var endpoint = new Uri(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://otel-collector:4317");

// LOGS
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;

    options.SetResourceBuilder(
        ResourceBuilder.CreateDefault()
                       .AddService(serviceName: serviceName, serviceVersion: serviceVersion));

    options.AddOtlpExporter(opt => { opt.Endpoint = endpoint; });
});

// TRACES + METRICS
builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tp =>
    {
        tp.AddAspNetCoreInstrumentation()
          .AddHttpClientInstrumentation()
          .AddSource("payments-api")
          .AddOtlpExporter(o => o.Endpoint = endpoint);
    })
    .WithMetrics(mp =>
    {
        mp.AddAspNetCoreInstrumentation()
          .AddHttpClientInstrumentation()
          .AddRuntimeInstrumentation()
          .AddMeter("payments-api-meter")
          .AddOtlpExporter(o => o.Endpoint = endpoint);
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
