using Celulares.Contratos;
using Comercializadora.Api.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Comercializadora.Api.Controllers;

[ApiController]
[Route("api/credito")]
public class CreditoController(ServicioBanQuito banco) : ControllerBase
{
    [HttpGet("elegibilidad")]
    public Task<EvaluacionCredito> Elegibilidad([FromQuery] string? cedula, CancellationToken ct)
    {
        var limpia = cedula?.Trim() ?? "";
        if (limpia.Length is < 1 or > 100)
            throw new ErrorNegocio("Ingrese una cédula de 1 a 100 caracteres.", 400);
        return banco.Evaluar(limpia, ct);
    }
}
