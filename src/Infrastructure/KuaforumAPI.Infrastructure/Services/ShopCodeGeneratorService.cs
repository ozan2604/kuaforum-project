using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace KuaforumAPI.Infrastructure.Services
{
    public class ShopCodeGeneratorService : IShopCodeGeneratorService
    {
        private static readonly char[] Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        // Normalize edilmiş (Türkçe karakter yok, küçük harf) şehir adı → plaka kodu
        private static readonly Dictionary<string, string> CityToPlate = new(StringComparer.OrdinalIgnoreCase)
        {
            { "adana", "01" }, { "adiyaman", "02" }, { "afyonkarahisar", "03" }, { "afyon", "03" },
            { "agri", "04" }, { "amasya", "05" }, { "ankara", "06" }, { "antalya", "07" },
            { "artvin", "08" }, { "aydin", "09" }, { "balikesir", "10" }, { "bilecik", "11" },
            { "bingol", "12" }, { "bitlis", "13" }, { "bolu", "14" }, { "burdur", "15" },
            { "bursa", "16" }, { "canakkale", "17" }, { "cankiri", "18" }, { "corum", "19" },
            { "denizli", "20" }, { "diyarbakir", "21" }, { "edirne", "22" }, { "elazig", "23" },
            { "erzincan", "24" }, { "erzurum", "25" }, { "eskisehir", "26" }, { "gaziantep", "27" },
            { "giresun", "28" }, { "gumushane", "29" }, { "hakkari", "30" }, { "hatay", "31" },
            { "isparta", "32" }, { "mersin", "33" }, { "icel", "33" },
            { "istanbul", "34" }, { "izmir", "35" }, { "kars", "36" }, { "kastamonu", "37" },
            { "kayseri", "38" }, { "kirklareli", "39" }, { "kirsehir", "40" }, { "kocaeli", "41" },
            { "izmit", "41" }, { "konya", "42" }, { "kutahya", "43" }, { "malatya", "44" },
            { "manisa", "45" }, { "kahramanmaras", "46" }, { "maras", "46" }, { "mardin", "47" },
            { "mugla", "48" }, { "mus", "49" }, { "nevsehir", "50" }, { "nigde", "51" },
            { "ordu", "52" }, { "rize", "53" }, { "sakarya", "54" }, { "adapazari", "54" },
            { "samsun", "55" }, { "siirt", "56" }, { "sinop", "57" }, { "sivas", "58" },
            { "tekirdag", "59" }, { "tokat", "60" }, { "trabzon", "61" }, { "tunceli", "62" },
            { "sanliurfa", "63" }, { "urfa", "63" }, { "usak", "64" }, { "van", "65" },
            { "yozgat", "66" }, { "zonguldak", "67" }, { "aksaray", "68" }, { "bayburt", "69" },
            { "karaman", "70" }, { "kirikkale", "71" }, { "batman", "72" }, { "sirnak", "73" },
            { "bartin", "74" }, { "ardahan", "75" }, { "igdir", "76" }, { "yalova", "77" },
            { "karabuk", "78" }, { "kilis", "79" }, { "osmaniye", "80" }, { "duzce", "81" }
        };

        private readonly ApplicationDbContext _context;

        public ShopCodeGeneratorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateAsync(string city)
        {
            var plate = GetPlate(city);

            for (int attempt = 0; attempt < 20; attempt++)
            {
                var code = plate + GenerateRandomLetters(6);
                var exists = await _context.Shops.AnyAsync(s => s.Code == code);
                if (!exists) return code;
            }

            throw new InvalidOperationException($"'{city}' şehri için benzersiz salon kodu üretilemedi. Lütfen tekrar deneyin.");
        }

        private static string GetPlate(string city)
        {
            var normalized = NormalizeCity(city);

            // Tam eşleşme
            if (CityToPlate.TryGetValue(normalized, out var plate))
                return plate;

            // Kısmi eşleşme (içinde geçiyorsa)
            foreach (var kvp in CityToPlate)
            {
                if (normalized.Contains(kvp.Key) || kvp.Key.Contains(normalized))
                    return kvp.Value;
            }

            // Bulunamazsa şehir adının ilk 2 rakamı yerine "00" kullan
            return "00";
        }

        private static string NormalizeCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return "";

            return city.Trim()
                .Replace('İ', 'I').Replace('Ş', 'S').Replace('Ğ', 'G')
                .Replace('Ç', 'C').Replace('Ö', 'O').Replace('Ü', 'U')
                .ToLowerInvariant()
                .Replace('ş', 's').Replace('ğ', 'g').Replace('ç', 'c')
                .Replace('ö', 'o').Replace('ü', 'u').Replace('ı', 'i');
        }

        private static string GenerateRandomLetters(int count)
        {
            var sb = new StringBuilder(count);
            for (int i = 0; i < count; i++)
                sb.Append(Letters[Random.Shared.Next(0, Letters.Length)]);
            return sb.ToString();
        }
    }
}
