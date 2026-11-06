using Celulares.Cliente.Controladores;
using Xunit;

namespace Comercializadora.Pruebas;

public class PrecioClienteTests
{
    [Theory]
    [InlineData("100.50", 100.50)]
    [InlineData("100,50", 100.50)]
    public void AceptaAmbosSeparadoresDecimales(string entrada, double esperado) =>
        Assert.Equal((decimal)esperado, ControladorTienda.InterpretarPrecio(entrada));

    [Theory]
    [InlineData("1.234,50")]
    [InlineData("1.234")]
    [InlineData("0")]
    public void RechazaFormatoAmbiguoOCantidadInvalida(string entrada) =>
        Assert.Throws<ArgumentException>(() => ControladorTienda.InterpretarPrecio(entrada));
}
