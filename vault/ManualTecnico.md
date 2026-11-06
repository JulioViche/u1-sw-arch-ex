CELULAR BDDLOCAL RESTFUL · GRUPO 09

GRUPO 09 · NRC 36884


# MANUAL TÉCNICO

Clientes .NET · REST

Protocolo: REST

Tecnología: .NET 10

Base de datos: SQL Server 2025 Express

27 de octubre de 2026


# 1. Propósito y alcance

Este manual documenta la arquitectura, instalación, uso y mantenimiento del sistema de ventas de celulares. Permite administrar el catálogo y las fotos, registrar ventas de contado o crédito, y consultar facturas y amortización. Los clientes consumen la API; SQL Server solo recibe conexiones del servidor.


# 2. Arquitectura y tecnologías

Servidor: ASP.NET Core REST sobre .NET 10. Persistencia: SQL Server 2025 Express con ADO.NET. Clientes: consola, escritorio WPF, web ASP.NET Core MVC y móvil .NET MAUI para Android. BanQuito es un servicio REST remoto e independiente.


| Capa | Responsabilidad |
| --- | --- |
| Vista | Captura catálogo, datos del cliente, selección de imagen y resultados. |
| Controlador | Valida acciones, confirma altas y coordina servicios. |
| Modelo | Representa celular, foto, factura y crédito. |
| Cliente de servicio | Envía REST, carga bytes y lee respuestas. |
| Servidor y repositorio | Valida reglas, consulta BanQuito y persiste mediante ADO.NET. |


La web usa MVC. Consola, WPF y MAUI separan vista, controlador y servicio. El servidor separa controladores, reglas de dominio, servicios y acceso SQL.


# 3. Contrato de comunicación

La tienda intercambia JSON mediante api/celulares, api/facturas y api/credito. Las fotos usan GET, PUT multipart/form-data y DELETE en /api/celulares/{id}/imagen. BanQuito recibe solicitudes HTTP separadas; sus datos no se guardan en la base de la tienda.


| Método y recurso | Uso |
| --- | --- |
| GET /api/celulares · POST /api/celulares | Listar catálogo o crear celular. |
| PUT /api/celulares/{id} · DELETE /api/celulares/{id} | Editar o desactivar celular. |
| GET · PUT multipart · DELETE /api/celulares/{id}/imagen | Consultar, cargar/reemplazar o quitar foto. |
| GET /api/credito/elegibilidad?cedula={cedula} | Consultar elegibilidad en BanQuito. |
| GET/POST /api/facturas · GET /api/facturas/{id}/amortizacion | Registrar y consultar factura o amortización. |


Configure ConnectionStrings:BaseDatos para SQL Server y Servicios:BanQuitoUrl para el servicio bancario. Configure la dirección de la API en cada cliente. Use nombres alcanzables desde el navegador, la red o el emulador Android.


# 4. Reglas de venta e imágenes

Contado: descuento del 42% sobre precio de lista. Crédito: elegibilidad bancaria, plazo de 3 a 18 meses y tasa anual fija de 16,5%. La factura se registra solo después de la aprobación de BanQuito.

Cada factura corresponde a un celular. El examen no define inventario, por lo que la venta no descuenta existencias.

La foto es opcional. Se puede cargar, reemplazar o quitar desde consola, WPF, web y Android. Formatos: JPEG, PNG y WebP; máximo 5 MiB.

El servidor valida tamaño, Content-Type y firma del archivo. Persiste los bytes en SQL Server como varbinary(max) y conserva el tipo MIME. El cliente no conecta a la base de datos.

Al quitar una foto se elimina solo el archivo asociado. No se elimina el celular ni su historial de facturas.

Si el crédito es rechazado o BanQuito no responde, la tienda no marca la venta como exitosa ni emite una factura de crédito.

Las consultas SQL son parametrizadas. Mantenga la cadena de conexión fuera del repositorio, capturas y registros.


# 5. Persistencia y MER

Los modelos conceptual, lógico y físico PowerDesigner muestran las entidades comerciales, claves y relaciones. El modelo físico usa SQL Server e incluye los bytes y tipo MIME de la foto. Los archivos nativos editables acompañan a cada imagen.

Ilustración 1. Modelo conceptual de celulares, ventas e imágenes.

Ilustración 2. Modelo lógico con atributos y claves.

Ilustración 3. Modelo físico SQL Server con imagen varbinary(max).


# 6. Configuración y construcción

Defina ConnectionStrings__BaseDatos y Servicios__BanQuitoUrl mediante configuración segura. Aplique 01_CrearBases.sql y los datos de ejemplo. Para una base existente, aplique 03_MigrarImagenes.sql antes de iniciar la API.

Desde el servidor, ejecute dotnet test en Pruebas y dotnet run en Comercializadora.Api. Inicie primero BanQuito cuando pruebe crédito. Luego ejecute consola, WPF o web. MAUI requiere emulador o dispositivo Android para instalar y abrir.

El usuario de SQL Server necesita permisos sobre la base comercializadora. El servicio BanQuito requiere URL accesible. No comparta permisos SQL con los cuatro clientes.


# 7. Captura de interfaz

Catálogo web en tema oscuro. La tarjeta muestra precio, estado y acciones de foto para cargar, reemplazar o quitar. También hay tema claro.


# 8. Diagnóstico y soporte


| Síntoma | Verificación |
| --- | --- |
| Foto no carga | Ejecute 03_MigrarImagenes.sql; valide JPEG, PNG o WebP y máximo 5 MiB. |
| Catálogo vacío | Revise URL de API, SQL Server SQLEXPRESS y tabla CELULAR. |
| Venta a crédito rechazada | Compruebe elegibilidad y conexión independiente a BanQuito. |
| API no accesible desde Android | Use IP alcanzable desde emulador/dispositivo; localhost apunta al propio dispositivo. |
| MAUI no instala | Inicie emulador o conecte un dispositivo; build no instala sin destino. |


No registre credenciales, cadenas de conexión ni datos personales. Registre fecha, ruta, código HTTP e identificador técnico. Si la carga falla, confirme migración, tamaño, formato y disponibilidad de SQL Server.
