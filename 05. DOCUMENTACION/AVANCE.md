# Avance del proyecto de celulares GR09

## Implementado

- Comercializadora REST .NET 10, contratos, reglas de facturación, persistencia SQL Server 2025 Express con ADO.NET y consultas parametrizadas.
- Cuatro clientes MVC por plataforma: consola, escritorio WPF, web ASP.NET Core MVC y Android .NET MAUI.
- BanQuito integrado por HTTP como aplicación independiente; elegibilidad, crédito y amortización siguen siendo responsabilidad bancaria.
- Catálogo sin inventario, facturas de contado/crédito, búsqueda de facturas y consulta de amortización.
- Fotos opcionales con cargar, reemplazar y quitar desde los cuatro clientes. El servidor valida JPEG, PNG y WebP, firma del archivo y límite de 5 MiB; guarda bytes en SQL Server.
- Interfaces con identidad visual compartida; web, WPF y MAUI tienen tema claro y oscuro.
- Capas de presentación, controlador, servicio y dominio; ASP.NET Core MVC en web e inyección de dependencias en servidor.
- Decisión ORM: ADO.NET; no se usa Entity Framework Core. El enunciado permite seleccionar tecnología.

## Entregables

- `01. UML/01. ERS`: ERS Word basado en la plantilla GR09.
- `01. UML/02. ECUD`: cinco casos detallados Word, con 15 diagramas incrustados.
- `01. UML/03. DIAGRAMAS UML`: fuentes editables PowerDesigner y exportaciones para casos de uso, actividad, secuencia, clases y arquitectura.
- `02. MER`: modelos conceptual, lógico y físico PowerDesigner, con sus vistas exportadas.
- `03. BDD`: creación de la base, datos de ejemplo y migración de imagen.
- `05. DOCUMENTACION/01. MANUAL TECNICO`: manual Word y Markdown.
- `05. DOCUMENTACION/02. PRESENTACION`: presentación PowerPoint de 44 diapositivas basada en la plantilla GR09.

## Verificación local · 27 de octubre de 2026

- Pruebas automatizadas: 19 aprobadas, 0 fallidas, 0 omitidas.
- API, consola, WPF, web, MAUI Android y herramienta de administración: compilación correcta, 0 advertencias y 0 errores.
- API de imagen, sobre `ComercializadoraCelulares_CellQA_20261027` aislada: carga `204`, reemplazo `204`, bytes descargados coinciden, caché `no-store`, eliminación `204`, GET posterior `404`.
- Smoke de venta y crédito: catálogo CRUD correcto, efectivo `$261,00`, crédito aprobado, reintento sin duplicado y saldo final `$0,00` en amortización.
- Cliente Android MAUI renovado y revisado en `emulator-5554`: navegación lateral, catálogo con fotos, venta con total previo, búsqueda de facturas, amortización, tema claro/oscuro y cambio de contraste.
- Word exportado a PDF: ERS 16 páginas, ECUD 19 y manual 5. Portadas, pies, imágenes y paginación revisados; documentos sin contenido heredado de otra solución.
- PowerPoint conserva 44 diapositivas, 1 patrón y 11 diseños; paquete ZIP validado.

## Límite de verificación

El APK autónomo Android se instaló y ejecutó en `emulator-5554`. Se verificaron conexión automática, catálogo, temas claro/oscuro, fotos, venta en efectivo y consulta de factura de crédito con amortización. La venta móvil generó la factura QA 3 por `$261,00`. No se probó teléfono físico ni se repitió la emisión de un crédito nuevo desde Android. Todas las operaciones móviles usaron bases QA aisladas; no alteraron las bases principales.

## Entrega

El paquete final se genera fuera del repositorio en `D:\tools\CelularGR09\Entrega_Examen_GR09_2026-10-27.zip`. El equipo no tiene unidad CD/DVD.
