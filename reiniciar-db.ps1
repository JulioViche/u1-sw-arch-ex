# Script de reinicio de base de datos para el examen
param (
    [switch]$Total
)

Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  HERRAMIENTA DE REINICIO DE BASE DE DATOS - EXAMEN CELULARES" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

if ($Total) {
    Write-Host "Ejecutando reinicio TOTAL de Docker (borrado de volumen mssql_data)..." -ForegroundColor Yellow
    wsl docker compose down -v
    wsl docker compose up -d
    Write-Host "¡Docker reiniciado y bases creadas desde cero!" -ForegroundColor Green
    exit 0
}

Write-Host "Liberando créditos y facturas de prueba (instantáneo)..." -ForegroundColor Yellow
wsl docker exec examen_sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Examen2026*Celulares" -C -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM BanQuitoCore.dbo.AMORTIZACION; DELETE FROM BanQuitoCore.dbo.CREDITO; DELETE FROM ComercializadoraCelulares.dbo.FACTURA; SELECT '¡LISTO! Todos los clientes vuelven a ser elegibles para crédito.' AS Estado;"
Write-Host ""
Write-Host "¡Completado! Todos los clientes de prueba vuelven a calificar en BanQuito." -ForegroundColor Green
Write-Host "Para un reinicio total de contenedores use: .\reiniciar-db.ps1 -Total" -ForegroundColor DarkGray

