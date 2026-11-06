using BanQuito.Contratos;
namespace BanQuito.Dominio;
public static class ReglasCredito
{
    public static EvaluacionCredito Evaluar(ClienteBanco? cliente, IReadOnlyList<Movimiento> movimientos, bool creditoActivo, DateOnly hoy)
    {
        if (cliente is null) return new(false, null, "", "El solicitante no es cliente del banco.", 0m);
        var edad = hoy.Year - cliente.FechaNacimiento.Year;
        if (cliente.FechaNacimiento.AddYears(edad) > hoy) edad--;
        var motivo = string.Equals(cliente.Genero, "M", StringComparison.OrdinalIgnoreCase) && edad < 25 ? "El cliente masculino debe tener 25 años cumplidos."
            : !movimientos.Any(m => string.Equals(m.Tipo, "DEP", StringComparison.OrdinalIgnoreCase) && m.Fecha >= hoy.AddMonths(-1) && m.Fecha <= hoy)
                ? "No hay depósitos en el último mes."
            : creditoActivo ? "El cliente ya tiene un crédito activo." : "";
        return new(motivo.Length == 0, cliente.CodigoCliente, cliente.Nombre,
            motivo.Length == 0 ? "El cliente es sujeto de crédito." : motivo, MontoMaximo(movimientos, hoy));
    }

    public static decimal MontoMaximo(IReadOnlyList<Movimiento> movimientos, DateOnly hoy)
    {
        var recientes = movimientos.Where(m => m.Fecha >= hoy.AddMonths(-3) && m.Fecha <= hoy).ToArray();
        var depositos = recientes.Where(m => string.Equals(m.Tipo, "DEP", StringComparison.OrdinalIgnoreCase)).Select(m => m.Valor).DefaultIfEmpty(0m).Average();
        var retiros = recientes.Where(m => string.Equals(m.Tipo, "RET", StringComparison.OrdinalIgnoreCase)).Select(m => m.Valor).DefaultIfEmpty(0m).Average();
        return Dinero(Math.Max(0m, (depositos - retiros) * 0.35m * 6m));
    }

    public static IReadOnlyList<Cuota> Amortizar(decimal monto, int meses, DateOnly fecha)
    {
        if (monto < 0.01m || monto > 99999999.99m) throw new ErrorNegocio("El monto debe ser positivo y caber en NUMERIC(10,2).", 400);
        if (meses < 3 || meses > 18) throw new ErrorNegocio("El plazo debe estar entre 3 y 18 meses.", 400);
        monto = Dinero(monto);
        const decimal tasaMensual = 0.165m / 12m;
        var factor = 1m;
        for (var i = 0; i < meses; i++) factor *= 1m + tasaMensual;
        var pago = Dinero(monto * tasaMensual * factor / (factor - 1m));
        var saldo = monto;
        var tabla = new List<Cuota> { new(0, fecha, 0m, 0m, 0m, saldo) };
        for (var i = 1; i <= meses; i++)
        {
            var interes = Dinero(saldo * tasaMensual);
            // La última cuota absorbe centavos de redondeo para cancelar todo el capital.
            var capital = i == meses ? saldo : Math.Min(saldo, Math.Max(0m, pago - interes));
            var valor = capital + interes;
            saldo = Dinero(saldo - capital);
            tabla.Add(new(i, fecha.AddMonths(i), valor, interes, capital, saldo));
        }
        return tabla;
    }

    public static decimal Dinero(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
}

