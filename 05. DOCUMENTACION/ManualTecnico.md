# Manual técnico

.NET SDK 10 está en `D:\tools\dotnet10`; los paquetes se guardan en `D:\NuGet\packages`. La base de datos requerida es SQL Server 2025 Express en `SQLEXPRESS`. La comercializadora persiste con ADO.NET y parámetros SQL; no usa Entity Framework Core. La web sigue ASP.NET Core MVC; la API separa controladores, servicios y dominio, con inyección de dependencias.

## Instalar SQL Server

Abrir PowerShell como administrador y ejecutar desde esta carpeta:

```powershell
& '.\03. BDD\InstalarSQL2025.ps1' -Media 'D:\Downloads\sqlserver2025\engine\SETUP.EXE'
```

El instalador verifica la firma Microsoft y no reemplaza una instancia SQLEXPRESS existente. Revisar su resultado antes de continuar.

## Inicializar las bases

Desde esta carpeta, en una terminal normal:

```powershell
$dotnet = 'D:\tools\dotnet10\dotnet.exe'
$banco = '..\..\TG.1\1.4. PAO202651_RESTFUL_DOTNET_CONBDD_NRC36884_GR09'
& $dotnet run --project (Join-Path $banco '01. SERVIDOR\BanQuito.Administracion\BanQuito.Administracion.csproj') -- --aplicar (Join-Path $banco '03. BDD\BanQuito_01_CrearBase.sql') (Join-Path $banco '03. BDD\BanQuito_02_DatosPrueba.sql')
& $dotnet run --project '.\04. SERVIDOR\Administracion\Administracion.csproj' -- --aplicar '.\03. BDD\01_CrearBases.sql' '.\03. BDD\02_DatosPrueba.sql'
```

Los inicializadores usan autenticación Windows y `SQLEXPRESS`. Los scripts son idempotentes; revise los datos existentes antes de aplicarlos. Si la base comercializadora ya existe, ejecute `03. BDD/03_MigrarImagenes.sql` para añadir columnas de imagen y `03. BDD/04_MigrarDetalleFactura.sql` para crear el detalle de las facturas y conservar sus datos anteriores. Aplique esta migración antes de iniciar la API actualizada.

Configure `CELULARES_SQL_MASTER` con la conexión a `master` de la misma instancia SQL que usa la API y ejecute:

```powershell
& $dotnet run --project '.\04. SERVIDOR\Administracion\Administracion.csproj' -- --aplicar '.\03. BDD\04_MigrarDetalleFactura.sql'
```

## Iniciar servicios y clientes

Desde la carpeta del proyecto, en terminales separadas:

```powershell
& 'D:\tools\dotnet10\dotnet.exe' run --project '..\..\TG.1\1.4. PAO202651_RESTFUL_DOTNET_CONBDD_NRC36884_GR09\01. SERVIDOR\BanQuito.Api\BanQuito.Api.csproj'
& 'D:\tools\dotnet10\dotnet.exe' run --project '.\04. SERVIDOR\Comercializadora.Api\Comercializadora.Api.csproj'
& 'D:\tools\dotnet10\dotnet.exe' run --project '.\04. CLIWEB\Celulares.Web\Celulares.Web.csproj'
```

BanQuito escucha en `127.0.0.1:19101`, la API de la tienda en `http://192.168.137.1:19102/` desde la PC servidor y la web en `http://127.0.0.1:19103`. Los clientes consola, escritorio, web y móvil usan `CELULARES_API_URL` cuando está definida; el valor predeterminado es la dirección del hotspot Windows. También se puede ejecutar `04. CLICON/Celulares.Consola` o `04. CLIESC/Celulares.Escritorio` con `dotnet run --project` y su archivo `.csproj`.

La API escucha en `0.0.0.0:19102` para aceptar conexiones desde la red. En el hotspot Windows, use `http://192.168.137.1:19102/`. La variable `CELULARES_API_URL` permite cambiar esta dirección para todos los clientes; en móvil también se puede configurar desde la interfaz. Permita el puerto `19102` en el firewall de la PC servidor.

## Compilar Android sin instalar

`dotnet build` compila MAUI y no requiere dispositivo conectado. Use:

```powershell
& 'D:\tools\dotnet10\dotnet.exe' build '.\04. CLIMOV\Celulares.Movil\Celulares.Movil.csproj' -f net10.0-android -p:AndroidSdkDirectory='D:\tools\android-sdk'
```

Para instalar y abrir en emulador, primero confirme un destino en `adb devices`, luego ejecute MSBuild `Run`:

```powershell
& 'D:\tools\android-sdk\platform-tools\adb.exe' devices
& 'D:\tools\dotnet10\dotnet.exe' build '.\04. CLIMOV\Celulares.Movil\Celulares.Movil.csproj' -t:Run -p:AndroidSdkDirectory='D:\tools\android-sdk'
```

Sin emulador o teléfono conectado, no ejecute `-t:Run`; la compilación independiente funciona sin destino. Una instalación manual de APK Debug puede omitir los ensamblados Fast Deployment.

## Imágenes

`GET /api/celulares/{id}/imagen` descarga la foto; `PUT` recibe `multipart/form-data` y carga o reemplaza; `DELETE` quita solo la foto. El servidor valida JPEG, PNG o WebP, firma del archivo y máximo de 5 MiB. Los bytes y tipo MIME se guardan en SQL Server.

Los cuatro clientes permiten administrar fotos. Si falla una carga, revise la migración, tamaño, formato, permisos SQL y disponibilidad de la API.

## Operación

La API expone catálogo, facturas, elegibilidad y amortización. BanQuito debe estar disponible para validar compras de crédito. Una operación pendiente debe reintentarse con el mismo identificador para evitar duplicados. No registre credenciales, cadenas de conexión ni datos personales en capturas o logs.
