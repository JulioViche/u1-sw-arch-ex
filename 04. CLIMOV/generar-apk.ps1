[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  GENERADOR DE APK - CLIENTE MOVIL CELULARES GR09" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Compilando y empaquetando APK Release firmado..." -ForegroundColor Yellow

$proyecto = Join-Path $PSScriptRoot "Celulares.Movil\Celulares.Movil.csproj"
$destino = Join-Path $PSScriptRoot "APK"

dotnet publish $proyecto -f net10.0-android -c Release -p:AndroidPackageFormat=apk -p:AndroidKeyStore=false

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERROR] La generación del APK ha fallado." -ForegroundColor Red
    exit $LASTEXITCODE
}

if (-not (Test-Path $destino)) {
    New-Item -ItemType Directory -Path $destino -Force | Out-Null
}

$apkGenerado = Get-ChildItem -Path (Join-Path $PSScriptRoot "Celulares.Movil\bin\Release\net10.0-android\publish\*-Signed.apk") | Select-Object -First 1
if ($apkGenerado) {
    Copy-Item $apkGenerado.FullName $destino -Force
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Green
    Write-Host "  ¡APK GENERADO CORRECTAMENTE!" -ForegroundColor Green
    Write-Host "  Ubicación: $destino\$($apkGenerado.Name)" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
}
