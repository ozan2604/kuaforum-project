using KuaforumAPI.Application.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace KuaforumAPI.Infrastructure.Services
{
    /// <summary>
    /// SMS gondermek yerine mesaji yakalar — test ortami icin.
    ///
    /// Kimlik dogrulama mantigina DOKUNMAZ. OTP yine rastgele uretilir,
    /// hash'lenir, suresi dolar ve dogrulanir; yalnizca kodun kullaniciya
    /// ulasma kanali degisir. Sabit kod vermek yerine bu yolun secilmesinin
    /// sebebi, test edilen akisin canlidakiyle birebir ayni kalmasi.
    ///
    /// Guvenlik: Production'da bu sinif ORNEKLENEMEZ. Kayit zaten ortama
    /// bagli yapiliyor (Program.cs), ancak yanlis yapilandirmaya karsi
    /// kurucuda ikinci bir kontrol daha var — sessizce SMS yutmak,
    /// acikca patlamaktan cok daha tehlikeli.
    /// </summary>
    public sealed partial class CapturedSmsService : ISmsService
    {
        private readonly ICapturedSmsStore _store;
        private readonly ILogger<CapturedSmsService> _logger;

        public CapturedSmsService(
            ICapturedSmsStore store,
            IHostEnvironment environment,
            ILogger<CapturedSmsService> logger)
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "CapturedSmsService Production ortaminda kullanilamaz. " +
                    "Bu ortamda gercek SMS saglayicisi kayitli olmali.");
            }

            _store = store;
            _logger = logger;
        }

        public Task SendSmsAsync(string phoneNumber, string message)
        {
            var code = SixDigits().Match(message) is { Success: true } m ? m.Value : null;

            _store.Add(new CapturedSms(phoneNumber, message, code, DateTime.UtcNow));

            _logger.LogInformation(
                "SMS yakalandi (gonderilmedi). Hedef: {Phone}, Kod: {Code}",
                phoneNumber, code ?? "-");

            return Task.CompletedTask;
        }

        /// <summary>OTP mesajlarindaki 6 haneli kodu ayiklar.</summary>
        [GeneratedRegex(@"\b\d{6}\b")]
        private static partial Regex SixDigits();
    }
}
