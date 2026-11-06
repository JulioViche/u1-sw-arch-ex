@echo off
chcp 65001 > nul
echo ===================================================================
echo   HERRAMIENTA DE REINICIO DE BASE DE DATOS - EXAMEN CELULARES
echo ===================================================================
echo.
echo Seleccione una opción:
echo   [1] Liberar créditos y ventas (Instantáneo: 1 segundo, no apaga Docker)
echo       - Todos los clientes vuelven a ser elegibles para crédito.
echo       - Conserva el catálogo de celulares y sus fotos.
echo.
echo   [2] Reinicio TOTAL de Docker (Desde cero con scripts iniciales)
echo       - Elimina volúmenes y recrea las bases Comercializadora y BanQuito.
echo.
set /p opcion="Ingrese opción (1 o 2) [default=1]: "
if "%opcion%"=="" set opcion=1

if "%opcion%"=="1" (
    echo.
    echo Limpiando ventas y liberando créditos en SQL Server...
    wsl docker exec examen_sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Examen2026*Celulares" -C -Q "SET QUOTED_IDENTIFIER ON; DELETE FROM BanQuitoCore.dbo.AMORTIZACION; DELETE FROM BanQuitoCore.dbo.CREDITO; DELETE FROM ComercializadoraCelulares.dbo.FACTURA; SELECT '¡LISTO! Todos los clientes están limpios y son elegibles nuevamente.' AS Estado;"
    echo.
    pause
    exit /b
)

if "%opcion%"=="2" (
    echo.
    echo Reiniciando contenedores y volúmenes de Docker...
    wsl docker compose down -v
    wsl docker compose up -d
    echo.
    echo ¡Docker reiniciado! SQL Server e inicialización ejecutados con éxito.
    echo.
    pause
    exit /b
)

echo Opción no válida.
pause

