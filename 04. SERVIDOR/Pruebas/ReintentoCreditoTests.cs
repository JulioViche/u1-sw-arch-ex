using System.Net;
using System.Net.Http.Json;
using Celulares.Contratos;
using Comercializadora.Api.Servicios;
using Xunit;

namespace Comercializadora.Pruebas;

public class ReintentoCreditoTests
{
    [Fact]
    public async Task ReintentoRecuperaCreditoExistenteAunqueYaEsteActivo()
    {
        var operacion = Guid.NewGuid();
        var manejador = new BancoSimulado(async (solicitud, numero) =>
        {
            if (numero == 1)
                return Respuesta(new EvaluacionCredito(false, 7, "Cliente", "Ya tiene un crédito activo.", 0m));

            var credito = await solicitud.Content!.ReadFromJsonAsync<SolicitudCredito>();
            Assert.Equal(operacion, credito!.ReferenciaVenta);
            return Respuesta(new Credito(11, 7, 100m, 6, new DateOnly(2026, 1, 1), "ACTIVO", []));
        });
        var servicio = CrearServicio(manejador);

        var resultado = await servicio.OtorgarParaFactura(FacturaPendiente(operacion), CancellationToken.None);

        Assert.Equal(11, resultado.Id);
        Assert.Equal(2, manejador.Llamadas);
    }

    [Fact]
    public async Task ClienteInexistenteNoSolicitaCredito()
    {
        var manejador = new BancoSimulado((_, _) => Task.FromResult(
            Respuesta(new EvaluacionCredito(false, null, "", "Cliente inexistente.", 0m))));

        var error = await Assert.ThrowsAsync<ErrorNegocio>(() =>
            CrearServicio(manejador).OtorgarParaFactura(FacturaPendiente(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(409, error.Estado);
        Assert.Equal(1, manejador.Llamadas);
    }

    [Fact]
    public async Task BancoSigueRechazandoCreditoNuevoNoElegible()
    {
        var manejador = new BancoSimulado((_, numero) => Task.FromResult(numero == 1
            ? Respuesta(new EvaluacionCredito(false, 7, "Cliente", "No cumple la edad.", 0m))
            : new HttpResponseMessage(HttpStatusCode.Conflict)));

        var error = await Assert.ThrowsAsync<ErrorNegocio>(() =>
            CrearServicio(manejador).OtorgarParaFactura(FacturaPendiente(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(409, error.Estado);
        Assert.Equal(2, manejador.Llamadas);
    }

    private static ServicioBanQuito CrearServicio(BancoSimulado manejador) =>
        new(new HttpClient(manejador) { BaseAddress = new Uri("http://localhost/") });

    private static Factura FacturaPendiente(Guid operacion) =>
        new(1, operacion, DateTime.UtcNow, "0102030405", "Cliente", 1, "Teléfono",
            "CREDITO_DIRECTO", 100m, 0m, 100m, null, 6, "PENDIENTE");

    private static HttpResponseMessage Respuesta<T>(T cuerpo) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(cuerpo) };

    private sealed class BancoSimulado(Func<HttpRequestMessage, int, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public int Llamadas { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken ct) =>
            responder(solicitud, ++Llamadas);
    }
}
