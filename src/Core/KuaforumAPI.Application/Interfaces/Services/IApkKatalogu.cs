using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    /// <summary>Depoda duran bir test APK dosyasi.</summary>
    public sealed record ApkDosyasi(
        string DosyaAdi,
        string IndirmeUrl,
        long Boyut,
        DateTime YuklenmeZamani,
        string? CalistirmaNo,
        string? CommitKisa)
    {
        /// <summary>
        /// "salonbir-test-1234-a1b2c3d.apk" -> ("1234", "a1b2c3d").
        ///
        /// Calistirma numarasi ve commit dosya ADINDA tasiniyor: S3 liste yaniti
        /// yalnizca anahtar, boyut ve tarih donuyor, ozel ustbilgileri okumak
        /// icin her nesne icin ayri bir istek atmak gerekirdi.
        ///
        /// Son iki parcanin BICIMI de dogrulanir: calistirma numarasi yalnizca
        /// rakam, commit yalnizca onaltilik karakter olmali. Yalnizca parca
        /// sayisina bakmak yetmiyordu — "elle-atilmis.apk" gibi bir ad iki
        /// parcaya bolunuyor ve surum sutununda "atilmis" yaziyordu.
        ///
        /// Ad beklenen bicimde degilse ikisi de bos doner; kovaya elle birakilmis
        /// bir dosya listeyi bozmamali.
        /// </summary>
        public static (string? CalistirmaNo, string? CommitKisa) AdiCozumle(string dosyaAdi)
        {
            if (string.IsNullOrWhiteSpace(dosyaAdi))
                return (null, null);

            var govde = dosyaAdi.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)
                ? dosyaAdi[..^4]
                : dosyaAdi;

            var parcalar = govde.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parcalar.Length < 2)
                return (null, null);

            var calistirma = parcalar[^2];
            var commit = parcalar[^1];

            if (!RakamlardanMi(calistirma) || !OnaltilikMi(commit))
                return (null, null);

            return (calistirma, commit);
        }

        private static bool RakamlardanMi(string deger) =>
            deger.Length > 0 && deger.All(char.IsAsciiDigit);

        private static bool OnaltilikMi(string deger) =>
            deger.Length is >= 7 and <= 40 && deger.All(char.IsAsciiHexDigit);
    }

    /// <summary>
    /// Mobil hattin urettigi test APK'larini listeler.
    ///
    /// Yalnizca test ortaminda kullanilir; canlida ilgili uc hic map edilmez.
    /// </summary>
    public interface IApkKatalogu
    {
        Task<IReadOnlyList<ApkDosyasi>> SonlariGetirAsync(int adet, CancellationToken iptal = default);
    }
}
