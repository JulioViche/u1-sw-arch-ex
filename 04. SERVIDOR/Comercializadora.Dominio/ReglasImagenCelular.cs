using Celulares.Contratos;

namespace Comercializadora.Dominio;

public static class ReglasImagenCelular
{
    public const int MaximoBytes = 5 * 1024 * 1024;

    public static string Validar(ReadOnlySpan<byte> datos, string? tipo)
    {
        if (datos.Length is 0 or > MaximoBytes) throw new ErrorNegocio("La imagen debe pesar entre 1 byte y 5 MB.", 400);
        var valido = tipo?.ToLowerInvariant() switch
        {
            "image/jpeg" => datos.Length >= 3 && datos[0] == 0xff && datos[1] == 0xd8 && datos[2] == 0xff,
            "image/png" => datos.Length >= 8 && datos[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/webp" => datos.Length >= 12 && datos[..4].SequenceEqual("RIFF"u8) && datos.Slice(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
        if (!valido) throw new ErrorNegocio("Use una imagen JPEG, PNG o WebP válida.", 400);
        return tipo!.ToLowerInvariant();
    }
}
