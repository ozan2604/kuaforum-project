namespace KuaforumAPI.Application.Settings
{
    /// <summary>
    /// Medya dosyalarinin servis edildigi adres.
    ///
    /// Veritabaninda tam URL degil, yalnizca anahtar saklanir
    /// (orn. "shops/covers/abc.jpg"). Tam URL bu taban adresle birlestirilerek
    /// uretilir. Saglayici degistiginde (Cloudinary -> R2 -> baska) yalnizca
    /// bu ayar degisir; veritabanina ve koda dokunulmaz.
    /// </summary>
    public class MediaSettings
    {
        /// <summary>Sondaki egik cizgi olmadan, orn. "https://cdn.salonbir.com".</summary>
        public string BaseUrl { get; set; } = string.Empty;
    }
}
