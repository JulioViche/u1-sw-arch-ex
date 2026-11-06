using Comercializadora.Dominio;
using Xunit;

namespace Comercializadora.Pruebas;

public class ReglasImagenCelularTests
{
    [Fact]
    public void AceptaFirmasJpegPngYWebp()
    {
        Assert.Equal("image/jpeg", ReglasImagenCelular.Validar(new byte[] { 0xff, 0xd8, 0xff }, "image/jpeg"));
        Assert.Equal("image/png", ReglasImagenCelular.Validar(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, "image/png"));
        Assert.Equal("image/webp", ReglasImagenCelular.Validar("RIFF0000WEBP"u8, "image/webp"));
    }

    [Fact]
    public void RechazaTipoOSignaturaFalsa() =>
        Assert.Throws<Celulares.Contratos.ErrorNegocio>(() => ReglasImagenCelular.Validar("<svg/>"u8, "image/svg+xml"));

    [Fact]
    public void LimitaTamanoA5Mb() =>
        Assert.Throws<Celulares.Contratos.ErrorNegocio>(() =>
            ReglasImagenCelular.Validar(new byte[ReglasImagenCelular.MaximoBytes + 1], "image/png"));
}
