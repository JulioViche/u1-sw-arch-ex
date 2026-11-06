@echo off
chcp 65001 > nul
echo ========================================================
echo   GENERADOR DE APK - CLIENTE MOVIL CELULARES GR09
echo ========================================================
echo.
echo Compilando y empaquetando APK Release firmado...
echo.

cd /d "%~dp0Celulares.Movil"
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk -p:AndroidKeyStore=false

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] La generación del APK ha fallado.
    pause
    exit /b %ERRORLEVEL%
)

if not exist "..\APK" mkdir "..\APK"
copy /y "bin\Release\net10.0-android\publish\*-Signed.apk" "..\APK\" > nul

echo.
echo ========================================================
echo   ¡APK GENERADO CORRECTAMENTE!
echo   Ubicación: 04. CLIMOV\APK\
echo ========================================================
echo.
dir "..\APK\*.apk"
echo.
pause

