using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KuaforumAPI.Persistence.Converters
{
    /// <summary>
    /// Veritabaninda medya anahtari saklar, entity'ye tam URL verir.
    ///
    ///   DB     : shops/covers/abc.jpg
    ///   Entity : https://cdn.salonbir.com/shops/covers/abc.jpg
    ///
    /// Amac: saglayici degistiginde (Cloudinary -> R2 -> baska) yalnizca
    /// MediaSettings.BaseUrl degissin, veritabani ve kod ayni kalsin.
    ///
    /// Gecis guvenligi: veritabaninda hala tam URL tutan eski satirlar olabilir.
    /// Mutlak adres goren her iki yon de degeri oldugu gibi gecirir; boylece
    /// veri migrasyonu tamamlanmadan once de uygulama dogru calisir.
    /// </summary>
    public sealed class MediaUrlConverter : ValueConverter<string, string>
    {
        public MediaUrlConverter(string baseUrl)
            : base(
                value => ToStorageKey(value, baseUrl),
                value => ToPublicUrl(value, baseUrl))
        {
        }

        private static bool IsAbsolute(string value) =>
            value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        /// <summary>Entity -> DB. Taban adresi soyup anahtari birakir.</summary>
        private static string ToStorageKey(string value, string baseUrl)
        {
            if (string.IsNullOrEmpty(value)) return value;

            if (!string.IsNullOrEmpty(baseUrl) &&
                value.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return value.Substring(baseUrl.Length).TrimStart('/');
            }

            // Baska bir saglayicinin adresi (eski kayit) — bozmadan gecir.
            return value;
        }

        /// <summary>DB -> Entity. Anahtarin basina taban adresi ekler.</summary>
        private static string ToPublicUrl(string value, string baseUrl)
        {
            if (string.IsNullOrEmpty(value)) return value;

            // Eski satir zaten tam URL tutuyorsa dokunma.
            if (IsAbsolute(value)) return value;

            if (string.IsNullOrEmpty(baseUrl)) return value;

            return $"{baseUrl.TrimEnd('/')}/{value.TrimStart('/')}";
        }
    }
}
