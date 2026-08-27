using Amazon.S3.Model;
using KuaforumAPI.Infrastructure.Services;

namespace KuaforumAPI.UnitTests.Infrastructure;

/// <summary>
/// S3 liste yanitinin gosterilecek kayitlara cevrilmesi.
/// </summary>
public class R2ApkKataloguTests
{
    private const string TabanUrl = "https://cdn.salonbir.com";

    private static S3Object Nesne(string anahtar, DateTime tarih, long boyut = 1024)
        => new() { Key = anahtar, LastModified = tarih, Size = boyut };

    [Fact]
    public void NesneListesiNullIseBosDoner()
    {
        // R2 kova bos oldugunda bos liste degil NULL donuyor. Dogrudan LINQ
        // uygulamak "Value cannot be null (Parameter 'source')" ile 500
        // uretiyordu; ilk gercek istekte bu yasandi.
        var sonuc = R2ApkKatalogu.SiralayipCevir(null, 20, TabanUrl);

        Assert.Empty(sonuc);
    }

    [Fact]
    public void BosListeBosDoner()
    {
        Assert.Empty(R2ApkKatalogu.SiralayipCevir([], 20, TabanUrl));
    }

    [Fact]
    public void ApkOlmayanDosyalariEler()
    {
        var nesneler = new List<S3Object>
        {
            Nesne("apk/", new DateTime(2026, 8, 27)),
            Nesne("apk/notlar.txt", new DateTime(2026, 8, 27)),
            Nesne("apk/salonbir-test-1-a1b2c3d.apk", new DateTime(2026, 8, 27)),
        };

        var sonuc = R2ApkKatalogu.SiralayipCevir(nesneler, 20, TabanUrl);

        Assert.Single(sonuc);
        Assert.Equal("salonbir-test-1-a1b2c3d.apk", sonuc[0].DosyaAdi);
    }

    [Fact]
    public void EnYeniUsttedir()
    {
        // Liste S3'ten ada gore geliyor; en yeniyi bulmak icin kendimiz
        // siralamamiz gerekiyor.
        var nesneler = new List<S3Object>
        {
            Nesne("apk/salonbir-test-1-aaaaaaa.apk", new DateTime(2026, 8, 1)),
            Nesne("apk/salonbir-test-9-ccccccc.apk", new DateTime(2026, 8, 27)),
            Nesne("apk/salonbir-test-5-bbbbbbb.apk", new DateTime(2026, 8, 15)),
        };

        var sonuc = R2ApkKatalogu.SiralayipCevir(nesneler, 20, TabanUrl);

        Assert.Equal(["9", "5", "1"], sonuc.Select(d => d.CalistirmaNo));
    }

    [Fact]
    public void AdetSinirinaUyar()
    {
        var nesneler = Enumerable.Range(1, 30)
            .Select(i => Nesne($"apk/salonbir-test-{i}-a1b2c3d.apk", new DateTime(2026, 8, 1).AddDays(i)))
            .ToList();

        Assert.Equal(5, R2ApkKatalogu.SiralayipCevir(nesneler, 5, TabanUrl).Count);
    }

    [Fact]
    public void IndirmeUrliTabanAdresleBirlestirir()
    {
        var nesneler = new List<S3Object> { Nesne("apk/salonbir-test-1-a1b2c3d.apk", new DateTime(2026, 8, 27)) };

        var sonuc = R2ApkKatalogu.SiralayipCevir(nesneler, 20, TabanUrl);

        Assert.Equal("https://cdn.salonbir.com/apk/salonbir-test-1-a1b2c3d.apk", sonuc[0].IndirmeUrl);
    }

    [Fact]
    public void TarihiOlmayanNesneListeyiDusurmez()
    {
        var nesneler = new List<S3Object>
        {
            new() { Key = "apk/salonbir-test-1-a1b2c3d.apk", LastModified = null, Size = null },
        };

        var sonuc = R2ApkKatalogu.SiralayipCevir(nesneler, 20, TabanUrl);

        Assert.Single(sonuc);
        Assert.Equal(0, sonuc[0].Boyut);
    }
}
