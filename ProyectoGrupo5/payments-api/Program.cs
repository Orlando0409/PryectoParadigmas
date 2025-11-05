using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Payments.API.Services;
using Payments.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "payments-api";
var serviceVersion = "1.0.0";
var endpoint = new Uri(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://otel-collector:4317");

// Configurar MySQL
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

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
// Servicios
builder.Services.AddSingleton<RabbitMQService>();
// Registrar PaymentProcessorService como singleton y también como HostedService
builder.Services.AddSingleton<PaymentProcessorService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PaymentProcessorService>());

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
