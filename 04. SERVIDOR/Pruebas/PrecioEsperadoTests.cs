using Celulares.Contratos;
using Comercializadora.Dominio;
using Xunit;

namespace Comercializadora.Pruebas;

public class PrecioEsperadoTests
{
    [Fact]
    public void PermitePrecioEsperadoCoincidente() =>
        ReglasFacturacion.ValidarPrecioEsperado(499.99m, 499.99m);

    [Fact]
    public void PermiteClienteAntiguoSinPrecioEsperado() =>
        ReglasFacturacion.ValidarPrecioEsperado(499.99m, null);

    [Fact]
    public void RechazaPrecioDeCatalogoDesactualizado()
    {
        var error = Assert.Throws<ErrorNegocio>(() =>
            ReglasFacturacion.ValidarPrecioEsperado(500m, 499.99m));

        Assert.Equal(409, error.Estado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.001)]
    public void RechazaPrecioEsperadoInvalido(decimal esperado) =>
        Assert.Equal(400, Assert.Throws<ErrorNegocio>(() =>
            ReglasFacturacion.ValidarPrecioEsperado(esperado, esperado)).Estado);
}
