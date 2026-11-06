using Celulares.Contratos;
using Comercializadora.Api.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Comercializadora.Api.Controllers;

[ApiController]
[Route("api/facturas")]
public class FacturasController(ServicioFacturas servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Factura>>> ListarPorCedula([FromQuery] string? cedula, CancellationToken ct)
    {
        try
        {
            return Ok(await servicio.ListarPorCedula(cedula, ct));
        }
        catch (ErrorNegocio error)
        {
            return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Factura>> Obtener(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await servicio.Obtener(id, ct));
        }
        catch (ErrorNegocio error)
        {
            return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Factura>> Crear(SolicitudFactura solicitud, CancellationToken ct)
    {
        try
        {
            var factura = await servicio.Crear(solicitud, ct);
            return CreatedAtAction(nameof(Obtener), new { id = factura.Id }, factura);
        }
        catch (ErrorNegocio error)
        {
            return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message });
        }
    }

    [HttpGet("{id:int}/amortizacion")]
    public async Task<ActionResult<IReadOnlyList<Cuota>>> Amortizacion(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await servicio.Amortizacion(id, ct));
        }
        catch (ErrorNegocio error)
        {
            return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message });
        }
    }
}
