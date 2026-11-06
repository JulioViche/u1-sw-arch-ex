using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
if (args.Length < 2 || args[0] != "--aplicar")
{
    Console.Error.WriteLine("Uso: Administracion --aplicar ruta-script.sql [otro-script.sql]");
    return 2;
}
var conexion = Environment.GetEnvironmentVariable("CELULARES_SQL_MASTER") ??
    @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;Connect Timeout=8";
try
{
    await using var db = new SqlConnection(conexion);
    await db.OpenAsync();
    foreach (var ruta in args.Skip(1))
    {
        var sql = await File.ReadAllTextAsync(ruta);
        foreach (var lote in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(lote)) continue;
            await using var cmd = new SqlCommand(lote, db) { CommandTimeout = 120 };
            await cmd.ExecuteNonQueryAsync();
        }
        Console.WriteLine($"Aplicado: {Path.GetFileName(ruta)}");
    }
    return 0;
}
catch (Exception ex) when (ex is SqlException or IOException)
{
    Console.Error.WriteLine($"No se pudo inicializar SQL Server: {ex.Message}");
    return 1;
}
