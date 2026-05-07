namespace KuaforumAPI.Application.Settings
{
    public class NetGsmSettings
    {
        public string UserCode { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string MessageHeader { get; set; } = "VefaCelik";

        /// <summary>
        /// Dolu ise tüm SMS'ler gerçek alıcı yerine bu numaraya gönderilir.
        /// Sadece geliştirme/test ortamında kullanılmalı.
        /// </summary>
        public string? TestPhoneOverride { get; set; }
    }
}
