using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace RealEstateHun
{
    public class Worker : BackgroundService
    {

        //kötelező függvényfej, amit a .NET keretrendszer keres, és amit magától, automatikusan el fog indítani a háttérben, amikor a szolgáltatás beindul
        //protected override --> Ez azt jelenti, hogy felül fogjuk írni a gyári BackgroundService szülőtől örökölt tulajdonságok némelyikét
        //async Task --> Ez garantálja, hogy a függvény képes használni az await szót, lehetővé teszi, hogy az időzítő és az adatbázis-mentés a háttérben, a többi programszál blokkolása nélkül fusson le
        //ExecuteAsync --> Ezzel adjuk meg, hogy tulajdonképpen ez lesz az első meghívott fv-ünk amint elindítjuk a programot. Mármint a main fv után, hiszen ott fogok létrehozni egy konkrét objektumot ebből az osztályból. A környezet pedig tudja fogja emiatt a kulcsszó miatt, hogy amint létrehoztam a konkrét objektumot ebből az osztályból, azonnal indítsa el ezt a benn elévő fv-t.
        //CancellationToken stoppingToken --> ha leáll a szolgáltatásunk (programunk, vagy akár a gépünk), akkor leállítja a while ciklust is. A while ciklusunk ezt a zászlót fogja figyelni, így ha a Windows leáll, a program nem fog összeomlani, hanem adatvesztés nélkül azonnal bezárja magát a háttérben
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>(); //Itt adom meg, hogy a továbbiakban a konfigurálásra használt optionsBuilder változóm az AppDbContext-ben található adatbázis kapcsolatot konfigurálja
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=real_estate_db;Username=zsombi_admin;Password=TitkosJelszo123");
            using var db = new AppDbContext(optionsBuilder.Options); //Létrehoztam egy konkrét objektumot az AppDbContext tervrajz (osztály) mintájára. Létrehozom a konkrét vezérlő objektumot (parancsnoki központot) a RAM-ban az AppDbContext tervrajz alapján. Ezen a 'db' változón keresztül fogok később parancsokat küldeni az adatbázisnak.
            //db.Database.EnsureCreated(); // Ezzel a paranccsal hozom létre fizikailag a táblát az SQL adatbázisban (Dockerben). Megnézi, létezik-e már a tábla, és ha nem, az ExchangeRate osztály oszlopai alapján legyártja.
            db.Database.Migrate();

            var idozito = new PeriodicTimer(TimeSpan.FromSeconds(10)); //10 másodperces időzítőt hozunk létre
            while (await idozito.WaitForNextTickAsync())
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        string baseUrl = "https://open.er-api.com";
                        string endpoint = "/v6/latest/EUR";
                        string url = baseUrl + endpoint;
                        string response = await client.GetStringAsync(url);
                        using (JsonDocument doc = JsonDocument.Parse(response))
                        {
                            decimal hufArfolyam = doc.RootElement.GetProperty("rates").GetProperty("HUF").GetDecimal();
                            Console.WriteLine($"Sikeres feldolgozás! 1 EUR = {hufArfolyam} HUF");
                            decimal usdArfolyam = doc.RootElement.GetProperty("rates").GetProperty("USD").GetDecimal();
                            Console.WriteLine($"Sikeres feldolgozás! 1 EUR = {usdArfolyam} USD");
                            string jsonMentes = $"{{\"EUR_HUF\": {hufArfolyam.ToString(CultureInfo.InvariantCulture)}, \"EUR_USD\": {usdArfolyam.ToString(CultureInfo.InvariantCulture)}}}";
                            System.IO.File.WriteAllText("arfolyamok.json", jsonMentes);
                            Console.WriteLine("Árfolyamok sikeresen elmentve az [arfolyamok.json] fájlba!/");

                            //Létrehozzuk a mentendő csomagot
                            var ujAdat = new ExchangeRate
                            {
                                EurHuf = hufArfolyam,
                                EurUsd = usdArfolyam,
                                TimeStamp = DateTime.UtcNow
                            };

                            db.ExchangeRates.Add(ujAdat);
                            await db.SaveChangesAsync();


                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hiba történt: {ex.Message} | Belső hiba: {ex.InnerException?.Message}");
                }
            }
        }
    }
}
