using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace RealEstateHun
{
    //Ez egy olyan osztály lesz, ahol egy sima fv lesz belül, ami nem automatikusan indul el, hanem kézi vezérléssel
    public class ExchangeRateService
    {
        public async Task SzamoldKiANapiAtlagotAsync(DateTime keresettDatum)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>(); //Itt adom meg, hogy a továbbiakban a konfigurálásra használt optionsBuilder változóm az AppDbContext-ben található adatbázis kapcsolatot konfigurálja
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=real_estate_db;Username=zsombi_admin;Password=TitkosJelszo123");
            using var db = new AppDbContext(optionsBuilder.Options); //Létrehoztam egy konkrét objektumot az AppDbContext tervrajz (osztály) mintájára. Létrehozom a konkrét vezérlő objektumot (parancsnoki központot) a RAM-ban az AppDbContext tervrajz alapján. Ezen a 'db' változón keresztül fogok később parancsokat küldeni az adatbázisnak.
            if(keresettDatum == DateTime.MinValue) //Amennyiben nem akarunk konkrét napra szűrni, akkor napok szerint csoportodítva megkapjuk az összes eddigi adatot. Tehát minden kmentett napnak az átlagát
            {
                var csoportositott_adatok = await db.ExchangeRates
                .GroupBy(s => s.TimeStamp.Date) //Itt történik a csoportosítás, a .Date azért kell, mert alapvetően óra-perc-másodpercet is tartalmazna anélkül
                .Select(g => new //Itt adjuk meg, hogy mi lesz a csoportosítás után a feladat, itt átlagot akarunk látni
                {
                    Datum = g.Key, //Itt nem a tábla hagyományos kulcsáról beszélünk (Id), hanem a csoportosításéról, ami ugye a dátum alapján történt
                    AtlagHuf = g.Average(s => s.EurHuf),
                    AtlagUsd = g.Average(s => s.EurUsd)
                })
                .ToListAsync();

                foreach (var elem in csoportositott_adatok)
                {
                    Console.WriteLine($"[{elem.Datum.ToShortDateString()}]: Átlag EUR/HUF: {elem.AtlagHuf:F2} Ft | Átlag EUR/USD: {elem.AtlagUsd:F4}");
                }
            }
            else
            {
                var keresett_adat = await db.ExchangeRates.Where(s => s.TimeStamp.Date == keresettDatum.Date).ToListAsync();
                if (keresett_adat.Any())
                {
                    decimal atlagHuf = keresett_adat.Average(s => s.EurHuf);
                    decimal atlagUsd = keresett_adat.Average(s => s.EurUsd);
                    Console.WriteLine($"[{keresettDatum.ToShortDateString()}]: Átlag EUR/HUF: {atlagHuf:F2} Ft | Átlag EUR/USD: {atlagUsd:F4}");
                }
                else
                {
                    Console.WriteLine($"Nem található adat a következő napra: {keresettDatum.ToShortDateString()}");
                }
            }
            
            
        }
    }
}
