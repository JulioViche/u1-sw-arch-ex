using System.ComponentModel.DataAnnotations;

namespace BanQuito.Contratos;

public record ClienteBanco(int CodigoCliente, string Cedula, string Nombre, string Genero, DateOnly FechaNacimiento);
public record Movimiento(string Tipo, decimal Valor, DateOnly Fecha);
public record EvaluacionCredito(bool SujetoCredito, int? CodigoCliente, string Nombre, string Motivo, decimal MontoMaximo);
public record Cuota(int NumeroCuota, DateOnly FechaPago, decimal ValorCuota, decimal InteresPagado, decimal CapitalPagado, decimal Saldo);
public record Credito(int Id, int CodigoCliente, decimal Monto, int NumeroCuotas, DateOnly Fecha, string Estado, IReadOnlyList<Cuota> Cuotas);

public class SolicitudCredito
{
    [Range(1, int.MaxValue)] public int CodigoCliente { get; set; }
    [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true)] public decimal PrecioTelefono { get; set; }
    [Range(3, 18)] public int NumeroCuotas { get; set; }
    public Guid ReferenciaVenta { get; set; } = Guid.NewGuid();
}
