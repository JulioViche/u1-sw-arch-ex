using Celulares.Cliente.Controladores;
using Celulares.Contratos;
using Celulares.Web.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Celulares.Web.Controllers;

public class TiendaController(ControladorTienda tienda) : Controller
{
    [HttpGet]
    public Task<IActionResult> Index() => Mostrar(new SolicitudFactura(), null);

    [HttpGet]
    public Task<IActionResult> Ventas([FromQuery] string? cedula) =>
        MostrarVentas(new SolicitudFactura(), cedula, null);

    private bool EsAjax() =>
        Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
        Request.Headers.Accept.ToString().Contains("application/json");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(int? id, string nombre, decimal precio)
    {
        try
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(nombre) || precio <= 0)
                throw new ArgumentException("Ingrese nombre y precio positivo.");
            var celular = await tienda.Guardar(id, nombre, precio);
            if (EsAjax())
                return Json(new { exito = true, mensaje = id is null ? "Celular guardado." : "Celular actualizado.", celular });
            TempData["Mensaje"] = id is null ? "Celular guardado." : "Celular actualizado.";
        }
        catch (Exception error) when (EsErrorEsperado(error))
        {
            if (EsAjax())
                return BadRequest(new { exito = false, error = Mensaje(error) });
            TempData["Error"] = Mensaje(error);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(5 * 1024 * 1024 + 64 * 1024)]
    public async Task<IActionResult> GuardarImagen(int id, IFormFile? archivo)
    {
        if (archivo is null || archivo.Length is 0 or > 5 * 1024 * 1024)
        {
            var msg = "Seleccione una imagen de hasta 5 MB.";
            if (EsAjax()) return BadRequest(new { exito = false, error = msg });
            TempData["Error"] = msg;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await using var flujo = archivo.OpenReadStream();
            await tienda.SubirImagen(id, flujo, Path.GetFileName(archivo.FileName), archivo.ContentType);
            if (EsAjax()) return Json(new { exito = true, mensaje = "Foto del celular guardada." });
            TempData["Mensaje"] = "Foto del celular guardada.";
        }
        catch (Exception error) when (EsErrorEsperado(error))
        {
            if (EsAjax()) return BadRequest(new { exito = false, error = Mensaje(error) });
            TempData["Error"] = Mensaje(error);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarImagen(int id)
    {
        try
        {
            await tienda.QuitarImagen(id);
            if (EsAjax()) return Json(new { exito = true, mensaje = "Foto del celular eliminada." });
            TempData["Mensaje"] = "Foto del celular eliminada.";
        }
        catch (Exception error) when (EsErrorEsperado(error))
        {
            if (EsAjax()) return BadRequest(new { exito = false, error = Mensaje(error) });
            TempData["Error"] = Mensaje(error);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await tienda.Eliminar(id);
            if (EsAjax()) return Json(new { exito = true, mensaje = "Celular desactivado." });
            TempData["Mensaje"] = "Celular desactivado.";
        }
        catch (Exception error) when (EsErrorEsperado(error))
        {
            if (EsAjax()) return BadRequest(new { exito = false, error = Mensaje(error) });
            TempData["Error"] = Mensaje(error);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Vender([Bind(Prefix = "Venta")] SolicitudFactura venta)
    {
        if (!ModelState.IsValid) return await MostrarVentas(venta, venta.Cedula, "Revise los datos de la venta.");
        try
        {
            var factura = await tienda.Facturar(venta);
            return RedirectToAction(nameof(Factura), new { id = factura.Id, volverCedula = venta.Cedula });
        }
        catch (Exception error) when (EsErrorEsperado(error))
        {
            var aviso = Mensaje(error);
            if (error is HttpRequestException or TaskCanceledException)
                aviso += " Si se cortó la conexión, reintente sin cambiar los datos.";
            return await MostrarVentas(venta, venta.Cedula, aviso);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Elegibilidad([FromQuery] string cedula)
    {
        try { return Json(await tienda.EvaluarCredito(cedula)); }
        catch (Exception error) when (EsErrorEsperado(error))
        {
            var estado = error is HttpRequestException or TaskCanceledException ? 503 : 400;
            return StatusCode(estado, new { mensaje = Mensaje(error) });
        }
    }

    [HttpGet, HttpPost]
    public IActionResult BuscarFacturas(string? cedula)
    {
        var busqueda = cedula?.Trim() ?? "";
        return RedirectToAction(nameof(Ventas), new { cedula = busqueda });
    }

    [HttpGet]
    public async Task<IActionResult> Factura(int id, [FromQuery] string? volverCedula)
    {
        try
        {
            var factura = await tienda.Factura(id);
            var cuotas = factura.FormaPago == "CREDITO_DIRECTO" && factura.Estado == "APROBADA"
                ? await tienda.Amortizacion(id) : [];
            return View(new PaginaFactura(factura, cuotas, volverCedula));
        }
        catch (Exception error) when (EsErrorEsperado(error))
        {
            TempData["Error"] = Mensaje(error);
            return RedirectToAction(nameof(Ventas), new { cedula = volverCedula });
        }
    }

    private async Task<IActionResult> Mostrar(SolicitudFactura venta, string? error)
    {
        Celular[] catalogo = [];
        try { catalogo = await tienda.Catalogo(); }
        catch (Exception e) when (EsErrorEsperado(e)) { error ??= Mensaje(e); }
        return View("Index", new PaginaTienda { Catalogo = catalogo, Venta = venta, Error = error });
    }

    private async Task<IActionResult> MostrarVentas(SolicitudFactura venta, string? cedula, string? error)
    {
        Celular[] catalogo = [];
        IReadOnlyList<Factura> facturas = [];
        var busqueda = cedula?.Trim();
        var realizada = false;
        try
        {
            catalogo = await tienda.Catalogo();
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                realizada = true;
                facturas = await tienda.FacturasPorCedula(busqueda);
            }
        }
        catch (Exception e) when (EsErrorEsperado(e)) { error ??= Mensaje(e); }

        return View("Ventas", new PaginaVentas
        {
            Catalogo = catalogo,
            Facturas = facturas,
            Venta = venta,
            CedulaBusqueda = busqueda,
            Error = error,
            BusquedaRealizada = realizada
        });
    }

    private static bool EsErrorEsperado(Exception error) =>
        error is InvalidOperationException or ArgumentException or HttpRequestException or TaskCanceledException or ErrorNegocio;

    private static string Mensaje(Exception error) => error is HttpRequestException or TaskCanceledException
        ? "No se pudo conectar con la comercializadora. Compruebe que el servidor esté iniciado."
        : error.Message;
}
