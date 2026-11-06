using BanQuito.Api.Servicios;
using BanQuito.Contratos;
using Microsoft.AspNetCore.Mvc;
namespace BanQuito.Api.Controllers;

[ApiController]
public class CreditoController(ServicioCredito servicio) : ControllerBase
{
    [HttpGet("api/credito/elegibilidad")]
    public Task<EvaluacionCredito> Elegibilidad([FromQuery] string cedula, CancellationToken ct) =>
        servicio.Evaluar(cedula.Trim(), ct);

    [HttpGet("api/credito/clientes/{codigoCliente:int}/monto-maximo")]
    public async Task<object> MontoMaximo(int codigoCliente, CancellationToken ct) =>
        new { codigoCliente, montoMaximo = await servicio.MontoMaximo(codigoCliente, ct) };

    [HttpPost("api/creditos")]
    public Task<Credito> Otorgar(SolicitudCredito solicitud, CancellationToken ct) =>
        servicio.Otorgar(solicitud, ct);

    [HttpGet("api/creditos/{id:int}")]
    public Task<Credito> Obtener(int id, CancellationToken ct) => servicio.Obtener(id, ct);

    [HttpGet("api/creditos/{id:int}/amortizacion")]
    public async Task<IReadOnlyList<Cuota>> Amortizacion(int id, CancellationToken ct) =>
        (await servicio.Obtener(id, ct)).Cuotas;
}

