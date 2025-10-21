using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;

    private static readonly ActivitySource ActivitySource = new("payments-api");
    private static readonly Meter Meter = new("payments-api-meter");
    private static readonly Counter<long> PaymentsCounter =
        Meter.CreateCounter<long>("payments.processed.count");

    public PaymentsController(ILogger<PaymentsController> logger)
    {
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok" });

    [HttpPost("process")]
    public IActionResult Process([FromBody] PaymentRequest req)
    {
        using var activity = ActivitySource.StartActivity("ProcessPayment");

        try
        {
            // Simular un error de negocio
            if (req.Amount <= 0)
            {
                _logger.LogWarning("La compra no se ha realizado: monto inválido ({Amount}).", req.Amount);
                PaymentsCounter.Add(1, new KeyValuePair<string, object?>("status", "FAILED"));
                return BadRequest(new { error = "Monto inválido" });
            }

            // Simular éxito
            PaymentsCounter.Add(1, new KeyValuePair<string, object?>("status", "PROCESSED"));
            _logger.LogInformation(
                "La compra se ha realizado correctamente. Monto: {Amount} {Currency}, ID: {PurchaseId}",
                req.Amount, req.Currency, req.PurchaseId);

            return Ok(new { result = "processed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar la compra: {Message}", ex.Message);
            PaymentsCounter.Add(1, new KeyValuePair<string, object?>("status", "ERROR"));
            return StatusCode(500, new { error = "Error interno" });
        }
    }
}

public record PaymentRequest(decimal Amount, string Currency, string PurchaseId);
