using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Celulares.Contratos;

public record EvaluacionCredito(bool SujetoCredito, int? CodigoCliente, string Nombre, string Motivo, decimal MontoMaximo);
public record Cuota(int NumeroCuota, DateOnly FechaPago, decimal ValorCuota, decimal InteresPagado, decimal CapitalPagado, decimal Saldo);
public record Credito(int Id, int CodigoCliente, decimal Monto, int NumeroCuotas, DateOnly Fecha, string Estado, IReadOnlyList<Cuota> Cuotas);
public record Celular(int Id, string Nombre, decimal Precio, bool Activo, string? ImagenUrl = null);
public class GuardarCelular
{
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true)] public decimal Precio { get; set; }
    public bool Activo { get; set; } = true;
}
public class SolicitudCredito
{
    [Range(1, int.MaxValue)] public int CodigoCliente { get; set; }
    [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true)] public decimal PrecioTelefono { get; set; }
    [Range(3, 18)] public int NumeroCuotas { get; set; }
    public Guid ReferenciaVenta { get; set; } = Guid.NewGuid();
}
public class SolicitudFactura
{
    public Guid IdOperacion { get; set; } = Guid.NewGuid();
    [Required, StringLength(100)] public string Cedula { get; set; } = "";
    [Required, StringLength(100)] public string NombreCliente { get; set; } = "";
    [Range(1, int.MaxValue)] public int IdCelular { get; set; }
    [Required, RegularExpression("^(EFECTIVO|CREDITO_DIRECTO)$")] public string FormaPago { get; set; } = "EFECTIVO";
    public int? NumeroCuotas { get; set; }
    [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true)] public decimal? PrecioEsperado { get; set; }
}
public record Factura(int Id, Guid IdOperacion, DateTime Fecha, string Cedula, string NombreCliente,
    int IdCelular, string Celular, string FormaPago, decimal Precio, decimal Descuento,
    decimal Total, int? CreditoId, int? NumeroCuotas, string Estado)
{
    [JsonIgnore] public string Etiqueta => $"{Id} · {Fecha:yyyy-MM-dd} · {Celular} · ${Total:F2} · {Estado}";
}
public class ErrorNegocio(string mensaje, int estado = 409) : Exception(mensaje)
{
    public int Estado { get; } = estado;
}

