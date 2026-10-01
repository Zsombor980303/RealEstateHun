import requests
import time
import cloudscraper
from bs4 import BeautifulSoup
import json
from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from webdriver_manager.chrome import ChromeDriverManager

URL_SCRAP = "https://webscraper.io/test-sites/e-commerce/allinone/computers/laptops"
URL_SCRAP_2 = "https://ingatlan.com/budapest/elado+lakas"

#---------------------LAPTOP MEGOLDÁS------------------------------------

try:
    response = requests.get(URL_SCRAP)

    if response.status_code == 200:
        soup = BeautifulSoup(response.content, "html.parser")
        mentendo_sorok = []
        termekek = soup.find_all("div", class_="thumbnail")

        with open(r"C:\Users\Zsombi\source\repos\RealEstateHun\RealEstateHun\bin\Debug\net10.0\arfolyamok.json", "r", encoding="utf-8") as f:
            arfolyam_adatok = json.load(f)
            #Ez átalakítja nekem Dictionary-vé, tehát ilyen formájú lesz a háttérben:
            # arfolyam_adatok = {
            #     "EUR_HUF": 362.05,
            #     "EUR_USD": 1.14
            # }

        #És itt a fentiek miatt elég így hivatkozni rá
        eur_huf = arfolyam_adatok["EUR_HUF"]
        eur_usd = arfolyam_adatok["EUR_USD"]

        for doboz in termekek:
            nev = doboz.find("a").text
            ar = doboz.find("span", itemprop="price").text
            usd_ar = float(ar.replace("$", ""))
            eur_ar = usd_ar / eur_usd
            huf_ar = eur_ar * eur_huf
            print(f"{nev}: {usd_ar}$, {eur_ar}€, {huf_ar}HUF")
            mentendo_sorok.append(f"{nev};{usd_ar};{eur_ar};{huf_ar}\n")

        with open("laptop.csv", "w", encoding="utf-8") as fajl:
            fajl.writelines(mentendo_sorok)
        print("💾 Idézetek sikeresen elmentve az [laptop.csv] fájlba!")

    else:
        print(f"Szerver hiba: {response.status_code}")
except Exception as e:
        print(f"Hiba a laptop scraping során: {e}")

#------------------INGATLAN MEGOLDÁS---------------------------

