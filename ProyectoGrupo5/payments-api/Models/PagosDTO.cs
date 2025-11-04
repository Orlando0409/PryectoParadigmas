namespace payments_api.Models
{
    public class ConfirmacionPago
    {
        public string IdCompra { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty; // "aprobado" o "rechazado"
        public string Mensaje { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class SolicitudPago
    {
        public string IdCompra { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public TarjetaPago Tarjeta { get; set; } = new();
    }

    public class TarjetaPago
    {
        public string Numero { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
    }
 
}