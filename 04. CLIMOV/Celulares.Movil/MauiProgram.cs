using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Celulares.Cliente.Servicios;

namespace Celulares.Movil;

public static class MauiProgram
{
    // IP por defecto para hotspot Windows; CELULARES_API_URL puede cambiarla por entorno.
    internal static string DireccionServidor =>
        ClienteComercializadora.Direccion(Environment.GetEnvironmentVariable("CELULARES_API_URL")).ToString();

    public static MauiApp CreateMauiApp() => MauiApp.CreateBuilder().UseMauiApp<App>().Build();
}
