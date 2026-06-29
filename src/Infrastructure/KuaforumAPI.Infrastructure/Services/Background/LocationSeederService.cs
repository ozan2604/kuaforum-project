using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services.Background
{
    public class LocationSeederService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LocationSeederService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public LocationSeederService(
            IServiceProvider serviceProvider, 
            ILogger<LocationSeederService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Check if cities are already seeded
                if (!await context.Cities.AnyAsync(stoppingToken))
                {
                    _logger.LogInformation("Starting location seeding from turkiyeapi...");
                    await SeedTurkiyeDataAsync(context, stoppingToken);
                    
                    _logger.LogInformation("Seeding Cyprus data...");
                    await SeedCyprusDataAsync(context, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding locations.");
            }
        }

        private async Task SeedTurkiyeDataAsync(ApplicationDbContext context, CancellationToken stoppingToken)
        {
            var client = _httpClientFactory.CreateClient("turkiyeapi");

            // Fetch provinces (includes districts)
            var response = await client.GetAsync("v1/provinces", stoppingToken);
            if (!response.IsSuccessStatusCode) return;

            var content = await response.Content.ReadAsStringAsync(stoppingToken);
            using var doc = JsonDocument.Parse(content);
            
            var dataArray = doc.RootElement.GetProperty("data").EnumerateArray();
            var cities = new List<City>();
            var districts = new List<District>();

            foreach (var province in dataArray)
            {
                int cityId = province.GetProperty("id").GetInt32();
                string cityName = province.GetProperty("name").GetString() ?? "";

                var city = new City { Id = cityId, Name = cityName };
                cities.Add(city);

                var provinceDistricts = province.GetProperty("districts").EnumerateArray();
                foreach (var districtData in provinceDistricts)
                {
                    int districtId = districtData.GetProperty("id").GetInt32();
                    string districtName = districtData.GetProperty("name").GetString() ?? "";
                    
                    districts.Add(new District { Id = districtId, Name = districtName, CityId = cityId });
                }
            }

            await context.Cities.AddRangeAsync(cities, stoppingToken);
            await context.Districts.AddRangeAsync(districts, stoppingToken);
            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation($"Seeded {cities.Count} cities and {districts.Count} districts for Turkiye. Neighborhoods will be fetched dynamically or need a full DB dump.");

            // To avoid blocking startup for an hour fetching 973 districts' neighborhoods, 
            // we will let the LocationController fetch and save them on-demand if they don't exist.
            // Alternatively, we could seed them here if we had a single endpoint.
        }

        private async Task SeedCyprusDataAsync(ApplicationDbContext context, CancellationToken stoppingToken)
        {
            int cyprusCityId = 82; // Custom ID for Cyprus
            
            if (await context.Cities.AnyAsync(c => c.Id == cyprusCityId, stoppingToken)) return;

            var cyprus = new City { Id = cyprusCityId, Name = "Kıbrıs (KKTC)" };
            await context.Cities.AddAsync(cyprus, stoppingToken);

            var districts = new List<District>
            {
                new District { Id = 10001, Name = "Lefkoşa", CityId = cyprusCityId },
                new District { Id = 10002, Name = "Gazimağusa", CityId = cyprusCityId },
                new District { Id = 10003, Name = "Girne", CityId = cyprusCityId },
                new District { Id = 10004, Name = "Güzelyurt", CityId = cyprusCityId },
                new District { Id = 10005, Name = "İskele", CityId = cyprusCityId },
                new District { Id = 10006, Name = "Lefke", CityId = cyprusCityId }
            };

            await context.Districts.AddRangeAsync(districts, stoppingToken);
            
            // Adding a comprehensive list of neighborhoods for each district
            var neighborhoods = new List<Neighborhood>();
            int nId = 100000;

            // 10001: Lefkoşa
            string[] lefkosaMah = { "Arabahmet", "Ayluka", "Ayios Dhometios", "Çağlayan", "Göçmenköy", "Hamitköy", "Haspolat", "Kızılay", "Köşklüçiftlik", "Kumsal", "Küçük Kaymaklı", "Marmara", "Metehan", "Ortaköy", "Surlariçi", "Taşkınköy", "Yenişehir", "Yenikent" };
            foreach (var m in lefkosaMah) neighborhoods.Add(new Neighborhood { Id = nId++, Name = m, DistrictId = 10001 });

            // 10002: Gazimağusa
            string[] magusaMah = { "Sur İçi", "Baykal", "Çanakkale", "Dumlupınar", "Karakol", "Sakarya", "Tuzla", "Mutluyaka", "Maraş", "Anadolu", "Piyale Paşa", "Zafer", "Yeni Boğaziçi", "Gazi", "Kemalpaşa" };
            foreach (var m in magusaMah) neighborhoods.Add(new Neighborhood { Id = nId++, Name = m, DistrictId = 10002 });

            // 10003: Girne
            string[] girneMah = { "Aşağı Girne", "Yukarı Girne", "Zeytinlik", "Karaoğlanoğlu", "Alsancak", "Lapta", "Karşıyaka", "Çatalköy", "Ozanköy", "Bellapais", "Doğanköy", "Edremit", "Esentepe", "Bahçeli", "Ilgaz", "Malatya", "İncesu" };
            foreach (var m in girneMah) neighborhoods.Add(new Neighborhood { Id = nId++, Name = m, DistrictId = 10003 });

            // 10004: Güzelyurt
            string[] guzelyurtMah = { "Aşağı Bostancı", "Yukarı Bostancı", "Aydınköy", "Gaziveren", "Kalkanlı", "Mevlevi", "Serhatköy", "Şahinler", "Yayla", "Zümrütköy", "Akçay", "Güneşköy" };
            foreach (var m in guzelyurtMah) neighborhoods.Add(new Neighborhood { Id = nId++, Name = m, DistrictId = 10004 });

            // 10005: İskele
            string[] iskeleMah = { "İskele Merkez", "Aygün", "Bafra", "Boğaziçi", "Boğaztepe", "Cevizli", "Dipkarpaz", "Erenköy", "Kumyalı", "Mehmetçik", "Sipahi", "Yedikonuk", "Ziyamet", "Kalecik", "Kurtuluş", "Ötüken", "Ağıllar", "Altınova" };
            foreach (var m in iskeleMah) neighborhoods.Add(new Neighborhood { Id = nId++, Name = m, DistrictId = 10005 });

            // 10006: Lefke
            string[] lefkeMah = { "Lefke Merkez", "Gemikonağı", "Yeşilyurt", "Doğancı", "Çamlıköy", "Bağlıköy", "Yedidalga", "Bademliköy", "Yeşilırmak" };
            foreach (var m in lefkeMah) neighborhoods.Add(new Neighborhood { Id = nId++, Name = m, DistrictId = 10006 });

            await context.Neighborhoods.AddRangeAsync(neighborhoods, stoppingToken);
            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("Successfully seeded Cyprus data (City, Districts, Neighborhoods).");
        }
    }
}
