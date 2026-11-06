using Celulares.Contratos;
using Comercializadora.Api.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Comercializadora.Api.Controllers;

[ApiController]
[Route("api/facturas")]
public class FacturasController(ServicioFacturas servicio) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<Factura>> ListarPorCedula([FromQuery] string? cedula, CancellationToken ct) =>
        servicio.ListarPorCedula(cedula, ct);

    [HttpGet("{id:int}")]
    public Task<Factura> Obtener(int id, CancellationToken ct) => servicio.Obtener(id, ct);

    [HttpPost]
    public async Task<ActionResult<Factura>> Crear(SolicitudFactura solicitud, CancellationToken ct)
    {
        var factura = await servicio.Crear(solicitud, ct);
        return CreatedAtAction(nameof(Obtener), new { id = factura.Id }, factura);
    }

    [HttpGet("{id:int}/amortizacion")]
    public Task<IReadOnlyList<Cuota>> Amortizacion(int id, CancellationToken ct) =>
        servicio.Amortizacion(id, ct);
}
