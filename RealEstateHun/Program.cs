using System.Text.Json;
using System.Globalization; //json miatt, hogy tört számot ne ","-val, hanem "."-al írja
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace RealEstateHun
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // var WorkerObj = new Worker(); Így kellene létrehozni az én, saját osztáylom objektumát, de itt most nem erről van szó, hanem egy beépített keretrendszer által szolgáltatott osztályról. Így nem hívódna meg automatikusan az osztályunkból belüli fv.
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql("Host=localhost;Port=5432;Database=real_estate_db;Username=zsombi_admin;Password=TitkosJelszo123"));
            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Properties"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Leiras"" TEXT NULL,
            ""Alapterulet"" INTEGER NOT NULL,
            ""Szobak"" INTEGER NOT NULL,
            ""Forint"" NUMERIC NOT NULL,
            ""Euro"" NUMERIC NOT NULL,
            ""Dollar"" NUMERIC NOT NULL,
            ""IngestedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL
        );");
            }
            var ExchangeRateServicesObj = new ExchangeRateService();
            //await ExchangeRateServicesObj.SzamoldKiANapiAtlagotAsync(DateTime.MinValue); //TO-DO: Máshova tenni, mert jelenle ki kell kommentenem az app.Run-t ha ezt futtatni akarom
            app.Run();
        }
    }
}
