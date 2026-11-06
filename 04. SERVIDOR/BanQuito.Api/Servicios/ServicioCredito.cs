using System.Data;
using BanQuito.Dominio;
using BanQuito.Contratos;
using Microsoft.Data.SqlClient;
namespace BanQuito.Api.Servicios;

public class ServicioCredito(IConfiguration config, TimeProvider reloj)
{
    private DateOnly Hoy => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
    private async Task<SqlConnection> Abrir(CancellationToken ct)
    {
        var db = new SqlConnection(config.GetConnectionString("BaseDatos") ?? throw new InvalidOperationException("Falta ConnectionStrings:BaseDatos."));
        try { await db.OpenAsync(ct); return db; }
        catch { await db.DisposeAsync(); throw; }
    }
    private static SqlCommand Cmd(SqlConnection db, SqlTransaction? tx, string sql, params (string, object)[] valores)
    {
        var cmd = new SqlCommand(sql, db, tx);
        foreach (var (nombre, valor) in valores)
        {
            var p = cmd.Parameters.AddWithValue(nombre, valor);
            if (valor is decimal) { p.SqlDbType = SqlDbType.Decimal; p.Precision = 10; p.Scale = 2; }
        }
        return cmd;
    }
    public async Task ComprobarConexion(CancellationToken ct) { await using var db = await Abrir(ct); }

