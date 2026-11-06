using BanQuito.Dominio;
using BanQuito.Contratos;
using Xunit;

namespace BanQuito.Pruebas;

public class ReglasCreditoTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 23);
    private static ClienteBanco Cliente(string genero = "M", int edad = 25) =>
        new(1, "0102030405", "Cliente de prueba", genero, Hoy.AddYears(-edad));
    private static readonly Movimiento[] Movimientos = [
        new("DEP", 1000m, Hoy.AddDays(-2)), new("DEP", 500m, Hoy.AddMonths(-3)),
        new("RET", 100m, Hoy.AddDays(-4)), new("RET", 200m, Hoy.AddMonths(-2)),
        new("DEP", 99999m, Hoy.AddMonths(-3).AddDays(-1)), new("DEP", 99999m, Hoy.AddDays(1))
    ];

    [Fact] public void MaximoUsa35PorCientoYPromediosPorMovimiento() =>
        Assert.Equal(1260m, ReglasCredito.MontoMaximo(Movimientos, Hoy));
    [Fact] public void SinRetirosUsaPromedioCero() =>
        Assert.Equal(2100m, ReglasCredito.MontoMaximo([new("DEP", 1000m, Hoy)], Hoy));
    [Fact] public void DiferenciaNegativaNoProduceCreditoNegativo() =>
        Assert.Equal(0m, ReglasCredito.MontoMaximo([new("DEP", 10m, Hoy), new("RET", 100m, Hoy)], Hoy));
    [Fact] public void ClienteInexistenteNoCalifica() =>
        Assert.False(ReglasCredito.Evaluar(null, Movimientos, false, Hoy).SujetoCredito);
    [Fact] public void Cumple25HoyCalifica() =>
        Assert.True(ReglasCredito.Evaluar(Cliente(), Movimientos, false, Hoy).SujetoCredito);
    [Fact] public void Cumple25MananaNoCalifica() =>
        Assert.False(ReglasCredito.Evaluar(Cliente() with { FechaNacimiento = Hoy.AddYears(-25).AddDays(1) }, Movimientos, false, Hoy).SujetoCredito);
    [Fact] public void ReglaEdadMasculinaNoSeExtiendeAMujeres() =>
        Assert.True(ReglasCredito.Evaluar(Cliente("F", 23), Movimientos, false, Hoy).SujetoCredito);
    [Fact] public void CreditoActivoRechaza() =>
        Assert.False(ReglasCredito.Evaluar(Cliente(), Movimientos, true, Hoy).SujetoCredito);
    [Fact] public void DepositoEnLimiteMesCuenta() =>
        Assert.True(ReglasCredito.Evaluar(Cliente(), [new("DEP", 100m, Hoy.AddMonths(-1))], false, Hoy).SujetoCredito);
    [Fact] public void DepositoAntiguoNoCuenta() =>
        Assert.False(ReglasCredito.Evaluar(Cliente(), [new("DEP", 100m, Hoy.AddMonths(-1).AddDays(-1))], false, Hoy).SujetoCredito);
    [Fact] public void Ejemplo450En6Meses()
    {
        var tabla = ReglasCredito.Amortizar(450m, 6, Hoy);
        Assert.Equal(7, tabla.Count);
        Assert.Equal(450m, tabla[0].Saldo);
        Assert.Equal(78.65m, tabla[1].ValorCuota);
        Assert.Equal(6.19m, tabla[1].InteresPagado);
        Assert.Equal(0m, tabla[^1].Saldo);
        Assert.Equal(450m, tabla.Sum(c => c.CapitalPagado));
    }
    [Theory] [InlineData(3)] [InlineData(18)] public void PlazosValidosCierranSaldo(int meses)
    {
        var tabla = ReglasCredito.Amortizar(1500m, meses, Hoy);
        Assert.Equal(meses + 1, tabla.Count);
        Assert.Equal(0m, tabla[^1].Saldo);
        Assert.All(tabla, c => Assert.True(c.Saldo >= 0m));
    }
    [Theory] [InlineData(2)] [InlineData(19)] public void PlazosFueraRangoFallan(int meses) =>
        Assert.Throws<ErrorNegocio>(() => ReglasCredito.Amortizar(450m, meses, Hoy));
    [Theory] [InlineData(0)] [InlineData(-5)] public void MontoNoPositivoFalla(decimal monto) =>
        Assert.Throws<ErrorNegocio>(() => ReglasCredito.Amortizar(monto, 6, Hoy));
    [Fact] public void MasculinoMinusculaNoEvitaReglaDeEdad() =>
        Assert.False(ReglasCredito.Evaluar(Cliente("m", 24), Movimientos, false, Hoy).SujetoCredito);
    [Fact] public void CodigosMinusculaConservanSuSignificado()
    {
        Movimiento[] datos = [new("dep", 1000m, Hoy), new("ret", 100m, Hoy)];
        Assert.True(ReglasCredito.Evaluar(Cliente(), datos, false, Hoy).SujetoCredito);
        Assert.Equal(1890m, ReglasCredito.MontoMaximo(datos, Hoy));
    }
    [Fact] public void ValidacionDePrecioFuncionaConDecimalRegional()
    {
        var anterior = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new("es-EC");
            var solicitud = new SolicitudCredito { CodigoCliente = 1, PrecioTelefono = 450m, NumeroCuotas = 6 };
            var errores = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            Assert.True(System.ComponentModel.DataAnnotations.Validator.TryValidateObject(solicitud,
                new(solicitud), errores, true));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = anterior; }
    }
}

