using Celulares.Contratos;

namespace Comercializadora.Dominio;

public static class ReglasFacturacion
{
    public static void ValidarPrecioEsperado(decimal precioActual, decimal? precioEsperado)
    {
        if (precioEsperado is null) return;
        if (precioEsperado <= 0m || precioEsperado > 99999999.99m ||
            precioEsperado != decimal.Round(precioEsperado.Value, 2, MidpointRounding.AwayFromZero))
            throw new ErrorNegocio("El precio seleccionado debe ser positivo y admitir hasta dos decimales.", 400);
        if (precioActual != precioEsperado)
            throw new ErrorNegocio("El precio del celular cambió. Actualice el catálogo antes de confirmar la venta.", 409);
    }

    public static decimal TotalEfectivo(decimal precio)
    {
        if (precio <= 0m) throw new ErrorNegocio("El precio debe ser positivo.", 400);
        return decimal.Round(precio * 0.58m, 2, MidpointRounding.AwayFromZero);
    }
}
