using KuaforumAPI.Persistence.Converters;

namespace KuaforumAPI.UnitTests.Persistence;

/// <summary>
/// MediaUrlConverter davranis testleri.
///
/// Bu donusturucu canlidaki tum medyanin adresini uretiyor: bozulursa her
/// gorsel ve video kirilir. Ozellikle iki "saydamlik" ozelligi kritik —
/// medya saglayicisi gecisi bu ikisine dayanarak iki ayri adima bolunmustu
/// (once dagitim, sonra veri migrasyonu):
///
///   1. BaseUrl bossa donusturucu devre disi kalir      -> DagitimTekBasinaDavranisiDegistirmez
///   2. Mutlak adresler iki yonde de aynen gecer        -> MutlakAdresler*
///
/// Ikisi de o zaman elle dogrulanmisti; burada kalici hale getiriliyor.
/// </summary>
public class MediaUrlConverterTests
{
    private const string Cdn = "https://cdn.salonbir.com";

    private static Func<string, string> Okuma(string baseUrl) =>
        new MediaUrlConverter(baseUrl).ConvertFromProviderExpression.Compile();

    private static Func<string, string> Yazma(string baseUrl) =>
        new MediaUrlConverter(baseUrl).ConvertToProviderExpression.Compile();

    // ── Okuma: DB anahtari -> tam adres ─────────────────────────────────────

    [Theory]
    [InlineData("shops/covers/abc.jpg", "https://cdn.salonbir.com/shops/covers/abc.jpg")]
    [InlineData("shops/videos/klip.mov", "https://cdn.salonbir.com/shops/videos/klip.mov")]
    [InlineData("profile_images/x.png", "https://cdn.salonbir.com/profile_images/x.png")]
    public void AnahtarTamAdreseCevrilir(string anahtar, string beklenen) =>
        Assert.Equal(beklenen, Okuma(Cdn)(anahtar));

    // ── Yazma: tam adres -> DB anahtari ─────────────────────────────────────

    [Theory]
    [InlineData("https://cdn.salonbir.com/shops/covers/abc.jpg", "shops/covers/abc.jpg")]
    [InlineData("https://cdn.salonbir.com/ads/videos/tanitim.mp4", "ads/videos/tanitim.mp4")]
    public void TamAdresAnahtaraCevrilir(string adres, string beklenen) =>
        Assert.Equal(beklenen, Yazma(Cdn)(adres));

    [Fact]
    public void ZatenAnahtarOlanDegerYazarkenBozulmaz() =>
        Assert.Equal("shops/covers/abc.jpg", Yazma(Cdn)("shops/covers/abc.jpg"));

    // ── Guvenlik ozelligi 1: bos BaseUrl = devre disi ───────────────────────

    /// <summary>
    /// Backend, veri migrasyonundan ONCE canliya cikti. O sirada BaseUrl
    /// tanimliydi ama veritabani hala tam Cloudinary adresleri tutuyordu —
    /// ve hicbir sey bozulmadi. Bu testler o guvenceyi kalici kiliyor.
    /// </summary>
    [Theory]
    [InlineData("shops/covers/abc.jpg")]
    [InlineData("https://res.cloudinary.com/dk1pdqlrt/image/upload/v1/x.jpg")]
    [InlineData("")]
    public void DagitimTekBasinaDavranisiDegistirmez(string deger)
    {
        Assert.Equal(deger, Okuma("")(deger));
        Assert.Equal(deger, Yazma("")(deger));
    }

    // ── Guvenlik ozelligi 2: mutlak adresler dokunulmadan gecer ─────────────

    [Theory]
    [InlineData("https://res.cloudinary.com/dk1pdqlrt/image/upload/v1781949788/shops/covers/x.jpg")]
    [InlineData("http://eski-saglayici.example.com/gorsel.png")]
    [InlineData("https://baska-cdn.example.com/a/b/c.webp")]
    public void MutlakAdreslerOkurkenAynenGecer(string adres) =>
        Assert.Equal(adres, Okuma(Cdn)(adres));

    [Theory]
    [InlineData("https://res.cloudinary.com/dk1pdqlrt/image/upload/v1781949788/shops/covers/x.jpg")]
    [InlineData("http://eski-saglayici.example.com/gorsel.png")]
    public void MutlakAdreslerYazarkenAynenGecer(string adres) =>
        Assert.Equal(adres, Yazma(Cdn)(adres));

    // ── Egik cizgi ve buyuk/kucuk harf ──────────────────────────────────────

    [Theory]
    [InlineData("https://cdn.salonbir.com/", "shops/x.jpg")]
    [InlineData("https://cdn.salonbir.com", "/shops/x.jpg")]
    [InlineData("https://cdn.salonbir.com/", "/shops/x.jpg")]
    public void EgikCizgiCiftlenmez(string baseUrl, string anahtar) =>
        Assert.Equal("https://cdn.salonbir.com/shops/x.jpg", Okuma(baseUrl)(anahtar));

    [Fact]
    public void TabanAdresiBuyukKucukHarfDuyarsizEslesir() =>
        Assert.Equal("shops/x.jpg", Yazma(Cdn)("HTTPS://CDN.SALONBIR.COM/shops/x.jpg"));

    // ── Bos deger ───────────────────────────────────────────────────────────

    [Fact]
    public void BosDegerIkiYondeDeBosKalir()
    {
        Assert.Equal(string.Empty, Okuma(Cdn)(string.Empty));
        Assert.Equal(string.Empty, Yazma(Cdn)(string.Empty));
    }

    [Fact]
    public void NullDegerKorunur()
    {
        var donusturucu = new MediaUrlConverter(Cdn);
        Assert.Null(donusturucu.ConvertFromProvider(null));
        Assert.Null(donusturucu.ConvertToProvider(null));
    }

    // ── Gidis-donus butunlugu ───────────────────────────────────────────────

    [Theory]
    [InlineData("shops/covers/fjfkolaskerf2jxfsa1f.jpg")]
    [InlineData("shops/videos/IMG_8863_c3rmud.mov")]
    [InlineData("ads/images/reklam.png")]
    [InlineData("profile_images/kullanici.webp")]
    public void AnahtarGidipDonduğundeAyniKalir(string anahtar)
    {
        var tamAdres = Okuma(Cdn)(anahtar);
        Assert.Equal(anahtar, Yazma(Cdn)(tamAdres));
    }

    // ── Saglayici degisimi ──────────────────────────────────────────────────

    /// <summary>
    /// Bu donusturucunun varlik sebebi: saglayici degisimi tek ayar satiri
    /// olsun. Ayni anahtar, farkli tabanla farkli adres uretmeli.
    /// </summary>
    [Fact]
    public void TabanDegisinceAyniAnahtarYeniSaglayiciyaIsaretEder()
    {
        const string anahtar = "shops/covers/abc.jpg";

        Assert.Equal("https://cdn.salonbir.com/shops/covers/abc.jpg", Okuma(Cdn)(anahtar));
        Assert.Equal("https://medya.baskabiralan.com/shops/covers/abc.jpg",
            Okuma("https://medya.baskabiralan.com")(anahtar));
    }
}
