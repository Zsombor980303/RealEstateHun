//Be kellett ehhez állítanunk a Solution Explorerben a RealEstateReport-on jobb klikk-el, hogy set project dependencies, és kiválasztani a RealEstateHun-t
using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RealEstateHun; //includeolom a másik projektet, hogy lássuk az AppDbContext-et és a Property-t

namespace RealEstateReport
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("RealEstateReport Console elindítva");
            Console.WriteLine("\n⌨️  Nyomj meg egy gombot a CSV adatok beolvasásához és Dockerbe öntéséhez");
            Console.ReadKey();
            Console.WriteLine("\n📥 CSV feldolgozás és Bulk Insert elindítva");

            // Az AppDbContext-et paraméter nélkül példányosítom, így a saját belső jelszavadat fogja használni. Ezért nem kell itt is kiírnom a belépési adatokat, mint a worker.cs-ben. Ott muszáj olyan formában használnom, különben befagyna a programom, ha bármi változna az adatbázisban, miközben a háttérprogramom futna. Itt pedig, mivel ez egy konzol alkalmazás, lefutnak a kérések alább, majd bezáródik a program, így itt adatbáis kapcsolat nem marad feleslegesen nyitva.
            using (var context = new AppDbContext())
            {
                try
                {
                    // A Python által legyártott CSV fájl pontos elhelyezkedése a C# exe mellett
                    string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "budapesti_lakasok.csv");
                    string forrasPythonCsv = @"C:\Users\Zsombi\PycharmProjects\WelcomeScreen\budapesti_lakasok.csv";
                    if (File.Exists(forrasPythonCsv)) File.Copy(forrasPythonCsv, csvPath, true);
                    if (!File.Exists(csvPath))
                    {
                        Console.WriteLine($"Hiba: A [budapesti_lakasok.csv] nem található a program mellett itt: {csvPath}");
                        Console.WriteLine("Tipp: Másold át a CSV fájlt a Python projektből a RealEstateReport bin/Debug/net10.0 mappájába!");
                    }
                    else
                    {
                        //Minden sort beolvasok, a lines pedig egy tömb lesz, ahol az első sor az lines[0], a második az lines[1]
                        var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
                        var propertiesList = new List<Property>();

                        // Az i = 1-ről indulok, mert a 0. a fejléc
                        for (int i = 1; i < lines.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(lines[i])) continue; //üres sort nem veszi figyelembe, megy a következő sorra
                            var cells = lines[i].Split(';'); //egyes sorokban pontosvesszővel vannakelválasztva az adatok, ezt itt szétszedjük
                            if (cells.Length < 6) continue; //Ha nincs meg a 6 adatunk egy sorban, akkor kihagyjuk ezt a sort

                            // Levágjuk a felesleges szóközöket a számok körül
                            string rawTerulet = cells[1].Replace(" ", "");
                            string rawSzobak = cells[2].Replace(" ", "");
                            string rawHuf = cells[3].Replace(" ", "");
                            string rawEur = cells[4].Replace(" ", "");
                            string rawUsd = cells[5].Replace(" ", "");

                            // Egyes esetekben a szobák száma 3 + fél vagy ilyesmi, itt csak az egész számot tartjuk meg most
                            if (rawSzobak.Contains("+"))
                            {
                                rawSzobak = rawSzobak.Split('+')[0].Trim();
                            }

                            // Számmá alakítások a Windows rendszer (vesszős tizedesek) alapján
                            int.TryParse(rawTerulet, out int alapterulet); //átalakítja számmá a szöveget, maennyiben tudja. Ha nem, akkor 0-át ad vissza, ha igen, akkor pedig az alapterulet változóba adja vissza
                            int.TryParse(rawSzobak, out int szobaszam);

                            //NumberStyles.Any: Megmondja a C#-nak, hogy legyen felkészülve bármilyen formátumra, még az Excel-féle tudományos (E+08) jelölésre is, és azt is matekká tudja alakítani
                            //CultureInfo.GetCultureInfo("hu-HU"): Kényszeríti a gépet, hogy magyar észjárással(magyar kultúrkör szerint) olvassa a szöveget, vagyis kőkeményen tudja: nálad a vessző jelenti a tizedesjegyet
                            decimal.TryParse(rawHuf, NumberStyles.Any, CultureInfo.GetCultureInfo("hu-HU"), out decimal hufAr);
                            decimal.TryParse(rawEur, NumberStyles.Any, CultureInfo.GetCultureInfo("hu-HU"), out decimal eurAr);
                            decimal.TryParse(rawUsd, NumberStyles.Any, CultureInfo.GetCultureInfo("hu-HU"), out decimal usdAr);

                            var ingatlan = new Property
                            {
                                Leiras = cells[0].Trim(), //trim --> automatikusan levágja a szöveg elejéről és a legvégéről az összes felesleges, láthatatlan szközt, tabulátort vagy Enter-maradványt
                                Alapterulet = alapterulet,
                                Szobak = szobaszam,
                                Forint = hufAr,
                                Euro = eurAr,
                                Dollar = usdAr,
                                IngestedAt = DateTime.UtcNow
                            };

                            propertiesList.Add(ingatlan);
                        }

                        // Ha vannak érvényes adatok, egyetlen lépésben betoljuk a Dockerbe
                        if (propertiesList.Count > 0)
                        {
                            Console.WriteLine($"📥 {propertiesList.Count} db ingatlan ömlesztett mentése a PostgreSQL-be...");
                            context.Properties.AddRange(propertiesList); //Bulk-insert, ömlesztve hozzáadjuk az összes adatot
                            context.SaveChanges(); //betölti a PostgreSQL-be, elmenti
                            Console.WriteLine("💾 [SIKER]: Minden budapesti lakás elmentve a Docker adatbázisba!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Hiba az adatbázis művelet során: {ex.Message}");
                }
            }

            // 5. A MÚLT HETI STATISZTIKAI ÉS ÁTLAGOLÓ LOGIKÁD FUTTATÁSA
            Console.WriteLine("\n📊 [RIPORT] Adatbázis statisztika lekérése folyamatban...");
            var szerviz = new ExchangeRateService();
            await szerviz.SzamoldKiANapiAtlagotAsync(DateTime.MinValue);

            Console.WriteLine("\nA bezáráshoz nyomj meg egy gombot...");
            Console.ReadKey(); // Ez tartja nyitva a konzolt a végén, amíg gombnyomással be nem zárja a felhasználó
        }
    }
}