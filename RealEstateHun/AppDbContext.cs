using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

//Itt hozzuk létre a hidat a táblám (ExchangeRate osztály) és az adatbázis között
namespace RealEstateHun
{
    public class AppDbContext : DbContext
    {
        // 1. Ez a meglévő konstruktor, amit a Worker és a Riportáló használ futási időben
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // 2. ITT AZ ÚJ ÜRES KONSTRUKTOR: Ez kell a parancssoros migrációnak!
        public AppDbContext() { }

        public DbSet<ExchangeRate> ExchangeRates { get; set; }
        public DbSet<Property> Properties { get; set; }

        //DESIGN-TIME CONFIG: Ha a parancssorból futtatom, így talál oda a Dockerhez. Ez nem feltétlenül kell, hiszen feljebb már megírtam az adatbázis kapcsolat logikát (lásd options rész, ami majd a worker.cs-ben megkapja azokat a bemeneti konfigurációs paramétereket, amiket itt lentebb látunk.
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Database=real_estate_db;Username=zsombi_admin;Password=TitkosJelszo123");
            }
        }
    }
}
