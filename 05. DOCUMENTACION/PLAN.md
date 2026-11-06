# Plan de implementación · CELULAR BDDLOCAL RESTFUL GR09

## Alcance

Implementar el sistema de la comercializadora descrito en `ExamenComplexivoPractivo_CELULARES.docx`. BanQuito es otra aplicación y conserva su propia API, reglas, base y diagramas. La comercializadora consume crédito por HTTP.

## Decisiones

- .NET 10, C# 14, REST, SQL Server 2025 Express (`SQLEXPRESS`).
- La comercializadora usa ADO.NET parametrizado; no usa Entity Framework Core. El examen deja elegir frameworks y motor.
- Web en ASP.NET Core MVC. API separa controladores, servicios y dominio, con inyección de dependencias.
- Cuatro clientes: consola, WPF, MAUI Android y web MVC. Ningún cliente accede directo a SQL Server.
- Efectivo descuenta 42%. Crédito requiere aprobación del servicio BanQuito.
- 35% para el cálculo de monto máximo, según decisión del equipo ante la diferencia entre texto y fórmula del enunciado.
- No hay inventario; cada factura corresponde a un celular.
- Fotos JPEG, PNG o WebP, hasta 5 MiB, en SQL Server `varbinary(max)`.

## Estructura

`01. UML` guarda ERS, ECUD y diagramas; `02. MER` tiene modelos conceptual, lógico y físico; `03. BDD` contiene SQL; `04. CLICON`, `CLIESC`, `CLIMOV` y `CLIWEB` son clientes; `04. COMPARTIDO` aloja contratos y transporte; `04. SERVIDOR` contiene API y dominio; `05. DOCUMENTACION` guarda manuales, presentación y evidencias.

## Estado · 27 de octubre de 2026

- [x] Mantener el proyecto aislado y no incluir archivos de BanQuito ni GR01.
- [x] Ajustar estructura numerada y agregar convenciones GR09 en `AGENTS.md`.
- [x] Completar catálogo, venta, elegibilidad, búsqueda de facturas y amortización en API y clientes.
- [x] Validar precio autoritativo y reintentos de facturación.
- [x] Implementar cargar, reemplazar y quitar imágenes desde API y cuatro clientes.
- [x] Crear migración idempotente para foto y actualizar modelos PowerDesigner.
- [x] Preparar ERS, ECUD, manual técnico y presentación desde plantillas GR09 existentes.
- [x] Revisar portadas, pies, diagramas y capturas; retirar referencias heredadas a otra solución.
- [x] Ejecutar 19 pruebas automatizadas: 19 aprobadas, 0 fallidas, 0 omitidas.
- [x] Compilar API, consola, WPF, web, MAUI Android y herramienta de administración: 0 advertencias, 0 errores.
- [x] Comprobar carga, reemplazo, descarga y eliminación de foto en una base QA aislada.
- [x] Generar paquete ZIP actualizado fuera del repositorio.
- [x] Instalar y ejecutar MAUI en emulador Android: conexión automática, temas, fotos, venta en efectivo, consulta de factura y amortización comprobados sobre SQL QA.
- Publicación: commit y push a `origin/main` autorizados; registrar el SHA remoto en el reporte final.

## Riesgos y límites

Crédito requiere iniciar BanQuito antes de probarlo. La compilación Android no equivale a instalación o prueba en dispositivo. La API de crédito usa la base independiente de BanQuito; la carpeta de la comercializadora no incluye ese proyecto.
