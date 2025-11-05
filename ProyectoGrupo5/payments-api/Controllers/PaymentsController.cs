using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Payments.API.Data;
using Payments.API.Services;
using payments_api.Models;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly PaymentsDbContext _dbContext;
    private readonly PaymentProcessorService _paymentProcessor;

    public PaymentsController(
        ILogger<PaymentsController> logger,
        PaymentsDbContext dbContext,
        PaymentProcessorService paymentProcessor)
    {
        _logger = logger;
        _dbContext = dbContext;
        _paymentProcessor = paymentProcessor;
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok" });

    // Endpoint principal para procesar pagos via HTTP
    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromBody] SolicitudPago solicitud)
    {
        Console.WriteLine($"Procesando pago :{solicitud.Purchase_Id }");
        var (success, message, nuevoSaldo) = await _paymentProcessor.ProcesarPago(solicitud, _dbContext);

        if (!success)
        {
            return BadRequest(new { error = message, saldoActual = nuevoSaldo });
        }

        return Ok(new
        {
            message,
            idCompra = solicitud.Purchase_Id,
            nuevoSaldo
        });
    }

    // Endpoint para consultar el saldo de una tarjeta
    [HttpGet("cards/{cardNumber}/balance")]
    public async Task<IActionResult> GetBalance(string cardNumber)
    {
        var card = await _dbContext.Cards.FirstOrDefaultAsync(c => c.Card_Number == cardNumber);

        if (card == null)
            return NotFound(new { error = "Tarjeta no encontrada" });

        return Ok(new { cardNumber = card.Card_Number, balance = card.Money });
    }

    // Endpoint para crear/registrar una tarjeta
    [HttpPost("cards")]
    public async Task<IActionResult> CreateCard([FromBody] CreateCardRequest request)
    {
        var existingCard = await _dbContext.Cards
            .FirstOrDefaultAsync(c => c.Card_Number == request.CardNumber);

        if (existingCard != null)
            return BadRequest(new { error = "La tarjeta ya existe" });

        var card = new Card
        {
            User_Id = request.UserId,
            Card_Type = request.CardType,
            Card_Number = request.CardNumber,
            Money = request.InitialBalance,
            Expiration_Date = request.ExpirationDate
        };

        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Tarjeta creada: {CardNumber}, Usuario: {UserId}, Tipo: {CardType}, Saldo: {Money}",
            card.Card_Number, card.User_Id, card.Card_Type, card.Money);

        return Ok(new { message = "Tarjeta creada exitosamente", card });
    }

    // Endpoint para listar todas las tarjetas
    [HttpGet("cards")]
    public async Task<IActionResult> GetAllCards()
    {
        var cards = await _dbContext.Cards.ToListAsync();
        return Ok(cards);
    }
}

public record CreateCardRequest(
    int UserId,
    string CardType,
    string CardNumber,
    int InitialBalance,
    DateTime ExpirationDate
);