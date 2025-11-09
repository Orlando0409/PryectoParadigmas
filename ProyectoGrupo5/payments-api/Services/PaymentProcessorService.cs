using Microsoft.EntityFrameworkCore;
using Payments.API.Data;
using payments_api.Models;
using System.Text.Json;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Payments.API.Services;

public class PaymentProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMQService _rabbitMQ;
    private readonly ILogger<PaymentProcessorService> _logger;

    private static readonly ActivitySource ActivitySource = new("payments-api");
    private static readonly Counter<long> PaymentsCounter =
        new Meter("payments-api-meter").CreateCounter<long>("payments.processed.count");

    public PaymentProcessorService(
        IServiceScopeFactory scopeFactory,
        RabbitMQService rabbitMQ,
        ILogger<PaymentProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbitMQ = rabbitMQ;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("✅ PaymentProcessorService iniciado. Escuchando mensajes de RabbitMQ...");

        _rabbitMQ.RecibirCompra("pagos.solicitudes", async (message) =>
        {
            await ProcesarSolicitudPagoDesdeRabbitMQ(message);
        });

        return Task.CompletedTask;
    }

    private async Task ProcesarSolicitudPagoDesdeRabbitMQ(string message)
    {
        try
        {
            var solicitud = JsonSerializer.Deserialize<SolicitudPago>(message);
            if (solicitud == null)
            {
                _logger.LogWarning("No se pudo deserializar la solicitud de pago.");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

            await ProcesarPago(solicitud, dbContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar mensaje de RabbitMQ: {Message}", ex.Message);
        }
    }

    public async Task<(bool Success, string Message, decimal? NuevoSaldo)> ProcesarPago(
        SolicitudPago solicitud,
        PaymentsDbContext dbContext)
    {
        using var activity = ActivitySource.StartActivity("ProcessPayment");

        try
        {
            _logger.LogInformation(
                "Procesando pago. ID: {IdCompra}, Total: {Total}, Tarjeta: {Card_Id}",
                solicitud.Purchase_Id, solicitud.Total, solicitud.Card_Id);

            // 🔸 Validar monto inválido
            if (solicitud.Total <= 0)
            {
                _logger.LogWarning("⚠ Monto inválido ({Total}) en la solicitud {IdCompra}", solicitud.Total, solicitud.Purchase_Id);
                PaymentsCounter.Add(1, new KeyValuePair<string, object?>("status", "FAILED"));

                EnviarConfirmacion(solicitud.Purchase_Id, "rechazado", "Monto inválido");
                return (false, "Monto inválido", null);
            }

            // 🔸 Buscar tarjeta
            var tarjeta = await dbContext.Cards.FirstOrDefaultAsync(c => c.Card_Id == solicitud.Card_Id);

            if (tarjeta == null)
            {
                _logger.LogWarning("Tarjeta no encontrada (ID: {Card_Id})", solicitud.Card_Id);
                PaymentsCounter.Add(1, new KeyValuePair<string, object?>("status", "FAILED"));

                EnviarConfirmacion(solicitud.Purchase_Id, "rechazado", "Tarjeta no encontrada");
                return (false, "Tarjeta no encontrada", null);
            }

            // 🔸 Verificar fondos suficientes
            if (tarjeta.Money < solicitud.Total)
            {
                _logger.LogWarning(
                    "Saldo insuficiente. Tarjeta: {Card_Number}, Saldo: {Money}, Monto: {Total}",
                    tarjeta.Card_Number, tarjeta.Money, solicitud.Total);
                PaymentsCounter.Add(1, new KeyValuePair<string, object?>("status", "FAILED"));

                EnviarConfirmacion(solicitud.Purchase_Id, "rechazado", "Saldo insuficiente");
                return (false, "Saldo insuficiente", tarjeta.Money);
            }

            // 🔸 Restar monto y guardar
            tarjeta.Money -= (int)solicitud.Total;

            await dbContext.SaveChangesAsync();

            PaymentsCounter.Add(1, new KeyValuePair<string, object?>("status", "PROCESSED"));

            _logger.LogInformation(
                "Pago aprobado. ID: {IdCompra}, Monto: {Total}, Tarjeta: {Card_Number}, Nuevo saldo: {Money}",
                solicitud.Purchase_Id, solicitud.Total, tarjeta.Card_Number, tarjeta.Money);

            EnviarConfirmacion(solicitud.Purchase_Id, "aprobado", "Pago procesado correctamente");

            return (true, "Pago procesado exitosamente", tarjeta.Money);
        }
        catch (Exception ex)
        {
            PaymentsCounter.Add(1, new KeyValuePair<string, object?>("status", "ERROR"));
            _logger.LogError(ex, " Error inesperado al procesar la compra: {Message}", ex.Message);
            return (false, "Error interno al procesar el pago", null);
        }
    }

    private void EnviarConfirmacion(int purchaseId, string estado, string mensaje)
    {
        var confirmacion = new ConfirmacionPago
        {
            IdCompra = purchaseId,
            Estado = estado,
            Mensaje = mensaje,
            Timestamp = DateTime.UtcNow
        };

        var routingKey = estado == "aprobado" ? "pago.aprobado" : "pago.rechazado";
        _rabbitMQ.PublicarConfirmacion(confirmacion, routingKey);

        _logger.LogInformation(
            "Confirmación enviada a RabbitMQ. ID: {IdCompra}, Estado: {Estado}",
            purchaseId, estado);
    }
}
