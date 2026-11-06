using Celulares.Cliente.Controladores;
using Celulares.Contratos;
using Celulares.Web.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Celulares.Web.Controllers;

public class TiendaController(ControladorTienda tienda) : Controller
{
    [HttpGet]
    public Task<IActionResult> Index() => Mostrar(new SolicitudFactura(), null);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(int? id, string nombre, decimal precio)
    {
        try
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(nombre) || precio <= 0)
                throw new ArgumentException("Ingrese nombre y precio positivo.");
            await tienda.Guardar(id, nombre, precio);
            TempData["Mensaje"] = "Celular guardado.";
        }
        catch (Exception error) when (EsErrorEsperado(error)) { TempData["Error"] = Mensaje(error); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(5 * 1024 * 1024 + 64 * 1024)]
    public async Task<IActionResult> GuardarImagen(int id, IFormFile? archivo)
    {
        if (archivo is null || archivo.Length is 0 or > 5 * 1024 * 1024)
        {
            TempData["Error"] = "Seleccione una imagen de hasta 5 MB.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await using var flujo = archivo.OpenReadStream();
            await tienda.SubirImagen(id, flujo, Path.GetFileName(archivo.FileName), archivo.ContentType);
            TempData["Mensaje"] = "Foto del celular guardada.";
        }
        catch (Exception error) when (EsErrorEsperado(error)) { TempData["Error"] = Mensaje(error); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarImagen(int id)
    {
        try { await tienda.QuitarImagen(id); TempData["Mensaje"] = "Foto del celular eliminada."; }
        catch (Exception error) when (EsErrorEsperado(error)) { TempData["Error"] = Mensaje(error); }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        try { await tienda.Eliminar(id); TempData["Mensaje"] = "Celular desactivado."; }
        catch (Exception error) when (EsErrorEsperado(error)) { TempData["Error"] = Mensaje(error); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Vender([Bind(Prefix = "Venta")] SolicitudFactura venta)
    {
        if (!ModelState.IsValid) return await Mostrar(venta, "Revise los datos de la venta.");
        try
        {
            var factura = await tienda.Facturar(venta);
            return RedirectToAction(nameof(Factura), new { id = factura.Id });
        }
        catch (Exception error) when (EsErrorEsperado(error))
        {
            return await Mostrar(venta, Mensaje(error) + " Si se cortó la conexión, reintente sin cambiar los datos.");
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BuscarFacturas(string? cedula)
    {
        try
        {
            var busqueda = cedula?.Trim() ?? "";
            var facturas = await tienda.FacturasPorCedula(busqueda);
            return View("Index", new PaginaTienda
            {
                Catalogo = await tienda.Catalogo(), Facturas = facturas,
                CedulaBusqueda = busqueda, Venta = new SolicitudFactura()
            });
        }
        catch (Exception error) when (EsErrorEsperado(error))
        {
            return await Mostrar(new SolicitudFactura(), Mensaje(error));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Factura(int id)
    {
        try
        {
            var factura = await tienda.Factura(id);
            var cuotas = factura.FormaPago == "CREDITO_DIRECTO" && factura.Estado == "APROBADA"
                ? await tienda.Amortizacion(id) : [];
            return View(new PaginaFactura(factura, cuotas));
        }
        catch (Exception error) when (EsErrorEsperado(error))
        {
            TempData["Error"] = Mensaje(error);
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<IActionResult> Mostrar(SolicitudFactura venta, string? error)
    {
        Celular[] catalogo = [];
        try { catalogo = await tienda.Catalogo(); }
        catch (Exception e) when (EsErrorEsperado(e)) { error ??= Mensaje(e); }
        return View("Index", new PaginaTienda { Catalogo = catalogo, Venta = venta, Error = error });
    }

    private static bool EsErrorEsperado(Exception error) =>
        error is InvalidOperationException or ArgumentException or HttpRequestException or TaskCanceledException;

    private static string Mensaje(Exception error) => error is HttpRequestException or TaskCanceledException
        ? "No se pudo conectar con la comercializadora. Compruebe que el servidor esté iniciado."
        : error.Message;
}