    private static async Task<ClienteBanco?> Cliente(SqlConnection db, SqlTransaction? tx, string filtro, object valor, bool bloquear, CancellationToken ct)
    {
        await using var cmd = Cmd(db, tx,
            "SELECT COD_CLIENTE,CEDULA,NOMBRE,GENERO,FECHA_NACIMIENTO FROM dbo.CLIENTE " +
            (bloquear ? "WITH (UPDLOCK,HOLDLOCK) " : "") + "WHERE " + filtro + "=@valor", ("@valor", valor));
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? new(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), DateOnly.FromDateTime(r.GetDateTime(4))) : null;
    }
    private async Task<EvaluacionCredito> Contexto(SqlConnection db, SqlTransaction? tx, ClienteBanco? cliente, DateOnly hoy, CancellationToken ct)
    {
        if (cliente is null) return ReglasCredito.Evaluar(null, [], false, hoy);
        var movimientos = new List<Movimiento>();
        await using (var cmd = Cmd(db, tx, """
            SELECT m.TIPO,m.VALOR,m.FECHA FROM dbo.MOVIMIENTO m
            JOIN dbo.CUENTA c ON c.NUM_CUENTA=m.NUM_CUENTA
            WHERE c.COD_CLIENTE=@cliente AND m.FECHA BETWEEN @desde AND @hasta
            """, ("@cliente", cliente.CodigoCliente), ("@desde", hoy.AddMonths(-3).ToDateTime(TimeOnly.MinValue)), ("@hasta", hoy.ToDateTime(TimeOnly.MinValue))))
        {
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) movimientos.Add(new(r.GetString(0), r.GetDecimal(1), DateOnly.FromDateTime(r.GetDateTime(2))));
        }
        await using var activo = Cmd(db, tx, "SELECT COUNT(*) FROM dbo.CREDITO WHERE COD_CLIENTE=@cliente AND ESTADO='ACTIVO'", ("@cliente", cliente.CodigoCliente));
        var tieneActivo = Convert.ToInt32(await activo.ExecuteScalarAsync(ct)) > 0;
        return ReglasCredito.Evaluar(cliente, movimientos, tieneActivo, hoy);
    }
    public async Task<EvaluacionCredito> Evaluar(string cedula, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cedula) || cedula.Length > 100) throw new ErrorNegocio("La cédula es obligatoria y admite hasta 100 caracteres.", 400);
        await using var db = await Abrir(ct);
        return await Contexto(db, null, await Cliente(db, null, "CEDULA", cedula, false, ct), Hoy, ct);
    }
    public async Task<decimal> MontoMaximo(int codigo, CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        var cliente = await Cliente(db, null, "COD_CLIENTE", codigo, false, ct) ?? throw new ErrorNegocio("Cliente inexistente.", 404);
        return (await Contexto(db, null, cliente, Hoy, ct)).MontoMaximo;
    }
    public async Task<Credito> Obtener(int id, CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        return await LeerCredito(db, null, "COD_CREDITO", id, ct) ?? throw new ErrorNegocio("Crédito inexistente.", 404);
    }
    private static async Task<Credito?> LeerCredito(SqlConnection db, SqlTransaction? tx, string filtro, object valor, CancellationToken ct)
    {
        int id, cliente, meses; decimal monto; DateOnly fecha; string estado;
        await using (var cmd = Cmd(db, tx, "SELECT COD_CREDITO,COD_CLIENTE,MONTO,NUMERO_CUOTAS,FECHA,ESTADO FROM dbo.CREDITO WHERE " + filtro + "=@valor", ("@valor", valor)))
        {
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return null;
            id = r.GetInt32(0); cliente = r.GetInt32(1); monto = r.GetDecimal(2); meses = r.GetInt32(3);
            fecha = DateOnly.FromDateTime(r.GetDateTime(4)); estado = r.GetString(5);
        }
        var cuotas = new List<Cuota>();
        await using (var cmd = Cmd(db, tx, """
            SELECT NUMERO_CUOTA,FECHA_PAGO,VALOR_CUOTA,INTERES_PAGADO,CAPITAL_PAGADO,SALDO
            FROM dbo.AMORTIZACION WHERE COD_CREDITO=@id ORDER BY NUMERO_CUOTA
            """, ("@id", id)))
        {
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                cuotas.Add(new(r.GetInt32(0), DateOnly.FromDateTime(r.GetDateTime(1)), r.GetDecimal(2), r.GetDecimal(3), r.GetDecimal(4), r.GetDecimal(5)));
        }
        return new(id, cliente, monto, meses, fecha, estado, cuotas);
    }
    public async Task<Credito> Otorgar(SolicitudCredito solicitud, CancellationToken ct)
    {
        if (solicitud.PrecioTelefono != ReglasCredito.Dinero(solicitud.PrecioTelefono)) throw new ErrorNegocio("El precio admite dos decimales.", 400);
        var hoy = Hoy;
        var tabla = ReglasCredito.Amortizar(solicitud.PrecioTelefono, solicitud.NumeroCuotas, hoy);
        await using var db = await Abrir(ct);
        await using var tx = (SqlTransaction)await db.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var cliente = await Cliente(db, tx, "COD_CLIENTE", solicitud.CodigoCliente, true, ct) ?? throw new ErrorNegocio("Cliente inexistente.", 404);
        var existente = await LeerCredito(db, tx, "REFERENCIA_VENTA", solicitud.ReferenciaVenta, ct);
        if (existente is not null)
        {
            if (existente.CodigoCliente != solicitud.CodigoCliente || existente.Monto != solicitud.PrecioTelefono || existente.NumeroCuotas != solicitud.NumeroCuotas)
                throw new ErrorNegocio("La referencia ya corresponde a otro crédito.");
            await tx.CommitAsync(ct);
            return existente;
        }
        var evaluacion = await Contexto(db, tx, cliente, hoy, ct);
        if (!evaluacion.SujetoCredito) throw new ErrorNegocio(evaluacion.Motivo);
        if (solicitud.PrecioTelefono > evaluacion.MontoMaximo) throw new ErrorNegocio("El precio supera el monto máximo aprobado.");
        int id;
        await using (var cmd = Cmd(db, tx, """
            INSERT dbo.CREDITO(COD_CLIENTE,REFERENCIA_VENTA,MONTO,NUMERO_CUOTAS,TASA_ANUAL,FECHA,ESTADO)
            OUTPUT INSERTED.COD_CREDITO VALUES(@cliente,@referencia,@monto,@meses,0.165,@fecha,'ACTIVO')
            """, ("@cliente", cliente.CodigoCliente), ("@referencia", solicitud.ReferenciaVenta),
            ("@monto", solicitud.PrecioTelefono), ("@meses", solicitud.NumeroCuotas), ("@fecha", hoy.ToDateTime(TimeOnly.MinValue))))
            id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        foreach (var cuota in tabla)
        {
            await using var cmd = Cmd(db, tx, """
                INSERT dbo.AMORTIZACION VALUES(@id,@numero,@fecha,@valor,@interes,@capital,@saldo)
                """, ("@id", id), ("@numero", cuota.NumeroCuota), ("@fecha", cuota.FechaPago.ToDateTime(TimeOnly.MinValue)),
                ("@valor", cuota.ValorCuota), ("@interes", cuota.InteresPagado), ("@capital", cuota.CapitalPagado), ("@saldo", cuota.Saldo));
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
        return new(id, cliente.CodigoCliente, solicitud.PrecioTelefono, solicitud.NumeroCuotas, hoy, "ACTIVO", tabla);
    }
}

