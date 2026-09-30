# Automatizált Real Estate Data Pipeline és Exchange Rate Monitor

Egy elosztott, hibrid működésű **Adatgyűjtő és Adattárház (Data Warehouse)** rendszer, amely a háttérben folyamatosan monitorozza a Magyar Nemzeti Bank (MNB) devizaárfolyamait, míg a kliens oldalon automatizált web scraping segítségével gyűjti, tisztítja és valutaváltás után Docker PostgreSQL adatbázisba önti a budapesti ingatlanpiaci adatokat.


# Rendszerarchitektúra & Technológiák

A szoftverrendszer három fő, egymástól elkülönülő feladattal bíró komponensből és egy központi adatbázisból áll:

- Háttérszolgáltatás (Worker Service - C# / .NET): 0-24 órában futó aszinkron Windows szolgáltatás architektúra. Időzítve rángatja az MNB API-t, az adatokat strukturáltan menti a PostgreSQL adatbázisba, és kiír egy lokális, élő `arfolyamok.json` fájlt a Python scraper számára.
- Adathalász Robot (Web Scraper - Python / Selenium / BeautifulSoup): Egyoldalas, célzott scraper. Beolvassa a C# által biztosított élő JSON árfolyamadatokat, álcázott böngészővel kinyeri az ingatlanhirdetéseket, elvégzi a dinamikus HUF/EUR/USD valutaváltást, majd egy letisztított CSV fájlt generál.
- Adatbeöntő & Elemző (Riport Console - C# / .NET / EF Core): Gombnyomás-vezérelt. Automatikusan átmásolja a Python projektből a legfrissebb CSV-t, elvégzi a számmá alakítási és szövegtisztítási feladatokat, majd Entity Framework Core Bulk Insert segítségével hozzáfűzi az adatokat az adatbázishoz, végül statisztikai elemzést futtat.
- Adattárház (Docker PostgreSQL): Konténerizált relációs adatbázis, amely elkülönített táblákban gyűjti a történelmi árfolyam-idősorokat és az ingatlanpiaci rekordokat.

---

# Egyéb biztonsági megoldások

- CQRS Alapok: Az írási feladatokat (0-24 árfolyam-mentés) és az olvasási/elemzési feladatokat (Riport modul) szigorúan elkülönítettem egymástól.
- Hibrid DB Konfiguráció: Az `AppDbContext` kétcsatornás védelmet kapott. A parancssoros migrációs tervezőmotor kiszolgálására a beépített belső vészkapu (`OnConfiguring`) felel, míg éles futáskor a modern Dependency Injection csatornán kapja meg a konfigurációt.
- Memóriabiztonság háttérprogramokban: A Worker Service-ben az adatbázis-kapcsolat nem él örökké. Minden ciklusban egy rövid életű, elszigetelt `using var db` példány jön létre, ami a mentés után azonnal megsemmisül, így kiküszöböli a memóriaszeszivárgást (Memory Leak).
- Adattisztítás és Hibatűrés: A Python és C# oldalon is szigorú parser védvonalak (mint a `TryParse` és szöveges minták lefejezése) dobják el a sérült CSV sorokat, megvédve a Docker adatbázist az összeomlástól. Emellett try-catch, try-exception blokkok használata.