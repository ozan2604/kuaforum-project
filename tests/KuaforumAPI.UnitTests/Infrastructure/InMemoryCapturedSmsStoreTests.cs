using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Infrastructure.Services;

namespace KuaforumAPI.UnitTests.Infrastructure;

public class InMemoryCapturedSmsStoreTests
{
    private static CapturedSms Kayit(string tel, string kod) =>
        new(tel, $"Kodunuz: {kod}", kod, DateTime.UtcNow);

    [Fact]
    public void EnYeniKayitBastaGelir()
    {
        var depo = new InMemoryCapturedSmsStore();

        depo.Add(Kayit("05551112233", "111111"));
        depo.Add(Kayit("05551112233", "222222"));

        Assert.Equal("222222", depo.Recent()[0].Code);
    }

    [Fact]
    public void DepoSinirsizBuyumez()
    {
        var depo = new InMemoryCapturedSmsStore();

        for (var i = 0; i < 200; i++)
            depo.Add(Kayit("05551112233", $"{i:D6}"));

        // Kapasite 50; en yeni kayit korunmali, eskiler dusmeli.
        Assert.Equal(50, depo.Recent(100).Count);
        Assert.Equal("000199", depo.Recent()[0].Code);
    }

    /// <summary>
    /// Numara veritabaninda "05551112233", istekte "5551112233", NetGSM
    /// tarafinda "905551112233" olabiliyor. Testerin hangi bicimi yazdigi
    /// onemli olmamali.
    /// </summary>
    [Theory]
    [InlineData("05551112233")]
    [InlineData("5551112233")]
    [InlineData("905551112233")]
    [InlineData("+90 555 111 22 33")]
    public void FarkliNumaraBicimleriAyniKaydaDenkGelir(string arama)
    {
        var depo = new InMemoryCapturedSmsStore();
        depo.Add(Kayit("05551112233", "654321"));

        var bulunan = depo.LastFor(arama);

        Assert.NotNull(bulunan);
        Assert.Equal("654321", bulunan.Code);
    }

    [Fact]
    public void BilinmeyenNumaraIcinNullDoner() =>
        Assert.Null(new InMemoryCapturedSmsStore().LastFor("05559998877"));

    [Fact]
    public void NumaraBazindaEnYeniKodDoner()
    {
        var depo = new InMemoryCapturedSmsStore();

        depo.Add(Kayit("05551112233", "111111"));
        depo.Add(Kayit("05554445566", "999999"));
        depo.Add(Kayit("05551112233", "222222"));

        Assert.Equal("222222", depo.LastFor("05551112233")!.Code);
        Assert.Equal("999999", depo.LastFor("05554445566")!.Code);
    }
}
