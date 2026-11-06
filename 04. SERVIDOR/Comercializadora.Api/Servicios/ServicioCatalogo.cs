using System.Data;
using Celulares.Contratos;
using Microsoft.Data.SqlClient;

namespace Comercializadora.Api.Servicios;

public class ServicioCatalogo(IConfiguration configuracion)
{
    private async Task<SqlConnection> Abrir(CancellationToken ct)
    {
        var cadena = configuracion.GetConnectionString("BaseDatos") ??
            throw new InvalidOperationException("Falta ConnectionStrings:BaseDatos.");
        var db = new SqlConnection(cadena);
        try { await db.OpenAsync(ct); return db; }
        catch { await db.DisposeAsync(); throw; }
    }

    private static SqlCommand Comando(SqlConnection db, string texto, params (string Nombre, object? Valor)[] valores)
    {
        var cmd = new SqlCommand(texto, db);
        foreach (var (nombre, valor) in valores)
        {
            if (valor is decimal dinero)
            {
                var parametro = cmd.Parameters.Add(nombre, SqlDbType.Decimal);
                parametro.Precision = 10; parametro.Scale = 2; parametro.Value = dinero;
            }
            else cmd.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);
        }
        return cmd;
    }

    private static Celular Leer(SqlDataReader lector)
    {
        var id = lector.GetInt32(0);
        return new(id, lector.GetString(1), lector.GetDecimal(2), lector.GetBoolean(3),
            lector.GetBoolean(4) ? $"/api/celulares/{id}/imagen" : null);
    }

    public async Task ComprobarConexion(CancellationToken ct)
    {
        await using var db = await Abrir(ct);
    }

    public async Task<IReadOnlyList<Celular>> Listar(CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        await using var cmd = new SqlCommand("SELECT ID_CELULAR,NOMBRE,PRECIO,ACTIVO,CAST(CASE WHEN IMAGEN IS NULL THEN 0 ELSE 1 END AS bit) FROM dbo.CELULAR WHERE ACTIVO=1 ORDER BY NOMBRE,ID_CELULAR", db);
        await using var lector = await cmd.ExecuteReaderAsync(ct);
        var celulares = new List<Celular>();
        while (await lector.ReadAsync(ct)) celulares.Add(Leer(lector));
        return celulares;
    }

    public async Task<Celular> Obtener(int id, CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        await using var cmd = Comando(db,
            "SELECT ID_CELULAR,NOMBRE,PRECIO,ACTIVO,CAST(CASE WHEN IMAGEN IS NULL THEN 0 ELSE 1 END AS bit) FROM dbo.CELULAR WHERE ID_CELULAR=@id", ("@id", id));
        await using var lector = await cmd.ExecuteReaderAsync(ct);
        if (!await lector.ReadAsync(ct)) throw new ErrorNegocio("Celular inexistente.", 404);
        return Leer(lector);
    }

    public async Task<Celular> Crear(GuardarCelular solicitud, CancellationToken ct)
    {
        var nombre = NombreValido(solicitud.Nombre);
        PrecioValido(solicitud.Precio);
        await using var db = await Abrir(ct);
        await using var cmd = Comando(db, """
            INSERT dbo.CELULAR(NOMBRE,PRECIO,ACTIVO)
            OUTPUT INSERTED.ID_CELULAR
            VALUES(@nombre,@precio,@activo)
            """, ("@nombre", nombre), ("@precio", solicitud.Precio), ("@activo", solicitud.Activo));
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        return new(id, nombre, solicitud.Precio, solicitud.Activo);
    }

    public async Task<Celular> Actualizar(int id, GuardarCelular solicitud, CancellationToken ct)
    {
        var nombre = NombreValido(solicitud.Nombre);
        PrecioValido(solicitud.Precio);
        await using (var db = await Abrir(ct))
        {
            await using var cmd = Comando(db, """
                UPDATE dbo.CELULAR SET NOMBRE=@nombre,PRECIO=@precio,ACTIVO=@activo
                WHERE ID_CELULAR=@id
                """, ("@nombre", nombre), ("@precio", solicitud.Precio), ("@activo", solicitud.Activo), ("@id", id));
            if (await cmd.ExecuteNonQueryAsync(ct) == 0) throw new ErrorNegocio("Celular inexistente.", 404);
        }
        return await Obtener(id, ct);
    }

    public async Task GuardarImagen(int id, byte[] datos, string tipo, CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        await using var cmd = new SqlCommand("UPDATE dbo.CELULAR SET IMAGEN=@imagen,IMAGEN_TIPO=@tipo WHERE ID_CELULAR=@id", db);
        cmd.Parameters.Add("@imagen", SqlDbType.VarBinary, -1).Value = datos;
        cmd.Parameters.Add("@tipo", SqlDbType.VarChar, 40).Value = tipo;
        cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
        if (await cmd.ExecuteNonQueryAsync(ct) == 0) throw new ErrorNegocio("Celular inexistente.", 404);
    }

    public async Task<(byte[] Datos, string Tipo)> ObtenerImagen(int id, CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        await using var cmd = Comando(db, "SELECT IMAGEN,IMAGEN_TIPO FROM dbo.CELULAR WHERE ID_CELULAR=@id", ("@id", id));
        await using var lector = await cmd.ExecuteReaderAsync(ct);
        if (!await lector.ReadAsync(ct)) throw new ErrorNegocio("Celular inexistente.", 404);
        if (lector.IsDBNull(0)) throw new ErrorNegocio("El celular no tiene una imagen cargada.", 404);
        if (lector.IsDBNull(1)) throw new ErrorNegocio("El tipo de imagen almacenado no es válido.", 500);
        return ((byte[])lector[0], lector.GetString(1));
    }

    public async Task QuitarImagen(int id, CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        await using var cmd = Comando(db,
            "UPDATE dbo.CELULAR SET IMAGEN=NULL,IMAGEN_TIPO=NULL WHERE ID_CELULAR=@id", ("@id", id));
        if (await cmd.ExecuteNonQueryAsync(ct) == 0) throw new ErrorNegocio("Celular inexistente.", 404);
    }

    public async Task Eliminar(int id, CancellationToken ct)
    {
        await using var db = await Abrir(ct);
        await using var cmd = Comando(db, "UPDATE dbo.CELULAR SET ACTIVO=0 WHERE ID_CELULAR=@id", ("@id", id));
        if (await cmd.ExecuteNonQueryAsync(ct) == 0) throw new ErrorNegocio("Celular inexistente.", 404);
    }

    private static string NombreValido(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new ErrorNegocio("El nombre del celular es obligatorio.", 400);
        var limpio = nombre.Trim();
        if (limpio.Length > 100) throw new ErrorNegocio("El nombre admite hasta 100 caracteres.", 400);
        return limpio;
    }

    private static void PrecioValido(decimal precio)
    {
        if (precio <= 0m || precio > 99999999.99m)
            throw new ErrorNegocio("El precio debe ser positivo y no superar 99999999.99.", 400);
        if (precio != decimal.Round(precio, 2, MidpointRounding.AwayFromZero))
            throw new ErrorNegocio("El precio admite dos decimales.", 400);
    }
}
