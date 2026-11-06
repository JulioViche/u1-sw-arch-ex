using System.Net.Http;
using Celulares.Cliente.Controladores;
using Celulares.Cliente.Servicios;
using Celulares.Contratos;

namespace Celulares.Escritorio.Controladores;

public sealed class ControladorEscritorio
{
    private readonly ControladorTienda tienda;
    private SolicitudFactura? pendiente;

    public bool TieneVentaPendiente => pendiente is not null;

    public ControladorEscritorio(HttpClient http) =>
        tienda = new ControladorTienda(new ClienteComercializadora(http));

    public Task<Celular[]> Catalogo() => tienda.Catalogo();

    public Task<Celular> Guardar(int? id, string nombre, decimal precio) =>
        tienda.Guardar(id, nombre, precio);

    public Task Eliminar(int id) => tienda.Eliminar(id);

    public async Task<Factura> RegistrarVenta(SolicitudFactura solicitud)
    {
        if (pendiente is not null && !Igual(pendiente, solicitud))
            throw new InvalidOperationException("Hay una venta pendiente. Reintente con los mismos datos o inicie una nueva venta.");

        pendiente ??= solicitud;
        var factura = await tienda.Facturar(pendiente);
        pendiente = null;
        return factura;
    }

    public void NuevaVenta() => pendiente = null;

    public Task<Factura> Factura(int id) => tienda.Factura(id);

    public Task<Cuota[]> Amortizacion(int id) => tienda.Amortizacion(id);

    private static bool Igual(SolicitudFactura izquierda, SolicitudFactura derecha) =>
        izquierda.Cedula == derecha.Cedula &&
        izquierda.NombreCliente == derecha.NombreCliente &&
        izquierda.IdCelular == derecha.IdCelular &&
        izquierda.FormaPago == derecha.FormaPago &&
        izquierda.NumeroCuotas == derecha.NumeroCuotas;
}
