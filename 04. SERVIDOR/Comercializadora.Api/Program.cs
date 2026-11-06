using Celulares.Contratos;
using Comercializadora.Api.Servicios;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddScoped<ServicioCatalogo>();
builder.Services.AddScoped<ServicioFacturas>();
builder.Services.AddHttpClient<ServicioBanQuito>((services, client) =>
{
    var configuracion = services.GetRequiredService<IConfiguration>();
    var direccion = configuracion["Servicios:BanQuitoUrl"] ?? "http://127.0.0.1:19101/";
    if (!direccion.EndsWith('/')) direccion += "/";
    client.BaseAddress = new Uri(direccion, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()!.Error;
    var estado = error is ErrorNegocio negocio ? negocio.Estado :
        error is SqlException sql && sql.Number is 2601 or 2627 ? 409 :
        error is SqlException or HttpRequestException ? 503 : 500;
    var titulo = error is ErrorNegocio negocioError ? negocioError.Message :
        estado == 409 ? "La operación ya existe o entra en conflicto." :
        estado == 503 ? "La base de datos o BanQuito no está disponible." :
        "Error interno del servicio.";
    app.Logger.LogError(error, "Error de la API Comercializadora");
    context.Response.StatusCode = estado;
    await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = estado, Title = titulo });
}));

app.MapControllers();
app.MapGet("/api/salud", async (ServicioCatalogo servicio, CancellationToken ct) =>
{
    await servicio.ComprobarConexion(ct);
    return Results.Ok(new { servicio = "Comercializadora", baseDatos = "disponible" });
});
app.Run();
