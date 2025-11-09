using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace payments_api.Models
{
    [Table("Cards")] 
    public class Card
    {
        [Key]
        [Column("Card_Id")]
        public int Card_Id { get; set; }

        [Column("User_Id")]
        public int User_Id { get; set; }

        [Column("Card_Type")]
        public string Card_Type { get; set; }

        [Column("Card_Number")]
        public string Card_Number { get; set; }

        [Column("Money")]
        public int Money { get; set; }


        [Column("Expiration_Date")]
        public DateTime Expiration_Date { get; set; }
    }
}
