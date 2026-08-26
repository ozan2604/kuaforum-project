namespace KuaforumAPI.Application.Settings
{
    /// <summary>
    /// Cloudflare R2 (S3 uyumlu) depolama ayarlari.
    ///
    /// Yalnizca YAZMA icin kullanilir. Okuma tarafinda bu bilgiler gerekmez:
    /// dosyalar herkese acik ozel alan adindan servis edilir ve adres
    /// <see cref="MediaSettings.BaseUrl"/> uzerinden uretilir.
    /// </summary>
    public class R2Settings
    {
        /// <summary>Cloudflare hesap kimligi; S3 uc noktasi bundan turetilir.</summary>
        public string AccountId { get; set; } = string.Empty;

        public string AccessKey { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        /// <summary>Kova adi, orn. "salonbir-media".</summary>
        public string Bucket { get; set; } = string.Empty;

        /// <summary>Ayarlarin tamami dolu mu — eksikse servis hata firlatir.</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AccountId) &&
            !string.IsNullOrWhiteSpace(AccessKey) &&
            !string.IsNullOrWhiteSpace(SecretKey) &&
            !string.IsNullOrWhiteSpace(Bucket);
    }
}
