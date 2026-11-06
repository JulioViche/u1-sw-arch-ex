using Comercializadora.Dominio;
using Celulares.Cliente.Controladores;
using Xunit;

namespace Comercializadora.Pruebas;

public class ReglasFacturacionTests
{
    [Fact]
    public void EfectivoDescuenta42PorCiento() =>
        Assert.Equal(58m, ReglasFacturacion.TotalEfectivo(100m));

    [Fact]
    public void VistaPreviaEfectivoCoincideConElServidor() =>
        Assert.Equal(ReglasFacturacion.TotalEfectivo(101.99m), ControladorTienda.TotalEfectivo(101.99m));
}
