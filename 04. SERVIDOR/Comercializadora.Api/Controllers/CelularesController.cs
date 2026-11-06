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
    public Task<IReadOnlyList<Celular>> Listar(CancellationToken ct) => servicio.Listar(ct);

    [HttpGet("{id:int}")]
    public Task<Celular> Obtener(int id, CancellationToken ct) => servicio.Obtener(id, ct);

    [HttpPost]
    public async Task<ActionResult<Celular>> Crear(GuardarCelular solicitud, CancellationToken ct)
    {
        var celular = await servicio.Crear(solicitud, ct);
        return CreatedAtAction(nameof(Obtener), new { id = celular.Id }, celular);
    }

    [HttpPut("{id:int}")]
    public Task<Celular> Actualizar(int id, GuardarCelular solicitud, CancellationToken ct) =>
        servicio.Actualizar(id, solicitud, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await servicio.Eliminar(id, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/imagen")]
    public async Task<IActionResult> Imagen(int id, CancellationToken ct)
    {
        var imagen = await servicio.ObtenerImagen(id, ct);
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(imagen.Datos, imagen.Tipo);
    }

    [HttpPut("{id:int}/imagen"), RequestSizeLimit(ReglasImagenCelular.MaximoBytes + 64 * 1024)]
    public async Task<IActionResult> GuardarImagen(int id, [FromForm] IFormFile? archivo, CancellationToken ct)
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

    [HttpDelete("{id:int}/imagen")]
    public async Task<IActionResult> QuitarImagen(int id, CancellationToken ct)
    {
        await servicio.QuitarImagen(id, ct);
        return NoContent();
    }
}
