param(
    [string]$Conexion
)

$ErrorActionPreference = 'Stop'
$anterior = $env:CELULARES_SQL_MASTER
try {
    if ([string]::IsNullOrWhiteSpace($Conexion)) {
        $Conexion = $env:ConnectionStrings__BaseDatos
    }
    if ([string]::IsNullOrWhiteSpace($Conexion)) {
        $config = Get-Content -LiteralPath (Join-Path $PSScriptRoot '04. SERVIDOR\Comercializadora.Api\appsettings.json') -Raw | ConvertFrom-Json
        $Conexion = $config.ConnectionStrings.BaseDatos
    }
    if ([string]::IsNullOrWhiteSpace($Conexion)) { throw 'No se encontro la conexion de la API.' }
    $datos = [System.Data.Common.DbConnectionStringBuilder]::new()
    $datos.set_ConnectionString($Conexion)
    $base = if ($datos.ContainsKey('Database')) { $datos['Database'] } else { $datos['Initial Catalog'] }
    if ($base -ne 'ComercializadoraCelulares') { throw 'La conexion debe apuntar a la base ComercializadoraCelulares.' }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'No se encontro dotnet en el PATH.' }

    $env:CELULARES_SQL_MASTER = $Conexion
    Push-Location $PSScriptRoot
    try {
        & dotnet run --project '.\04. SERVIDOR\Administracion\Administracion.csproj' -- --aplicar '.\03. BDD\04_MigrarDetalleFactura.sql'
        if ($LASTEXITCODE -ne 0) { throw 'No se aplico la migracion. Revise la conexion de SQL Server y vuelva a intentarlo.' }
    }
    finally { Pop-Location }
    Write-Host 'Migracion de facturas aplicada correctamente.' -ForegroundColor Green
}
catch {
    Write-Error $_
    exit 1
}
finally {
    $env:CELULARES_SQL_MASTER = $anterior
}
