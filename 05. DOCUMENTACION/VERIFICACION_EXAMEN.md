# Verificación de requisitos del examen · celulares GR09

Fecha de revisión: 27 de octubre de 2026. Este documento cubre la comercializadora de celulares; BanQuito permanece como sistema REST independiente.

## Requisitos y evidencia

1. **Catálogo sin inventario.** La tienda mantiene celulares disponibles, permite alta, edición y desactivación, y no descuenta existencias. Código: `04. SERVIDOR/Comercializadora.Api/Servicios/ServicioCatalogo.cs`.
2. **Modelo de facturación.** SQL Server guarda catálogo y facturas. Modelo ERS y archivos editables PowerDesigner están en `02. MER`; scripts en `03. BDD`.
3. **Venta al contado.** Se aplica 42% de descuento al precio de lista. Pruebas de reglas y smoke API confirman total `$261,00` para precio `$450,00`.
4. **Crédito directo.** La tienda consulta elegibilidad y monto máximo a BanQuito por HTTP. Solo emite la factura después de recibir aprobación; las bases y modelos bancarios siguen fuera de esta carpeta.
5. **Factura y amortización.** La API conserva la referencia remota del crédito y consulta amortización a BanQuito. El smoke confirmó reintento idempotente y saldo final `$0,00`.
6. **Clientes.** Consola, WPF, web MVC y MAUI Android consumen la misma API REST. Los cuatro compiladores pasaron sin errores ni advertencias.
7. **Fotos de celulares.** Cada cliente puede cargar, reemplazar y quitar fotos. API en `/api/celulares/{id}/imagen`: GET, PUT multipart y DELETE. JPEG, PNG o WebP hasta 5 MiB; los bytes se guardan en SQL Server.
8. **Modelos y diagramas.** Hay ERS, ECUD de cinco casos, 15 diagramas de caso de uso/actividad/secuencia, diagramas de clases y arquitectura. Se incluyen fuentes PowerDesigner y exportaciones. MER incluye modelos conceptual, lógico y físico.
9. **Documentación.** ERS, ECUD, manual técnico y presentación se generaron usando las plantillas Word y PowerPoint GR09 existentes.
10. **Pruebas automatizadas.** `dotnet test 04. SERVIDOR/Pruebas/Comercializadora.Pruebas.csproj`: 19 aprobadas, 0 fallidas, 0 omitidas.
11. **Construcción.** API, consola, WPF, MVC web, MAUI Android y herramienta de administración compilaron correctamente; 0 advertencias y 0 errores.
12. **Imágenes sobre SQL QA aislado.** PUT carga `204`; PUT de reemplazo `204`; GET descarga bytes cuyo SHA-256 coincide; DELETE devuelve `204`; GET siguiente devuelve `404`. Respuesta incluye `Cache-Control: no-store`.
13. **Ejecución Android.** APK autónomo instalado y abierto en `emulator-5554`, AVD `GR10_Pixel_35`, paquete `ec.edu.monster.celulares.gr09`. Conexión automática a `http://10.0.2.2:19102/`, sin campo editable de servidor. Se comprobaron catálogo, temas claro/oscuro, carga/reemplazo/eliminación de fotos y venta al contado desde la interfaz: factura QA 3 aprobada por `$261,00`. También se abrió la factura de crédito QA 2 y su amortización hasta saldo `$0,00`. Los archivos descargados después de las cargas coincidieron por SHA-256. Bases utilizadas: `ComercializadoraCelulares_CellQA_20261027` y `BanQuitoCore_CellQA_20261027`.

## Decisiones técnicas

- ASP.NET Core MVC en la interfaz web; controladores, servicios, dominio y dependencias inyectadas en el servidor.
- Persistencia SQL Server con ADO.NET y parámetros. No se usa Entity Framework Core; la rúbrica del examen no obliga a un ORM específico.
- BanQuito y comercializadora son aplicaciones separadas, con comunicación REST; no se comparte base ni ensamblados.

## No comprobado en esta revisión

La ejecución e instalación se comprobaron en emulador Android; no se probó un teléfono físico. La emisión de un crédito nuevo desde el móvil no se repitió: se comprobó la consulta de un crédito existente y su amortización, además del smoke API de crédito ya registrado. Las pruebas móviles usaron bases QA aisladas, sin modificar las bases principales.
