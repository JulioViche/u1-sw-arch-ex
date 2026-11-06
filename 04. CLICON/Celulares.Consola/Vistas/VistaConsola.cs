using Celulares.Cliente.Controladores;

namespace Celulares.Consola.Vistas;

internal static class VistaConsola
{
    public static bool TemaClaro { get; private set; }

    public static void AplicarTema()
    {
        if (Console.IsOutputRedirected) return;
        Console.BackgroundColor = TemaClaro ? ConsoleColor.White : ConsoleColor.Black;
        Console.ForegroundColor = TemaClaro ? ConsoleColor.Black : ConsoleColor.Gray;
    }

    public static void CambiarTema()
    {
        TemaClaro = !TemaClaro;
        AplicarTema();
        if (!Console.IsOutputRedirected) Console.Clear();
        Info(TemaClaro ? "Tema claro activado." : "Tema oscuro activado.");
    }

    public static void Encabezado()
    {
        Console.WriteLine();
        var orig = Console.ForegroundColor;
        Console.ForegroundColor = TemaClaro ? ConsoleColor.DarkCyan : ConsoleColor.Cyan;
        Console.WriteLine("  ╔══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║              📱 COMERCIALIZADORA DE CELULARES · GR09                     ║");
        Console.WriteLine("  ║         Catálogo oficial y financiamiento directo con BanQuito           ║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════════════════════════════╝");
        Console.ForegroundColor = orig;
    }

    public static void Seccion(string titulo)
    {
        var orig = Console.ForegroundColor;
        Console.ForegroundColor = TemaClaro ? ConsoleColor.DarkBlue : ConsoleColor.DarkCyan;
        var relleno = Math.Max(2, 68 - titulo.Length);
        Console.WriteLine($"\n  ─── {titulo} " + new string('─', relleno));
        Console.ForegroundColor = orig;
    }

    public static void Exito(string mensaje)
    {
        var orig = Console.ForegroundColor;
        Console.ForegroundColor = TemaClaro ? ConsoleColor.DarkGreen : ConsoleColor.Green;
        Console.WriteLine($"  ✓ {mensaje}");
        Console.ForegroundColor = orig;
    }

    public static void Error(string mensaje)
    {
        var orig = Console.ForegroundColor;
        Console.ForegroundColor = TemaClaro ? ConsoleColor.DarkRed : ConsoleColor.Red;
        Console.WriteLine($"  ✗ Error: {mensaje}");
        Console.ForegroundColor = orig;
    }

    public static void Aviso(string mensaje)
    {
        var orig = Console.ForegroundColor;
        Console.ForegroundColor = TemaClaro ? ConsoleColor.DarkYellow : ConsoleColor.Yellow;
        Console.WriteLine($"  ⚠️  {mensaje}");
        Console.ForegroundColor = orig;
    }

    public static void Info(string mensaje)
    {
        var orig = Console.ForegroundColor;
        Console.ForegroundColor = TemaClaro ? ConsoleColor.DarkCyan : ConsoleColor.Cyan;
        Console.WriteLine($"  ℹ️  {mensaje}");
        Console.ForegroundColor = orig;
    }

    public static string Texto(string etiqueta)
    {
        var orig = Console.ForegroundColor;
        Console.ForegroundColor = TemaClaro ? ConsoleColor.DarkBlue : ConsoleColor.Cyan;
        Console.Write($"  » {etiqueta}: ");
        Console.ForegroundColor = orig;
        return Console.ReadLine()?.Trim() ?? "";
    }

    public static int Entero(string etiqueta, int min = 1, int max = int.MaxValue)
    {
        while (true)
        {
            var entrada = Texto(etiqueta);
            if (int.TryParse(entrada, out var valor) && valor >= min && valor <= max)
                return valor;
            Error($"Ingrese un número entero válido (entre {min} y {(max == int.MaxValue ? "adelante" : max)}).");
        }
    }

    public static decimal Dinero(string etiqueta)
    {
        while (true)
        {
            try
            {
                return ControladorTienda.InterpretarPrecio(Texto(etiqueta));
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }
    }

    public static bool Confirmar(string mensaje)
    {
        var orig = Console.ForegroundColor;
        Console.ForegroundColor = TemaClaro ? ConsoleColor.DarkYellow : ConsoleColor.Yellow;
        Console.Write($"  » {mensaje} (S/N): ");
        Console.ForegroundColor = orig;
        var r = Console.ReadLine()?.Trim() ?? "";
        return r.Equals("s", StringComparison.OrdinalIgnoreCase) || r.Equals("si", StringComparison.OrdinalIgnoreCase);
    }

    public static void Pausa(string mensaje = "Presione ENTER para continuar...")
    {
        var orig = Console.ForegroundColor;
        Console.ForegroundColor = TemaClaro ? ConsoleColor.DarkGray : ConsoleColor.DarkGray;
        Console.Write($"\n  [{mensaje}] ");
        Console.ForegroundColor = orig;
        Console.ReadLine();
    }
}
