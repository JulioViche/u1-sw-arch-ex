using System.Data;
using Celulares.Contratos;
using Comercializadora.Dominio;
using Microsoft.Data.SqlClient;

namespace Comercializadora.Api.Servicios;

public class ServicioFacturas(IConfiguration configuracion, ServicioBanQuito banco)
{
    private const string ProyeccionFactura = """
        ID_FACTURA,ID_OPERACION,FECHA,CEDULA,NOMBRE_CLIENTE,ID_CELULAR,NOMBRE_CELULAR,
        FORMA_PAGO,PRECIO,DESCUENTO,TOTAL,COD_CREDITO,NUMERO_CUOTAS,ESTADO
        """;

    private static string ConsultaFactura(string? top = null) =>
        $"SELECT {top} {ProyeccionFactura} FROM dbo.FACTURA";

    private sealed record DatosSolicitud(Guid IdOperacion, string Cedula, string NombreCliente,
        int IdCelular, string FormaPago, int? NumeroCuotas, decimal? PrecioEsperado);

    private async Task<SqlConnection> Abrir(CancellationToken ct)
    {
        var cadena = configuracion.GetConnectionString("BaseDatos") ??
            throw new InvalidOperationException("Falta ConnectionStrings:BaseDatos.");
        var db = new SqlConnection(cadena);
        try
        {
            await db.OpenAsync(ct);
            return db;
        }
        catch
        {
            await db.DisposeAsync();
            throw;
        }
    }

