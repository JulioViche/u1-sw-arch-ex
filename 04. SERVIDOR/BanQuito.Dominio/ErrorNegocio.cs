namespace BanQuito.Dominio;

public class ErrorNegocio(string mensaje, int estado = 409) : Exception(mensaje)
{
    public int Estado { get; } = estado;
}
