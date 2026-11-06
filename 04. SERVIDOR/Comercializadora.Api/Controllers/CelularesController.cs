using Celulares.Contratos;
using Comercializadora.Api.Servicios;
using Comercializadora.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace Comercializadora.Api.Controllers;

[ApiController]
[Route("api/celulares")]
public class CelularesController(ServicioCatalogo servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Celular>>> Listar(CancellationToken ct)
    {
        try { return Ok(await servicio.Listar(ct)); }
        catch (ErrorNegocio error) { return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message }); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Celular>> Obtener(int id, CancellationToken ct)
    {
        try { return Ok(await servicio.Obtener(id, ct)); }
        catch (ErrorNegocio error) { return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message }); }
    }

    [HttpPost]
    public async Task<ActionResult<Celular>> Crear(GuardarCelular solicitud, CancellationToken ct)
    {
        try
        {
            var celular = await servicio.Crear(solicitud, ct);
            return CreatedAtAction(nameof(Obtener), new { id = celular.Id }, celular);
        }
        catch (ErrorNegocio error) { return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message }); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Celular>> Actualizar(int id, GuardarCelular solicitud, CancellationToken ct)
    {
        try { return Ok(await servicio.Actualizar(id, solicitud, ct)); }
        catch (ErrorNegocio error) { return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        try
        {
            await servicio.Eliminar(id, ct);
            return NoContent();
        }
        catch (ErrorNegocio error) { return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message }); }
    }

    [HttpGet("{id:int}/imagen")]
    public async Task<IActionResult> Imagen(int id, CancellationToken ct)
    {
        try
        {
            var imagen = await servicio.ObtenerImagen(id, ct);
            Response.Headers.XContentTypeOptions = "nosniff";
            Response.Headers.CacheControl = "no-store";
            return File(imagen.Datos, imagen.Tipo);
        }
        catch (ErrorNegocio error) { return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message }); }
    }

    [HttpPut("{id:int}/imagen"), RequestSizeLimit(ReglasImagenCelular.MaximoBytes + 64 * 1024)]
    public async Task<IActionResult> GuardarImagen(int id, [FromForm] IFormFile? archivo, CancellationToken ct)
    {
        try
        {
            if (archivo is null) throw new ErrorNegocio("Seleccione una imagen.", 400);
            if (archivo.Length is 0 or > ReglasImagenCelular.MaximoBytes)
                throw new ErrorNegocio("La imagen debe pesar entre 1 byte y 5 MB.", 400);
            await using var flujo = new MemoryStream((int)archivo.Length);
            await archivo.CopyToAsync(flujo, ct);
            var datos = flujo.ToArray();
            var tipo = ReglasImagenCelular.Validar(datos, archivo.ContentType);
            await servicio.GuardarImagen(id, datos, tipo, ct);
            return NoContent();
        }
        catch (ErrorNegocio error) { return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message }); }
    }

    [HttpDelete("{id:int}/imagen")]
    public async Task<IActionResult> QuitarImagen(int id, CancellationToken ct)
    {
        try
        {
            await servicio.QuitarImagen(id, ct);
            return NoContent();
        }
        catch (ErrorNegocio error) { return StatusCode(error.Estado, new ProblemDetails { Status = error.Estado, Title = error.Message }); }
    }
}
