namespace RealEstateHun
{
    public class ExchangeRate
    {
        public int Id { get; set; } //automatikusan ezt veszi kulcsnak, ha Id a neve
        public decimal EurHuf { get; set; }
        public decimal EurUsd { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
