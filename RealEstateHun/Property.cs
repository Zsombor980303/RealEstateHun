using System.ComponentModel.DataAnnotations;

namespace RealEstateHun
{
    public class Property
    {
        [Key] //Ez a primary key megadása miatt kell!
        public int Id { get; set; }
        public string Leiras { get; set; }
        public int Alapterulet { get; set; }
        public int Szobak { get; set; }
        public decimal Forint { get; set; }
        public decimal Euro { get; set; }
        public decimal Dollar { get; set; }
        public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
    }
}
