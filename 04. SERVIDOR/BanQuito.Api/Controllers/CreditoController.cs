using BanQuito.Api.Servicios;
using BanQuito.Contratos;
using BanQuito.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace BanQuito.Api.Controllers;

[ApiController]
public class CreditoController(ServicioCredito servicio) : ControllerBase
{
    [HttpGet("api/credito/elegibilidad")]
    public async Task<ActionResult<EvaluacionCredito>> Elegibilidad([FromQuery] string cedula, CancellationToken ct)
    {
        try
        {
            return Ok(await servicio.Evaluar(cedula.Trim(), ct));
        }
        catch (ErrorNegocio error)
        {
            return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message });
        }
    }

    [HttpGet("api/credito/clientes/{codigoCliente:int}/monto-maximo")]
    public async Task<ActionResult<object>> MontoMaximo(int codigoCliente, CancellationToken ct)
    {
        try
        {
            return Ok(new { codigoCliente, montoMaximo = await servicio.MontoMaximo(codigoCliente, ct) });
        }
        catch (ErrorNegocio error)
        {
            return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message });
        }
    }

    [HttpPost("api/creditos")]
    public async Task<ActionResult<Credito>> Otorgar(SolicitudCredito solicitud, CancellationToken ct)
    {
        try
        {
            return Ok(await servicio.Otorgar(solicitud, ct));
        }
        catch (ErrorNegocio error)
        {
            return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message });
        }
    }

    [HttpGet("api/creditos/{id:int}")]
    public async Task<ActionResult<Credito>> Obtener(int id, CancellationToken ct)
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

    [HttpGet("api/creditos/{id:int}/amortizacion")]
    public async Task<ActionResult<IReadOnlyList<Cuota>>> Amortizacion(int id, CancellationToken ct)
    {
        try
        {
            return Ok((await servicio.Obtener(id, ct)).Cuotas);
        }
        catch (ErrorNegocio error)
        {
            return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message });
        }
    }
}
