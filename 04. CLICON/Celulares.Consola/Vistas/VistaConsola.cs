using Celulares.Cliente.Controladores;

namespace Celulares.Consola.Vistas;

internal static class VistaConsola
{
    private static bool temaClaro;

    public static void AplicarTema()
    {
        if (Console.IsOutputRedirected) return;
        Console.BackgroundColor = temaClaro ? ConsoleColor.White : ConsoleColor.Black;
        Console.ForegroundColor = temaClaro ? ConsoleColor.Black : ConsoleColor.Gray;
    }

    public static void CambiarTema()
    {
        temaClaro = !temaClaro;
        AplicarTema();
        if (!Console.IsOutputRedirected) Console.Clear();
    }

    public static void Encabezado()
    {
        var color = Console.ForegroundColor;
        Console.ForegroundColor = temaClaro ? ConsoleColor.DarkCyan : ConsoleColor.Cyan;
        Console.WriteLine("\nCOMERCIALIZADORA GR09");
        Console.ForegroundColor = color;
    }

    public static string Texto(string etiqueta)
    {
        Console.Write(etiqueta + ": ");
        return Console.ReadLine()?.Trim() ?? "";
    }

    public static int Entero(string etiqueta)
    {
        if (!int.TryParse(Texto(etiqueta), out var valor) || valor <= 0)
            throw new ArgumentException("Ingrese un número entero positivo.");
        return valor;
    }

    public static decimal Dinero(string etiqueta)
    {
        return ControladorTienda.InterpretarPrecio(Texto(etiqueta));
    }

    public static bool Confirmar(string mensaje) =>
        Texto(mensaje + " (s/n)").Equals("s", StringComparison.OrdinalIgnoreCase);
}