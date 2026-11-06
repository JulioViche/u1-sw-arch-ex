namespace BanQuito.Dominio;

public class ErrorNegocio(string mensaje, int estado = 409) : InvalidOperationException(mensaje)
{
    public int Estado { get; } = estado;
}
