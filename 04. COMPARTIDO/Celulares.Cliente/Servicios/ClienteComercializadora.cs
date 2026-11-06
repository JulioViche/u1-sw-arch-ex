using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Celulares.Contratos;

namespace Celulares.Cliente.Servicios;

public sealed class ClienteComercializadora(HttpClient http)
{
    public static Uri Direccion(string? valor, string predeterminada = "http://127.0.0.1:19102/")
    {
        var texto = string.IsNullOrWhiteSpace(valor) ? predeterminada : valor.Trim();
        if (!Uri.TryCreate(texto.EndsWith('/') ? texto : texto + "/", UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("La dirección del servidor debe ser una URL HTTP o HTTPS válida.");
        return uri;
    }

    public async Task<Celular[]> Listar(CancellationToken ct = default) =>
        (await Leer<Celular[]>(http.GetAsync("api/celulares", ct), ct)).Select(ConUrlImagen).ToArray();

    public async Task<Celular> Guardar(int? id, GuardarCelular celular, CancellationToken ct = default) =>
        ConUrlImagen(await Leer<Celular>(id is null ? http.PostAsJsonAsync("api/celulares", celular, ct)
            : http.PutAsJsonAsync($"api/celulares/{id}", celular, ct), ct));

    public async Task SubirImagen(int id, Stream datos, string nombre, string tipo, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        using var archivo = new StreamContent(datos);
        archivo.Headers.ContentType = new MediaTypeHeaderValue(tipo);
        form.Add(archivo, "archivo", Path.GetFileName(nombre));
        using var respuesta = await http.PutAsync($"api/celulares/{id}/imagen", form, ct);
        await Comprobar(respuesta, ct);
    }

    public async Task QuitarImagen(int id, CancellationToken ct = default)
    {
        using var respuesta = await http.DeleteAsync($"api/celulares/{id}/imagen", ct);
        await Comprobar(respuesta, ct);
    }

    public Task<byte[]> DescargarImagen(Celular celular, CancellationToken ct = default) =>
        celular.ImagenUrl is null ? Task.FromResult(Array.Empty<byte>()) : http.GetByteArrayAsync(celular.ImagenUrl, ct);

    public async Task Eliminar(int id, CancellationToken ct = default)
    {
        using var respuesta = await http.DeleteAsync($"api/celulares/{id}", ct);
        await Comprobar(respuesta, ct);
    }

    public Task<Factura> Facturar(SolicitudFactura factura, CancellationToken ct = default) =>
        Leer<Factura>(http.PostAsJsonAsync("api/facturas", factura, ct), ct);

    public Task<EvaluacionCredito> EvaluarCredito(string cedula, CancellationToken ct = default) =>
        Leer<EvaluacionCredito>(http.GetAsync($"api/credito/elegibilidad?cedula={Uri.EscapeDataString(cedula?.Trim() ?? "")}", ct), ct);

    public Task<Factura[]> FacturasPorCedula(string cedula, CancellationToken ct = default) =>
        Leer<Factura[]>(http.GetAsync($"api/facturas?cedula={Uri.EscapeDataString(cedula?.Trim() ?? "")}", ct), ct);

    public Task<Factura> ObtenerFactura(int id, CancellationToken ct = default) =>
        Leer<Factura>(http.GetAsync($"api/facturas/{id}", ct), ct);

    public Task<Cuota[]> Amortizacion(int id, CancellationToken ct = default) =>
        Leer<Cuota[]>(http.GetAsync($"api/facturas/{id}/amortizacion", ct), ct);

    private Celular ConUrlImagen(Celular celular) => celular.ImagenUrl is null ? celular :
        celular with { ImagenUrl = new Uri(http.BaseAddress!, celular.ImagenUrl).AbsoluteUri };

    private static async Task<T> Leer<T>(Task<HttpResponseMessage> enviar, CancellationToken ct)
    {
        using var respuesta = await enviar;
        await Comprobar(respuesta, ct);
        return await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new InvalidOperationException("El servidor devolvió una respuesta vacía.");
    }

    private static async Task Comprobar(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.IsSuccessStatusCode) return;
        string? mensaje = null;
        try
        {
            using var json = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (json.RootElement.TryGetProperty("title", out var titulo)) mensaje = titulo.GetString();
        }
        catch (JsonException) { }
        throw new InvalidOperationException(mensaje ?? $"El servidor rechazó la operación (HTTP {(int)respuesta.StatusCode}).");
    }
}
