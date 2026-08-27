using KuaforumAPI.Application.Interfaces.Services;

namespace KuaforumAPI.UnitTests.Application;

/// <summary>
/// Test APK dosya adindan surum bilgisi cikarma.
///
/// Ad, mobil deponun surekli tumlestirme hatti tarafindan uretiliyor
/// (salonbir-test-&lt;calistirmaNo&gt;-&lt;commit&gt;.apk). Iki taraf ayri
/// depolarda oldugu icin bicim sozlesmesi burada sabitleniyor: hat adlandirmayi
/// degistirirse bu testler duser.
/// </summary>
public class ApkDosyasiTests
{
    [Fact]
    public void BeklenenBicimdenCalistirmaVeCommitCikarir()
    {
        var (calistirma, commit) = ApkDosyasi.AdiCozumle("salonbir-test-1234-a1b2c3d.apk");

        Assert.Equal("1234", calistirma);
        Assert.Equal("a1b2c3d", commit);
    }

    [Fact]
    public void UzantisizAdiDaCozumler()
    {
        var (calistirma, commit) = ApkDosyasi.AdiCozumle("salonbir-test-99-abcdef1");

        Assert.Equal("99", calistirma);
        Assert.Equal("abcdef1", commit);
    }

    [Fact]
    public void BuyukHarfliUzantiyiTanir()
    {
        var (_, commit) = ApkDosyasi.AdiCozumle("salonbir-test-1-aaa1111.APK");

        Assert.Equal("aaa1111", commit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tekparca.apk")]
    // Iki parcaya bolunuyor ama ikisi de bicime uymuyor. Yalnizca parca
    // sayisina bakan bir cozumleyici burada "atilmis" yazardi.
    [InlineData("elle-atilmis.apk")]
    [InlineData("surum-1234.apk")]        // commit onaltilik degil
    [InlineData("salonbir-test-abc-a1b2c3d.apk")]  // calistirma rakam degil
    [InlineData("salonbir-test-12-xyz.apk")]       // commit hem kisa hem onaltilik degil
    [InlineData("salonbir-test-12-a1b2c3.apk")]    // commit 7 karakterden kisa
    public void BeklenmeyenAdlarSessizceBosDoner(string ad)
    {
        // Kovaya elle birakilmis bir dosya listeyi bozmamali; bilinmeyen ad
        // yalnizca surum sutununu bos birakir.
        var (calistirma, commit) = ApkDosyasi.AdiCozumle(ad);

        Assert.Null(calistirma);
        Assert.Null(commit);
    }

    [Fact]
    public void UzunCommitKarmasiniDaKabulEder()
    {
        // Hat kisa karma yaziyor ama tam karma da gecerli sayilmali.
        var (_, commit) = ApkDosyasi.AdiCozumle("salonbir-test-5-0123456789abcdef0123456789abcdef01234567.apk");

        Assert.Equal("0123456789abcdef0123456789abcdef01234567", commit);
    }

    [Fact]
    public void FazladanTireOlsaBileSondanIkiParcayiAlir()
    {
        // Dal adi vb. eklenirse bicim yine de son iki parcadan okunmali.
        var (calistirma, commit) = ApkDosyasi.AdiCozumle("salonbir-test-ozellik-dal-777-bbb2222.apk");

        Assert.Equal("777", calistirma);
        Assert.Equal("bbb2222", commit);
    }
}
