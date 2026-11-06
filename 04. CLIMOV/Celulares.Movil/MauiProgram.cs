using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace Celulares.Movil;

public static class MauiProgram
{
    internal const string DireccionServidor = "http://10.0.2.2:19102/";

    public static MauiApp CreateMauiApp() => MauiApp.CreateBuilder().UseMauiApp<App>().Build();
}
