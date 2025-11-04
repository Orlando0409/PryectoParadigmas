using System.ComponentModel.DataAnnotations;


namespace payments_api.Models
{
    public class Card
    {
        [Key,Required]
        public int Card_Id { get; set; }
        public int User_Id { get; set; }
        [Required]
        public string Card_Type { get; set; }
        [Required]
        public string Card_Number { get; set; }
        [Required]
        public int Money { get; set; }
        [Required]
        public DateTime Expiration_Date { get; set; }
    }
}