try:
    print("Láthatatlan Chrome motor indítása a háttérben...")

    #SELENIUM BOTVÉDELEM-KERÜLŐ BEÁLLÍTÁSOK
    #Létrehozunk egy üres konfigurációs listát a Chrome számára. Ebbe pakoljuk bele a parancsokat, mielőtt a böngésző ténylegesen elindulna.
    options = webdriver.ChromeOptions()
    #Megparancsolja, hogy a Chrome böngésző láthatatlanul, a háttérben fusson le. Nem fog felugrani egy igazi Chrome ablak, hanem csendben a RAM memóriában végzi el a munkát
    options.add_argument("--headless")
    # kikapcsolja azt a jelzést, hogy ez egy automata script, így nem látja a bot-bélyeget a böngésző
    options.add_argument("--disable-blink-features=AutomationControlled")
    #Letiltja azt a tipikus felugró sávot a Chrome-ban, ami kiírná, hogy „A Chrome-ot automatizált tesztszoftver vezérli”, és teljesen elrejti az automatizációs kiterjesztéseket a háttérben.
    options.add_experimental_option("excludeSwitches", ["enable-automation"])
    options.add_experimental_option('useAutomationExtension', False)

    #fizikailag életre kelti és beindítja a háttér-Chrome-ot. Megnézi, hogy a laptopon pontosan milyen verziójú Chrome van telepítve, letölti hozzá a megfelelő vezérlő szoftvert (WebDriver-t), és rányomja a fenti álcázó options beállításokat.
    driver = webdriver.Chrome(service=Service(ChromeDriverManager().install()), options=options)

    # Átírjuk a User-Agentet egy éles, lakossági Windows 11-es Chrome böngésző azonosítójára
    driver.execute_cdp_cmd("Network.setUserAgentOverride", {
        "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    })

    mentendo_sorok = []
    driver.get(URL_SCRAP_2)

    # Elgörgetünk az oldal legaljára, különben nem biztos, hogy az egész oldal html szövegét betöltené és leszedné
    driver.execute_script("window.scrollTo(0, document.body.scrollHeight);")

    print("⏳ Várakozás az oldal és a dinamikus hirdetések teljes betöltésére...")
    time.sleep(5)  # 5 másodperc fix türelmi idő a Cloudflare és a JavaScript számára

    # Kiszedem a memóriából a már teljesen feloldott HTML tartalmat
    html_tartalom = driver.page_source
    #Átadom a megszerzett tiszta HTML kódot a BeautifulSoup-nak, ami egy strukturált, kereshető családfává (DOM) alakítja át a szöveget. Innentől kezdve tudunk majd keresni benne a find_all parancsokkal
    soup = BeautifulSoup(html_tartalom, "html.parser")
    print("✔️ HTML sikeresen kiszívva a böngészőből. Feldolgozás indítása...")

    #Minden olyan DIV-et kiszedek, aminek az osztálynevében szerepel a listing-card szó
    ingatlanok = soup.find_all(lambda tag: tag.name == "div" and tag.has_attr("class") and any("listing-card" in c for c in tag["class"]))
    #Sokkal könnyebb volt így debuggolni a későbbiekben, amikor elsőre nem mentett el semmit. Így megkapom a leszedett html formát, amit a fenti sorom "kiválasztott"
    print(f"HTML KÓD NYERSEN: {ingatlanok}")

    #Összekötés a C# projekttel a json file-on keresztül
    with open(r"C:\Users\Zsombi\source\repos\RealEstateHun\RealEstateHun\bin\Debug\net10.0\arfolyamok.json", "r", encoding="utf-8") as f:
        arfolyam_adatok = json.load(f)

    eur_huf = arfolyam_adatok["EUR_HUF"]
    eur_usd = arfolyam_adatok["EUR_USD"]

    print(f" Élő árfolyamok beolvasva a C#-ból sikeres! 1 EUR = {eur_huf} HUF")

    # 5. ADATKINYERŐ ÉS VALUTAVÁLTÓ CIKLUS
    for doboz in ingatlanok:
        try:
            # #-----------------ÚJ MEGOLDÁS--------------------------
            #Kinyerem az összes tiszta szöveget a dobozból
            teljes_szoveg = doboz.get_text(separator="|", strip=True)
            adatok = [s.strip() for s in teljes_szoveg.split("|") if s.strip()]

            #Az ár kiszedése (Megkeresem azt az elemet, amiben ott van a "Ft" szöveg)
            ar_szoveg = next((s for s in adatok if "Ft" in s), None)
            if not ar_szoveg:
                continue

            # A leírás az az elem lesz, ami nem az ár, nem tartalmaz négyzetmétert és nem a szobaszám
            leiras = next((s for s in adatok if "Ft" not in s and "m²" not in s and "m2" not in s and "szoba" not in s and not (s.isdigit() and len(s) == 1)), "Budapesti eladó lakás")

            #Célzott elemkeresés a pontos éles osztálynév alapján
            paraméterek = doboz.find_all("span", class_="fs-6 text-gray-900 fw-bold")

            if len(paraméterek) >= 2:
                # 0. elem = Alapterület (A get_text() egybefűzi a sup-ot, így "76m2" lesz)
                terulet_szoveg = paraméterek[0].get_text(strip=True)
                tiszta_terulet = terulet_szoveg.replace("m", "").replace("2", "").replace(" ", "").strip()
                alapterulet = tiszta_terulet.split(",")[0].split(".")[0]

                # 1. elem = Szobák száma
                szobak = paraméterek[1].get_text(strip=True)
            else:
                alapterulet = "0"
                szobak = "0"

            # Ár megtisztítása és tiszta számmá alakítása milliók kezelésével
            szam_resz = ar_szoveg.replace("M Ft", "").replace("Ft", "").replace(" ", "").replace(",", ".").strip()

            # Ha millió formátumban van (pl. 173 M Ft), megszorzom, különben sima int. Bár itt amúgy mindig M formában van, de így a biztos
            if "M" in ar_szoveg or float(szam_resz) < 1000:
                huf_ar = int(float(szam_resz) * 1000000)
            else:
                huf_ar = int(float(szam_resz.replace(".", "")))

            eur_ar = huf_ar / eur_huf
            usd_ar = eur_ar * eur_usd

            print(f"🏠 {leiras}: {huf_ar} HUF | {int(eur_ar)} EUR | {int(usd_ar)} USD | Terület: {alapterulet} | Szobák: {szobak}")

            # CSV-be mentendő sor
            mentendo_sorok.append(f"{leiras};{alapterulet};{szobak};{huf_ar};{int(eur_ar)};{int(usd_ar)}\n")

        except Exception:
            continue

    driver.quit()
    # 6. FIZIKAI KIÍRÁS A MEREVLEMEZRE A CIKLUS UTÁN
    if mentendo_sorok:
        fejlec = "Leiras;Alapterulet;Szobak;HUF_Ar;EUR_Ar;USD_Ar\n"
        mentendo_sorok.insert(0, fejlec)

        with open("budapesti_lakasok.csv", "w", encoding="utf-8") as fajl:
            fajl.writelines(mentendo_sorok)

        print(f"\n💾 [SIKER]: {len(mentendo_sorok) - 1} db éles budapesti ingatlan kimentve a [budapesti_lakasok.csv] fájlba!")
    else:
        print("\n❌ Hiba: Nem sikerült egyetlen hirdetést sem kinyerni. A keresőrobot blokkolva lett.")

except Exception as e:
    print(f"❌ Kritikus hiba az ingatlan scraping során: {e}")