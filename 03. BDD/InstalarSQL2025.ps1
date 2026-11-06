#Requires -RunAsAdministrator
param(
    [Parameter(Mandatory=$true)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$Media,
    [string]$Usuario = [Security.Principal.WindowsIdentity]::GetCurrent().Name
)
$ErrorActionPreference = 'Stop'
$toolsRoot = 'D:\tools'
$sqlInstallRoot = Join-Path $toolsRoot 'SQLServer2025'
$dataRoot = Join-Path $toolsRoot 'sqlserver2025-data'
$logs = Join-Path $toolsRoot 'CelularGR09\sql-setup'
foreach ($ruta in @($logs,$sqlInstallRoot,(Join-Path $dataRoot 'data'),(Join-Path $dataRoot 'log'),(Join-Path $dataRoot 'temp'),(Join-Path $dataRoot 'backup'))) {
    New-Item -ItemType Directory -Force -Path $ruta | Out-Null
}
Start-Transcript -Path (Join-Path $logs 'instalacion.log') -Append
try {
    if (Get-Service -Name 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue) { throw 'SQLEXPRESS ya existe; no se reemplaza automáticamente.' }
    $firma = Get-AuthenticodeSignature -LiteralPath $Media
    if ($firma.Status -ne 'Valid' -or $firma.SignerCertificate.Subject -notmatch 'Microsoft Corporation') { throw 'No se pudo validar la firma Microsoft del instalador.' }
    $argumentos = @(
        '/Q','/ACTION=Install','/FEATURES=SQLENGINE','/INSTANCENAME=SQLEXPRESS',
        '/IACCEPTSQLSERVERLICENSETERMS','/SQLSVCSTARTUPTYPE=Automatic','/TCPENABLED=0','/NPENABLED=0',
        ('/INSTANCEDIR="{0}"' -f $sqlInstallRoot),
        ('/INSTALLSHAREDDIR="{0}"' -f (Join-Path $sqlInstallRoot 'Shared')),
        ('/INSTALLSHAREDWOWDIR="{0}"' -f (Join-Path $sqlInstallRoot 'Shared32')),
        ('/INSTALLSQLDATADIR="{0}"' -f (Join-Path $dataRoot 'data')),
        ('/SQLUSERDBDIR="{0}"' -f (Join-Path $dataRoot 'data')),
        ('/SQLUSERDBLOGDIR="{0}"' -f (Join-Path $dataRoot 'log')),
        ('/SQLTEMPDBDIR="{0}"' -f (Join-Path $dataRoot 'temp')),
        ('/SQLTEMPDBLOGDIR="{0}"' -f (Join-Path $dataRoot 'temp')),
        ('/SQLBACKUPDIR="{0}"' -f (Join-Path $dataRoot 'backup')),
        ('/SQLSYSADMINACCOUNTS="{0}"' -f $Usuario)
    )
    $p = Start-Process -FilePath $Media -WindowStyle Hidden -ArgumentList $argumentos -PassThru -Wait
    @{ exitCode=$p.ExitCode; time=(Get-Date -Format o) } | ConvertTo-Json | Set-Content (Join-Path $logs 'resultado.json')
    if ($p.ExitCode -notin @(0,3010)) { throw "SQL Setup devolvió $($p.ExitCode). Revisar Summary.txt de SQL Setup." }
    Get-Service -Name 'MSSQL$SQLEXPRESS'
} finally { Stop-Transcript }
