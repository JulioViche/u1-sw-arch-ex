using Celulares.Contratos;
using Comercializadora.Api.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Comercializadora.Api.Controllers;

[ApiController]
[Route("api/credito")]
public class CreditoController(ServicioBanQuito banco) : ControllerBase
{
    [HttpGet("elegibilidad")]
    public async Task<ActionResult<EvaluacionCredito>> Elegibilidad([FromQuery] string? cedula, CancellationToken ct)
    {
        try
        {
            var limpia = cedula?.Trim() ?? "";
            if (limpia.Length is < 1 or > 100)
                throw new ErrorNegocio("Ingrese una cédula de 1 a 100 caracteres.", 400);
            return Ok(await banco.Evaluar(limpia, ct));
        }
        catch (ErrorNegocio error)
        {
            return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message });
        }
    }
}
