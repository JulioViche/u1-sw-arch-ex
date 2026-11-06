using BanQuito.Contratos;
using BanQuito.Dominio;
using BanQuito.Api.Servicios;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ServicioCredito>();
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()!.Error;
    var estado = error is ErrorNegocio n ? n.Estado :
        error is SqlException sql && sql.Number is 2601 or 2627 ? 409 : error is SqlException ? 503 : 500;
    var mensaje = error is ErrorNegocio ? error.Message :
        estado == 409 ? "La operación entra en conflicto con un crédito existente." :
        estado == 503 ? "La base de datos no está disponible. Revisar el servicio SQL Server." : "Error interno del servicio.";
    app.Logger.LogError(error, "Error de crédito");
    context.Response.StatusCode = estado;
    await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = estado, Title = mensaje });
}));
app.MapControllers();
app.MapGet("/api/salud", async (ServicioCredito servicio, CancellationToken ct) =>
{
    await servicio.ComprobarConexion(ct);
    return Results.Ok(new { servicio = "BanQuito", baseDatos = "disponible" });
});
app.Run();

