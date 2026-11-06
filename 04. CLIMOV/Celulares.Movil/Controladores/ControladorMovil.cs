using Celulares.Cliente.Controladores;
using Celulares.Cliente.Servicios;
using Celulares.Contratos;

namespace Celulares.Movil.Controladores;

public sealed class ControladorMovil
{
    private ControladorTienda? tienda;
    private SolicitudFactura? pendiente;

    public Uri? Direccion { get; private set; }
    public bool TieneVentaPendiente => pendiente is not null;

    public void Conectar(Uri direccion)
    {
        tienda = new ControladorTienda(new ClienteComercializadora(
            new HttpClient { BaseAddress = direccion, Timeout = TimeSpan.FromSeconds(30) }));
        Direccion = direccion;
    }

    public Task<Celular[]> Catalogo() => Tienda().Catalogo();

    public Task<Celular> Guardar(int? id, string nombre, decimal precio) =>
        Tienda().Guardar(id, nombre, precio);

    public Task Eliminar(int id) => Tienda().Eliminar(id);

    public async Task<Factura> RegistrarVenta(SolicitudFactura solicitud)
    {
        if (pendiente is not null && !Igual(pendiente, solicitud))
            throw new InvalidOperationException("Hay una venta pendiente. Reintente con los mismos datos o inicie una nueva venta.");

        pendiente ??= solicitud;
        var factura = await Tienda().Facturar(pendiente);
        pendiente = null;
        return factura;
    }

    public void NuevaVenta() => pendiente = null;

    public Task<Factura> Factura(int id) => Tienda().Factura(id);

    public Task<Cuota[]> Amortizacion(int id) => Tienda().Amortizacion(id);

    private ControladorTienda Tienda() =>
        tienda ?? throw new InvalidOperationException("Conecte primero con la comercializadora.");

    private static bool Igual(SolicitudFactura izquierda, SolicitudFactura derecha) =>
        izquierda.Cedula == derecha.Cedula &&
        izquierda.NombreCliente == derecha.NombreCliente &&
        izquierda.IdCelular == derecha.IdCelular &&
        izquierda.FormaPago == derecha.FormaPago &&
        izquierda.NumeroCuotas == derecha.NumeroCuotas;
}
