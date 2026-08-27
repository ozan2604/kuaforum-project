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

        /// <summary>
        /// Yuklenen dosyalarin onune eklenen klasor, orn. "test".
        ///
        /// Test ortami canliyla ayni kovayi kullaniyor; ayrim bu onekle
        /// yapiliyor. Boylece test dosyalari canli dosyalarla karismaz ve
        /// gerektiginde "test/" altini silmek yeterli olur.
        ///
        /// Canlida BOS birakilir. Ayri kova acmak yerine onek tercih edildi:
        /// ikinci kova ikinci bir ozel alan adi ve Cloudflare yapilandirmasi
        /// gerektirirdi, kazanci ise ayni.
        /// </summary>
        public string KeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Test APK dosyalarinin bulundugu klasor.
        ///
        /// <see cref="KeyPrefix"/>'ten AYRI ve ortamdan bagimsizdir: APK'lari
        /// buraya mobil deponun surekli tumlestirme hatti birakiyor ve o hat
        /// API'nin hangi ortamda kostugunu bilmez. Medya onekine baglanmis
        /// olsaydi, test API'si dosyalari "test/apk/" altinda arar ama hat
        /// "apk/" altina yazardi.
        /// </summary>
        public string ApkPrefix { get; set; } = "apk/";

        /// <summary>Ayarlarin tamami dolu mu — eksikse servis hata firlatir.</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AccountId) &&
            !string.IsNullOrWhiteSpace(AccessKey) &&
            !string.IsNullOrWhiteSpace(SecretKey) &&
            !string.IsNullOrWhiteSpace(Bucket);
    }
}