    private static SqlCommand Comando(SqlConnection db, SqlTransaction? tx, string texto,
        params (string Nombre, object? Valor)[] valores)
    {
        var cmd = new SqlCommand(texto, db, tx);
        foreach (var (nombre, valor) in valores)
        {
            if (valor is decimal dinero)
            {
                var parametro = cmd.Parameters.Add(nombre, SqlDbType.Decimal);
                parametro.Precision = 10;
                parametro.Scale = 2;
                parametro.Value = dinero;
            }
            else
            {
                cmd.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);
            }
        }
        return cmd;
    }

    private static Factura Leer(SqlDataReader lector) => new(
        lector.GetInt32(0), lector.GetGuid(1), lector.GetDateTime(2), lector.GetString(3),
        lector.GetString(4), lector.GetInt32(5), lector.GetString(6), lector.GetString(7),
        lector.GetDecimal(8), lector.GetDecimal(9), lector.GetDecimal(10),
        lector.IsDBNull(11) ? null : lector.GetInt32(11),
        lector.IsDBNull(12) ? null : lector.GetInt32(12), lector.GetString(13));

    private static DatosSolicitud Preparar(SolicitudFactura solicitud)
    {
        if (solicitud.IdOperacion == Guid.Empty) throw new ErrorNegocio("IdOperacion no puede estar vacío.", 400);
        var cedula = solicitud.Cedula?.Trim() ?? "";
        var nombre = solicitud.NombreCliente?.Trim() ?? "";
        var forma = solicitud.FormaPago?.Trim() ?? "";
        if (cedula.Length == 0 || cedula.Length > 100) throw new ErrorNegocio("La cédula es obligatoria y admite hasta 100 caracteres.", 400);
        if (nombre.Length == 0 || nombre.Length > 100) throw new ErrorNegocio("El nombre del cliente es obligatorio y admite hasta 100 caracteres.", 400);

        if (forma == "EFECTIVO" && solicitud.NumeroCuotas is not null)
            throw new ErrorNegocio("Una venta en efectivo no admite número de cuotas.", 400);
        if (forma == "CREDITO_DIRECTO" && solicitud.NumeroCuotas is not (>= 3 and <= 18))
            throw new ErrorNegocio("El crédito directo requiere entre 3 y 18 cuotas.", 400);
        if (forma is not ("EFECTIVO" or "CREDITO_DIRECTO"))
            throw new ErrorNegocio("FormaPago debe ser EFECTIVO o CREDITO_DIRECTO.", 400);

        return new(solicitud.IdOperacion, cedula, nombre, solicitud.IdCelular, forma,
            solicitud.NumeroCuotas, solicitud.PrecioEsperado);
    }

    private static async Task<Factura?> LeerPor(SqlConnection db, SqlTransaction? tx, string columna,
        object valor, bool bloquear, CancellationToken ct)
    {
        var pista = bloquear ? " WITH (UPDLOCK,HOLDLOCK)" : "";
        await using var cmd = Comando(db, tx, ConsultaFactura() + pista + $" WHERE {columna}=@valor", ("@valor", valor));
        await using var lector = await cmd.ExecuteReaderAsync(ct);
        return await lector.ReadAsync(ct) ? Leer(lector) : null;
    }

    private static void ValidarReintento(Factura factura, DatosSolicitud solicitud)
    {
        ReglasFacturacion.ValidarPrecioEsperado(factura.Precio, solicitud.PrecioEsperado);
        if (!string.Equals(factura.Cedula, solicitud.Cedula, StringComparison.Ordinal) ||
            !string.Equals(factura.NombreCliente, solicitud.NombreCliente, StringComparison.Ordinal) ||
            factura.IdCelular != solicitud.IdCelular || factura.FormaPago != solicitud.FormaPago ||
            factura.NumeroCuotas != solicitud.NumeroCuotas)
            throw new ErrorNegocio("IdOperacion ya está asociada a una solicitud diferente.", 409);
    }

    public async Task<IReadOnlyList<Factura>> ListarPorCedula(string? cedula, CancellationToken ct)
    {
        var limpia = cedula?.Trim() ?? "";
        if (limpia.Length is < 1 or > 100)
            throw new ErrorNegocio("Ingrese una cédula de 1 a 100 caracteres.", 400);

        await using var db = await Abrir(ct);
        await using var cmd = Comando(db, null,
            ConsultaFactura("TOP (50)") + " WHERE CEDULA=@cedula ORDER BY FECHA DESC,ID_FACTURA DESC",
            ("@cedula", limpia));
        await using var lector = await cmd.ExecuteReaderAsync(ct);
        var facturas = new List<Factura>();
        while (await lector.ReadAsync(ct)) facturas.Add(Leer(lector));
        return facturas;
    }

    public async Task<Factura> Crear(SolicitudFactura entrada, CancellationToken ct)
    {
        var solicitud = Preparar(entrada);
        var factura = await ObtenerOCrear(solicitud, ct);
        ValidarReintento(factura, solicitud);

        if (factura.FormaPago == "EFECTIVO" || factura.Estado == "APROBADA") return factura;
        if (factura.Estado == "RECHAZADA")
            throw new ErrorNegocio("La operación ya fue rechazada por BanQuito.", 409);

        try
        {
            var credito = await banco.OtorgarParaFactura(factura, ct);
            return await MarcarAprobada(factura.IdOperacion, credito.CodigoCliente, credito.Id, ct);
        }
        catch (ErrorNegocio error) when (error.Estado is >= 400 and < 500)
        {
            await MarcarRechazada(factura.IdOperacion, ct);
            throw;
        }
    }

    private async Task<Factura> ObtenerOCrear(DatosSolicitud solicitud, CancellationToken ct)
    {
        try
        {
            await using var db = await Abrir(ct);
            await using var tx = (SqlTransaction)await db.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var existente = await LeerPor(db, tx, "ID_OPERACION", solicitud.IdOperacion, true, ct);
            if (existente is not null)
            {
                ValidarReintento(existente, solicitud);
                await tx.CommitAsync(ct);
                return existente;
            }

            decimal precio;
            string nombreCelular;
            await using (var cmd = Comando(db, tx, """
                SELECT NOMBRE,PRECIO FROM dbo.CELULAR WITH (UPDLOCK,HOLDLOCK)
                WHERE ID_CELULAR=@id AND ACTIVO=1
                """, ("@id", solicitud.IdCelular)))
            {
                await using var lector = await cmd.ExecuteReaderAsync(ct);
                if (!await lector.ReadAsync(ct)) throw new ErrorNegocio("Celular inexistente o inactivo.", 404);
                nombreCelular = lector.GetString(0);
                precio = lector.GetDecimal(1);
            }
            ReglasFacturacion.ValidarPrecioEsperado(precio, solicitud.PrecioEsperado);

            var total = solicitud.FormaPago == "EFECTIVO" ? ReglasFacturacion.TotalEfectivo(precio) : precio;
            var descuento = precio - total;
            var estado = solicitud.FormaPago == "EFECTIVO" ? "APROBADA" : "PENDIENTE";
            await using (var cmd = Comando(db, tx, """
                INSERT dbo.FACTURA(ID_OPERACION,CEDULA,NOMBRE_CLIENTE,ID_CELULAR,NOMBRE_CELULAR,
                    FORMA_PAGO,PRECIO,DESCUENTO,TOTAL,COD_CLIENTE_BANCO,COD_CREDITO,NUMERO_CUOTAS,ESTADO)
                OUTPUT INSERTED.ID_FACTURA
                VALUES(@operacion,@cedula,@cliente,@celular,@nombreCelular,@forma,@precio,@descuento,
                    @total,NULL,NULL,@cuotas,@estado)
                """, ("@operacion", solicitud.IdOperacion), ("@cedula", solicitud.Cedula),
                ("@cliente", solicitud.NombreCliente), ("@celular", solicitud.IdCelular),
                ("@nombreCelular", nombreCelular), ("@forma", solicitud.FormaPago), ("@precio", precio),
                ("@descuento", descuento), ("@total", total), ("@cuotas", solicitud.NumeroCuotas), ("@estado", estado)))
            {
                await cmd.ExecuteScalarAsync(ct);
            }
            await tx.CommitAsync(ct);
            return await ObtenerPorOperacion(solicitud.IdOperacion, ct) ??
                throw new ErrorNegocio("No se pudo leer la factura recién creada.", 500);
        }
        catch (SqlException error) when (error.Number is 2601 or 2627)
        {
            var existente = await ObtenerPorOperacion(solicitud.IdOperacion, ct);
            if (existente is null) throw;
            ValidarReintento(existente, solicitud);
            return existente;
        }
    }

    private async Task<Factura?> ObtenerPorOperacion(Guid id, CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        return await LeerPor(db, null, "ID_OPERACION", id, false, ct);
    }

    public async Task<Factura> Obtener(int id, CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        return await LeerPor(db, null, "ID_FACTURA", id, false, ct) ??
            throw new ErrorNegocio("Factura inexistente.", 404);
    }

    public async Task<IReadOnlyList<Cuota>> Amortizacion(int id, CancellationToken ct)
    {
        var factura = await Obtener(id, ct);
        if (factura.FormaPago != "CREDITO_DIRECTO" || factura.Estado != "APROBADA" || factura.CreditoId is null)
            throw new ErrorNegocio("La factura no tiene un crédito aprobado.", 409);
        return await banco.Amortizacion(factura.CreditoId.Value, ct);
    }

    private async Task<Factura> MarcarAprobada(Guid operacion, int codigoCliente, int creditoId, CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        await using var cmd = Comando(db, null, """
            UPDATE dbo.FACTURA SET ESTADO='APROBADA',COD_CLIENTE_BANCO=@cliente,COD_CREDITO=@credito
            WHERE ID_OPERACION=@operacion AND ESTADO='PENDIENTE'
            """, ("@cliente", codigoCliente), ("@credito", creditoId), ("@operacion", operacion));
        if (await cmd.ExecuteNonQueryAsync(ct) == 0)
        {
            var actual = await LeerPor(db, null, "ID_OPERACION", operacion, false, ct);
            if (actual is not { Estado: "APROBADA" } || actual.CreditoId != creditoId)
                throw new ErrorNegocio("La factura dejó de estar pendiente.", 409);
            return actual;
        }
        return await LeerPor(db, null, "ID_OPERACION", operacion, false, ct) ??
            throw new ErrorNegocio("No se pudo leer la factura aprobada.", 500);
    }

    private async Task MarcarRechazada(Guid operacion, CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        await using var cmd = Comando(db, null,
            "UPDATE dbo.FACTURA SET ESTADO='RECHAZADA' WHERE ID_OPERACION=@operacion AND ESTADO='PENDIENTE'",
            ("@operacion", operacion));
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
