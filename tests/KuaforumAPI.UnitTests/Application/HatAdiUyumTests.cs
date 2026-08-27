using KuaforumAPI.Application.Interfaces.Services;

namespace KuaforumAPI.UnitTests.Application;

/// <summary>
/// Mobil hattin uretecegi gercek ad bicimi.
///
/// Ad iki ayri depoda uretiliyor ve cozumleniyor; sozlesmenin kirilmasi
/// sessiz olurdu (surum sutunu bosalir, kimse fark etmez).
/// </summary>
public class HatAdiUyumTests
{
    [Fact]
    public void HattinUrettigiAdCozumlenebiliyor()
    {
        // .github/workflows/derleme.yml icindeki bicimin birebir kopyasi:
        //   salonbir-test-<run_number>-<sha ilk 7>.apk
        var (calistirma, commit) = ApkDosyasi.AdiCozumle("salonbir-test-1234-3ba9614.apk");

        Assert.Equal("1234", calistirma);
        Assert.Equal("3ba9614", commit);
    }
}
