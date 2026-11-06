$ErrorActionPreference = 'Stop'
$toolsRoot = 'D:\tools'
$env:DOTNET_ROOT = Join-Path $toolsRoot 'dotnet10'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_HOME = Join-Path $toolsRoot 'dotnet-cli'
$env:NUGET_PACKAGES = 'D:\NuGet\packages'
$env:TEMP = Join-Path $toolsRoot 'dotnet-temp'
$env:TMP = $env:TEMP
$env:CelularBuildRoot = Join-Path $toolsRoot 'CelularGR09'
$env:CelularPackagesRoot = $env:NUGET_PACKAGES
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'No se encuentra dotnet en el PATH. Abra una terminal nueva y vuelva a intentarlo.'
}
foreach ($ruta in @($env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES, $env:TEMP, $env:CelularBuildRoot)) {
    New-Item -ItemType Directory -Force -Path $ruta | Out-Null
}
