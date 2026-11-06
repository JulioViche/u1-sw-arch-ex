using System.Net.Http.Json;
using System.Text.Json;
using Celulares.Contratos;
using Microsoft.AspNetCore.Mvc;

namespace Comercializadora.Api.Servicios;

public class ServicioBanQuito(HttpClient cliente)
{
    public Task<EvaluacionCredito> Evaluar(string cedula, CancellationToken ct) =>
        Solicitar<EvaluacionCredito>(
            () => cliente.GetAsync($"api/credito/elegibilidad?cedula={Uri.EscapeDataString(cedula)}", ct), ct);

    public Task<Credito> Otorgar(SolicitudCredito solicitud, CancellationToken ct) =>
        Solicitar<Credito>(() => cliente.PostAsJsonAsync("api/creditos", solicitud, ct), ct);

    public async Task<Credito> OtorgarParaFactura(Factura factura, CancellationToken ct)
    {
        var evaluacion = await Evaluar(factura.Cedula, ct);
        if (evaluacion.CodigoCliente is not int codigoCliente)
            throw new ErrorNegocio(string.IsNullOrWhiteSpace(evaluacion.Motivo)
                ? "BanQuito no identificó al cliente." : evaluacion.Motivo, 409);

        // El banco resuelve primero la referencia idempotente y vuelve a validar créditos nuevos.
        var credito = await Otorgar(new SolicitudCredito
        {
            CodigoCliente = codigoCliente,
            PrecioTelefono = factura.Total,
            NumeroCuotas = factura.NumeroCuotas ?? throw new ErrorNegocio("Faltan las cuotas del crédito.", 500),
            ReferenciaVenta = factura.IdOperacion
        }, ct);
        if (credito.CodigoCliente != codigoCliente || credito.Monto != factura.Total ||
            credito.NumeroCuotas != factura.NumeroCuotas)
            throw new ErrorNegocio("BanQuito devolvió datos de crédito que no coinciden con la factura.", 502);
        return credito;
    }

    public async Task<IReadOnlyList<Cuota>> Amortizacion(int creditoId, CancellationToken ct)
    {
        var cuotas = await Solicitar<Cuota[]>(
            () => cliente.GetAsync($"api/creditos/{creditoId}/amortizacion", ct), ct);
        return cuotas;
    }

    private static async Task<T> Solicitar<T>(Func<Task<HttpResponseMessage>> enviar, CancellationToken ct)
    {
        try
        {
            using var respuesta = await enviar();
            if (!respuesta.IsSuccessStatusCode)
            {
                string? titulo = null;
                try
                {
                    var problema = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
                    titulo = problema?.Title;
                }
                catch (JsonException)
                {
                    // La respuesta puede no estar en formato ProblemDetails.
                }

                var estado = (int)respuesta.StatusCode >= 500 ? 503 : 409;
                throw new ErrorNegocio(titulo is null ? "BanQuito rechazó la operación." : $"BanQuito: {titulo}", estado);
            }

            return await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct) ??
                throw new ErrorNegocio("BanQuito devolvió una respuesta vacía.", 502);
        }
        catch (ErrorNegocio)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw new ErrorNegocio("No fue posible comunicarse con BanQuito.", 503);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ErrorNegocio("BanQuito no respondió dentro del tiempo esperado.", 503);
        }
    }
}
