# Comercializadora de celulares · GR09

Sistema .NET 10 para catálogo y facturación de celulares. SQL Server 2025 Express almacena celulares, fotos y facturas. La tienda no maneja inventario.

## Límites

- La comercializadora expone REST en `http://192.168.137.1:19102/` desde la PC que comparte el hotspot de Windows.
- BanQuito es un sistema separado, en `U1/TG.1/1.4. PAO202651_RESTFUL_DOTNET_CONBDD_NRC36884_GR09`; atiende elegibilidad y crédito por HTTP en `http://127.0.0.1:19101/`.
- La web MVC usa `http://127.0.0.1:19103/`.
- La tienda guarda la referencia remota del crédito; no comparte base ni ensamblados con BanQuito.

## Ejecutar

1. Instale .NET 10 y SQL Server 2025 Express con instancia `SQLEXPRESS`.
2. Aplique `03. BDD/01_CrearBases.sql` y `02_DatosPrueba.sql`. Para una base existente, aplique también `03_MigrarImagenes.sql` y `04_MigrarDetalleFactura.sql` antes de iniciar la API.
3. Inicie BanQuito si probará compras a crédito.
4. Inicie `04. SERVIDOR/Comercializadora.Api/Comercializadora.Api.csproj`.
5. Inicie uno o más clientes: consola, WPF, MAUI Android o web MVC.

Los clientes leen `CELULARES_API_URL`; por defecto usan `http://192.168.137.1:19102/`. Para definirla en Windows para el usuario actual:

```powershell
[Environment]::SetEnvironmentVariable('CELULARES_API_URL', 'http://192.168.137.1:19102/', 'User')
```

Reinicie Visual Studio y los clientes para que reciban la variable. La PC servidor debe permitir conexiones entrantes al puerto `19102`.

`05. DOCUMENTACION/ManualTecnico.md` explica configuración y comandos. Android compila sin dispositivo con `dotnet build`; instalar requiere un emulador o dispositivo conectado.

## Funciones

- Catálogo con alta, edición, desactivación y fotos opcionales.
- Fotos JPEG, PNG o WebP de hasta 5 MiB; se pueden cargar, reemplazar y quitar desde los cuatro clientes.
- Venta en efectivo con 42% de descuento.
- Selección de varios modelos en una venta, registrados en una sola factura.
- Venta a crédito aprobada por BanQuito, consulta de facturas y amortización.
- Tema claro y oscuro en las interfaces compatibles.

## Decisiones técnicas

La web usa ASP.NET Core MVC. El servidor separa controladores, servicios y reglas de dominio; usa inyección de dependencias. SQL Server se accede con ADO.NET y comandos parametrizados; no usa Entity Framework Core. El enunciado permite elegir framework y motor de base de datos.

## Estado verificado · 27 de octubre de 2026

- 19/19 pruebas automatizadas aprobadas.
- API, consola, WPF, web MVC, MAUI Android y herramienta de administración compilaron sin errores ni advertencias.
- Flujo de imagen comprobado en una base QA aislada: carga y reemplazo `204`, descarga con bytes esperados, eliminación `204` y consulta posterior `404`.
- No se comprobó instalación ni ejecución en emulador o teléfono en esta revisión.

Consulta `05. DOCUMENTACION/AVANCE.md` y `05. DOCUMENTACION/VERIFICACION_EXAMEN.md` para alcance y evidencias.
