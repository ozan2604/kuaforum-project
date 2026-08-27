namespace KuaforumAPI.Application.Interfaces.Services
{
    /// <summary>Yakalanmis bir SMS kaydi.</summary>
    /// <param name="PhoneNumber">Gercekte gonderilecek olan numara.</param>
    /// <param name="Message">SMS metninin tamami.</param>
    /// <param name="Code">Metinden ayiklanan 6 haneli kod; bulunamazsa null.</param>
    /// <param name="CapturedAt">Yakalanma zamani (UTC).</param>
    public sealed record CapturedSms(
        string PhoneNumber,
        string Message,
        string? Code,
        DateTime CapturedAt);

    /// <summary>
    /// SMS yerine yakalanan mesajlarin tutuldugu depo.
    ///
    /// Yalnizca Production DISI ortamlarda kayitlidir. Amac, test ortaminda
    /// gercek SMS gondermeden giris yapabilmek: OTP normal uretilir,
    /// hash'lenir, suresi dolar ve dogrulanir — degisen tek sey kodun
    /// kullaniciya hangi kanaldan ulastigidir.
    /// </summary>
    public interface ICapturedSmsStore
    {
        void Add(CapturedSms sms);

        /// <summary>En yeni kayit basta olacak sekilde son mesajlar.</summary>
        IReadOnlyList<CapturedSms> Recent(int count = 20);

        /// <summary>Belirli bir numaraya ait en son kayit.</summary>
        CapturedSms? LastFor(string phoneNumber);
    }
}
