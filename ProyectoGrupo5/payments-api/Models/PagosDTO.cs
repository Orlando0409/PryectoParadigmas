namespace payments_api.Models
{
    public class ConfirmacionPago
    {
        public int IdCompra { get; set; }
        public string Estado { get; set; } = string.Empty; // "aprobado" o "rechazado"
        public string Mensaje { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class SolicitudPago
    {
        public int Purchase_Id { get; set; }
        public int Card_Id { get; set; }
        public decimal Total { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public int User_Id { get; set; }
    }

    public class TarjetaPago
    {
        public string Numero { get; set; } = string.Empty;
        public string Titular { get; set; } = string.Empty;
    }
 
}