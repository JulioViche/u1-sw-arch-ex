using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace Celulares.Movil;

public static class MauiProgram
{
    // IP por defecto para hotspot Windows (192.168.137.1) o configurable dinámicamente en la app
    internal const string DireccionServidor = "http://192.168.137.1:19102/";

    public static MauiApp CreateMauiApp() => MauiApp.CreateBuilder().UseMauiApp<App>().Build();
}
