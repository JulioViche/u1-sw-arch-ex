ESPECIFICACION DE REQUERIMIENTOS: ERS CELULAR BDDLOCAL RESTFUL DOTNET GR09

ÍNDICE DE ILUSTRACIONES

No se encuentran elementos de tabla de ilustraciones.

ÍNDICE DE TABLAS


# No se encuentran elementos de tabla de ilustraciones.


# CASO DE ESTUDIO

La comercializadora mantiene un catálogo de celulares, registra ventas de contado o crédito y consulta facturas y amortizaciones. BanQuito es un servicio bancario separado que evalúa y otorga el crédito directo.

.NET 10 y ASP.NET Core REST con SQL Server 2025 Express. Cuatro clientes consumen la API: consola, escritorio WPF, web ASP.NET Core MVC y Android .NET MAUI. Ningún cliente conecta directamente con SQL Server.

La venta en efectivo descuenta 42%. La venta a crédito requiere elegibilidad vigente, monto suficiente y aprobación remota antes de emitir factura. El sistema no administra inventario y cada factura corresponde a un celular.


# 1. Introducción

Este documento fija requisitos funcionales, reglas de negocio y criterios de calidad para la comercializadora de celulares del grupo 09.

La API controla validaciones, facturación y persistencia. Los cuatro clientes usan el mismo contrato REST; el módulo BanQuito conserva el cálculo y registro del crédito.


# 2. Propósito

Definir requisitos verificables para catálogo, facturación de contado y crédito, consulta de facturas y amortización en los cuatro clientes .NET.

La especificación guía diseño, implementación, pruebas y aceptación. Distingue la base de la tienda del servicio de crédito BanQuito.


# 3. Alcance

El sistema permite crear, editar y desactivar celulares, cargar, reemplazar y quitar fotos, registrar ventas y consultar facturas. BanQuito determina elegibilidad y monto máximo.

Quedan fuera el inventario, el Core bancario y la administración de clientes del banco. La comercializadora solo consume el contrato HTTP del módulo BanQuito.

Los clientes no acceden a la base de datos. Una venta a crédito se confirma solo después de recibir aprobación; una falla del banco no se presenta como venta exitosa.


# 4. Definiciones, Acrónimos y Abreviaturas


## 4.1 Definiciones

Tabla 1. Definiciones


| TÉRMINO | DEFINICIÓN |
| --- | --- |
| Celular | Equipo comercializado con marca, modelo, precio y características. |
| Catálogo | Conjunto de celulares activos ofrecidos por la comercializadora. |
| Factura | Documento de una venta asociado a un solo celular. |
| Precio de lista | Precio vigente antes del descuento o financiamiento. |
| Venta en efectivo | Venta con descuento del 42% sobre precio de lista. |
| Crédito directo | Financiamiento registrado por el servicio bancario BanQuito. |
| Elegibilidad | Resultado bancario que determina si el cliente puede solicitar crédito. |
| Monto máximo | Límite aprobado según depósitos y retiros del cliente. |
| Amortización | Plan de pagos con capital, interés, cuota y saldo. |
| Imagen del celular | Archivo JPEG, PNG o WebP opcional de hasta 5 MiB. |
| Operador comercial | Usuario que administra catálogo y registra ventas. |
| BanQuito | Servicio remoto separado que evalúa y registra crédito. |
| Tipo MIME | Identificador del formato de archivo comunicado a la API. |
| varbinary(max) | Tipo SQL Server para persistir los bytes de la imagen. |
| API REST | Interfaz HTTP que comparten los cuatro clientes. |
| Transacción | Unidad de cambios de base de datos confirmada o revertida en conjunto. |
| ADO.NET | Tecnología .NET de acceso parametrizado a SQL Server. |
| SQL Server Express | Motor relacional obligatorio para la comercializadora. |
| CRUD | Operaciones de alta, consulta, actualización y desactivación. |
| Desactivación | Cambio del celular a estado inactivo sin borrar su historial. |
| Aprobación bancaria | Respuesta exitosa de BanQuito que autoriza registrar una venta a crédito. |



## 4.2 Acrónimos y abreviaturas

Tabla 2. Acrónimos y abreviaturas


| Acrónimo/Abreviatura | Significado |
| --- | --- |
| API | Interfaz de programación de aplicaciones. |
| ADO.NET | Acceso a datos de .NET mediante conexiones, comandos y parámetros. |
| BDD | Base de datos. |
| CRUD | Crear, consultar, actualizar y desactivar registros. |
| ECUD | Especificación de casos de uso detallado. |
| ERS | Especificación de requisitos de software. |
| MVC | Modelo, vista y controlador. |
| REST | Estilo de arquitectura para servicios HTTP. |
| SQL Server | Motor relacional usado por la comercializadora. |



# 5. Referencias

La especificación se basa en el enunciado del examen, el modelo de datos, el contrato REST implementado y criterios de calidad de software.

Tabla 3. Referencias


| Referencia | Título | Descripción | Entidad | Año |
| --- | --- | --- | --- | --- |
| 1 | Examen práctico de celulares | Requisitos funcionales y reglas del crédito directo. | Grupo 09 | 2026 |
| 2 | Modelo de datos comercializadora | Tablas CELULAR y FACTURA; el crédito vive en BanQuito. | Grupo 09 | 2026 |
| 3 | Documentación ASP.NET Core y ADO.NET | Tecnologías utilizadas por el servidor y los clientes. | Microsoft | 2026 |
| 4 | Documentación de SQL Server | Tipos de datos, integridad y almacenamiento varbinary(max). | Microsoft | 2026 |



# 6. Visión General del Documento

El documento describe alcance, entorno, interfaces y restricciones; luego fija requisitos funcionales, reglas de crédito, calidad, seguridad y rendimiento.

El ECUD detalla cinco casos con diagramas de casos de uso, actividad y secuencia.


# 7. Descripción General


## 7.1 Perspectiva del Producto

La solución separa la tienda de BanQuito. Cada cliente organiza vista, controlador y servicio; la API comercializadora valida solicitudes, aplica reglas y persiste con ADO.NET.

La API REST de la tienda usa SQL Server. Para crédito, llama por HTTP al módulo BanQuito y conserva DTO propios; no comparte su base de datos.

La arquitectura separa clientes, transporte REST, reglas de facturación, acceso ADO.NET y SQL Server.

Ilustración 1. Diagrama de arquitectura cliente-servidor de la comercializadora


## 7.2 Funciones del Producto (software)

Las funciones son mantener catálogo e imágenes, consultar elegibilidad, vender al contado, vender a crédito, emitir factura y consultar amortización.


## 7.3 Condiciones del Entorno

Servidor: .NET 10 y ASP.NET Core; persistencia SQL Server 2025 Express. Clientes: consola, WPF, ASP.NET Core MVC y Android .NET MAUI. BanQuito se configura como servicio HTTP independiente.


## 7.4 Características de los Usuarios

Tabla 4. Usuario del sistema


| Tipo de usuario | Operador de la comercializadora |
| --- | --- |
| Formación | Capacitación básica en ventas y manejo del sistema. |
| Habilidades | Identifica productos, registra datos del comprador y selecciona la forma de pago. |
| Actividades | Administra el catálogo y fotos, registra ventas y consulta facturas. |



## 7.5 Interfaces Externas

El operador usa consola, escritorio, web o móvil. Los cuatro canales exponen catálogo, foto, venta, factura y consulta según sus controles.

La API se comunica con SQL Server mediante ADO.NET y con BanQuito por REST. No se usa Entity Framework Core.


## 7.6 Restricciones

La solución requiere .NET 10 y SQL Server. La web usa ASP.NET Core MVC; consola, WPF y MAUI separan vista, controlador y cliente de servicio. La persistencia usa ADO.NET.


## 7.7 Suposiciones y Dependencias

El servicio BanQuito debe estar disponible para validar y otorgar créditos. La API de tienda valida el precio vigente y no registra la venta si el resultado remoto es incierto.


# 8. Especificación de Requerimientos


## 8.1 Requisitos comunes de las interfaces

Los canales muestran estado, errores de validación y resultado. Las acciones de imagen explican formato y límite de tamaño.


### 8.1.1 Interfaces de usuario

La web ofrece vista previa antes de guardar; WPF y MAUI muestran la foto seleccionada. Consola identifica la foto por producto y ruta de archivo.


### 8.1.2 Interfaces de hardware

Tabla 5. Requisitos de interfaces de hardware


| Componente | Uso | Perfil | Procesador | Memoria | Red / almacenamiento |
| --- | --- | --- | --- | --- | --- |
| Servidor API | Ejecuta ASP.NET Core REST y reglas de facturación. | .NET 10 | CPU compatible con .NET 10 | 4 GB RAM | Acceso IP a SQL Server y BanQuito |
| SQL Server | Persiste catálogo, fotos, facturas y eventos de operación. | SQL Server 2025 Express | Instancia SQLEXPRESS | 4 GB RAM recomendados | Almacenamiento persistente y conexión local |
| Cliente consola | Opera catálogo y ventas en terminal. | .NET 10 | Equipo de oficina | 2 GB RAM | Acceso HTTP a la API |
| Cliente escritorio | Interfaz de catálogo, imagen, facturación y consultas. | WPF · Windows | CPU compatible | 2 GB RAM | Acceso HTTP a la API |
| Cliente web | Interfaz MVC accesible desde navegador. | ASP.NET Core MVC | Navegador moderno | 2 GB RAM | Conexión a la API |
| Cliente móvil | Interfaz MAUI para Android. | Android 6.0 o posterior | Dispositivo compatible | 2 GB RAM | Red al alcance de la API |
| Almacenamiento de imágenes | Guarda archivos de hasta 5 MiB por producto. | SQL Server varbinary(max) | Firma JPEG/PNG/WebP | Depende del tamaño | API limita carga a 5 MiB |



### 8.1.3 Interfaces de software

Tabla 6. Interfaces de software


| Componente | Tecnología / versión | Interfaz | Propósito |
| --- | --- | --- | --- |
| Servidor comercializador | .NET 10 · ASP.NET Core | API REST JSON | Catálogo, imágenes y facturación |
| Base de datos | SQL Server 2025 Express | ADO.NET | Persistencia parametrizada de tienda |
| Servicio bancario | BanQuito REST | HTTP JSON | Elegibilidad, crédito y amortización |
| Cliente compartido | C# · HttpClient | Servicios y DTO | Comunicación común con la API de tienda |
| Cliente de consola | .NET 10 · Console | Menú de texto | Operación de catálogo, fotos y ventas |
| Cliente de escritorio | .NET 10 · WPF | Vista de escritorio | Administración de catálogo y venta |
| Cliente web | ASP.NET Core MVC | Controlador y vistas | Tienda web con tema claro y oscuro |
| Cliente móvil | .NET MAUI Android | Picker, vistas y controladores | Foto, catálogo y venta desde Android |
| Modelado | PowerDesigner 16.1 | .oom, .eam, .cdm, .ldm, .pdm | Diagramas editables GR09 |
| Imágenes | JPEG, PNG y WebP · hasta 5 MiB | GET/PUT/DELETE /api/celulares/{id}/imagen | Almacenar, reemplazar y quitar la foto |



### 8.1.4 Interfaces de comunicación

Los clientes usan HTTP y JSON. La API de tienda expone catálogo, imágenes, elegibilidad, facturas y amortización; BanQuito conserva endpoints propios.

API de tienda: http://192.168.137.1:19102/ en el hotspot Windows, configurable con CELULARES_API_URL. Web: http://127.0.0.1:19103/ . BanQuito: http://127.0.0.1:19101/.


## 8.2 Requisitos Funcionales


### 8.2.1. Objetivo General

Permitir ventas correctas y trazables al separar catálogo, facturación, reglas de crédito, persistencia local e integración bancaria.


### 8.2.2. Objetivos Específicos

Tabla 7. Administrar catálogo e imágenes


| OBJ-001 | Administrar catálogo e imágenes |
| --- | --- |
| Descripción | Crear, editar y desactivar celulares; cargar, reemplazar y quitar fotos JPEG, PNG o WebP de hasta 5 MiB. |
| Importancia | Alta |
| Comentarios | Disponible desde consola, WPF, web MVC y MAUI Android. |


Tabla 8. Verificar elegibilidad de crédito


| OBJ-002 | Verificar elegibilidad de crédito |
| --- | --- |
| Descripción | Consultar en BanQuito los requisitos del cliente y el monto máximo antes de ofrecer crédito. |
| Importancia | Alta |
| Comentarios | Disponible desde consola, WPF, web MVC y MAUI Android. |


Tabla 9. Registrar venta en efectivo


| OBJ-003 | Registrar venta en efectivo |
| --- | --- |
| Descripción | Calcular el descuento del 42% y emitir una factura por el celular elegido. |
| Importancia | Alta |
| Comentarios | Disponible desde consola, WPF, web MVC y MAUI Android. |


Tabla 10. Registrar venta a crédito


| OBJ-004 | Registrar venta a crédito y factura |
| --- | --- |
| Descripción | Validar elegibilidad, precio máximo y plazo; solicitar aprobación bancaria y emitir factura solo ante éxito. |
| Importancia | Alta |
| Comentarios | Disponible desde consola, WPF, web MVC y MAUI Android. |


Tabla 11. Consultar factura y amortización


| OBJ-005 | Consultar factura y amortización |
| --- | --- |
| Descripción | Buscar facturas por cédula, consultar detalle y mostrar amortización del crédito aprobado. |
| Importancia | Alta |
| Comentarios | Disponible desde consola, WPF, web MVC y MAUI Android. |



### 8.2.3. Actores

Tabla 12. Actor: operador comercial


| ACT-001 | Operador comercial |
| --- | --- |
| Descripción | Usuario que mantiene catálogo y fotos, registra ventas y consulta facturas. |
| Comentarios | La venta a crédito depende del servicio BanQuito. |



### 8.2.4. Lista de requisitos funcionales

La lista consolida las cinco capacidades de la comercializadora y su relación con el operador.

El diagrama siguiente resume los casos de uso del sistema de ventas.

Ilustración 2. Diagrama de casos de uso de venta de celulares

Tabla 13. Lista de requisitos funcionales


| Comercializadora de celulares | Comercializadora de celulares | Comercializadora de celulares | Comercializadora de celulares |
| --- | --- | --- | --- |
| Lista de requisitos funcionales | Lista de requisitos funcionales | Lista de requisitos funcionales | Lista de requisitos funcionales |
| Código | Requisito | Caso de uso | Actor |
| RF-01 | Mantener catálogo y fotos del celular. | ECUD1 | Operador |
| RF-02 | Consultar elegibilidad y monto máximo en BanQuito. | ECUD2 | Operador |
| RF-03 | Vender en efectivo con descuento del 42%. | ECUD3 | Operador |
| RF-04 | Vender a crédito y emitir factura tras aprobación. | ECUD4 | Operador |
| RF-05 | Consultar factura y tabla de amortización. | ECUD5 | Operador |



### 8.2.5 Requisitos Funcionales

Tabla 14. Administrar catálogo e imágenes


| RQF-001 | Administrar catálogo e imágenes |
| --- | --- |
| Descripción | Crear, editar y desactivar celulares. Cargar, reemplazar o quitar JPEG, PNG o WebP de hasta 5 MiB; la API valida firma y tipo y guarda bytes en SQL Server. |
| Objetivo | OBJ-001 |
| Importancia | Alta |
| Estado | Implementado; verificado en API aislada. |
| Estabilidad | Contrato de imagen versionado. |
| Comentarios | Foto opcional por celular. |


Tabla 15. Consultar elegibilidad y monto máximo


| RQF-002 | Consultar elegibilidad y monto máximo |
| --- | --- |
| Descripción | BanQuito exige cliente existente, depósito en el último mes y ausencia de crédito activo; para hombres, edad mínima de 25 años. Máximo max(0,(promedio depósitos-promedio retiros)*0.35*6). |
| Objetivo | OBJ-002 |
| Importancia | Alta |
| Estado | Regla implementada en BanQuito. |
| Estabilidad | La regla depende del contrato remoto. |
| Comentarios | El texto indica 35%; fórmula impresa, 30%. Se aplica 35%. |


Tabla 16. Registrar venta en efectivo


| RQF-003 | Registrar venta en efectivo |
| --- | --- |
| Descripción | Validar celular activo y precio vigente, aplicar descuento del 42% y emitir factura por un celular. No se descuentan existencias porque no hay inventario. |
| Objetivo | OBJ-003 |
| Importancia | Alta |
| Estado | Implementado y probado. |
| Estabilidad | Regla comercial definida por examen. |
| Comentarios | Precio de servidor es autoritativo. |


Tabla 17. Registrar venta a crédito y emitir factura


| RQF-004 | Registrar venta a crédito |
| --- | --- |
| Descripción | Validar elegibilidad, monto y plazo de 3 a 18 meses a 16,5% anual. BanQuito registra crédito y amortización; tienda emite factura solo si la aprobación llega correctamente. |
| Objetivo | OBJ-004 |
| Importancia | Alta |
| Estado | Implementado con servicio remoto. |
| Estabilidad | Dependiente de disponibilidad de BanQuito. |
| Comentarios | Timeout o rechazo no confirma venta. |


Tabla 18. Consultar factura y amortización


| RQF-005 | Consultar factura y amortización |
| --- | --- |
| Descripción | Buscar hasta 50 facturas recientes por cédula, consultar detalle y obtener amortización si el crédito fue aprobado. |
| Objetivo | OBJ-005 |
| Importancia | Alta |
| Estado | Implementado y probado. |
| Estabilidad | Contrato de consulta versionado. |
| Comentarios | Facturas antiguas mantienen su precio guardado. |



## 8.3 Requisitos No Funcionales


### 8.3.1 Objetivo General

Proteger datos personales, conservar integridad de ventas y mostrar el estado real del servicio externo en todos los canales.

Tabla 19. Tiempo de respuesta


| RNF-001 | Tiempo de respuesta |
| --- | --- |
| Descripción | El catálogo y las consultas comunes responden en hasta 3 s en red normal. La carga de imagen se limita a 5 MiB. |
| Importancia | Alta |
| Estado | Objetivo para pruebas de aceptación. |


Tabla 20. Presentación y legibilidad


| RNF-002 | Presentación y legibilidad |
| --- | --- |
| Descripción | Los cuatro clientes muestran precio, datos, estado e imagen sin solapamientos. Las fotos conservan proporción y texto alternativo donde aplica. |
| Importancia | Media |
| Estado | Tema claro y oscuro en interfaces compatibles. |


Tabla 21. Facilidad de uso


| RNF-003 | Facilidad de uso |
| --- | --- |
| Descripción | Los formularios indican campos requeridos, vista previa y acciones para cargar, reemplazar o quitar foto. La venta permite seleccionar un celular del catálogo. |
| Importancia | Alta |
| Estado | Valida archivo y confirma acciones destructivas. |


Tabla 22. Seguridad


| RNF-004 | Seguridad |
| --- | --- |
| Descripción | La API valida MIME y firma de JPEG, PNG o WebP; limita tamaño y envía nosniff y no-store. Los clientes no guardan credenciales SQL. |
| Importancia | Alta |
| Estado | Rechaza SVG, archivo vacío, tipo falso y más de 5 MiB. |


Tabla 23. Plataforma y arquitectura


| RNF-005 | Plataforma y arquitectura |
| --- | --- |
| Descripción | Servidor .NET 10 REST y SQL Server 2025 Express. Web MVC; consola, WPF y MAUI separan vista, controlador y servicio. ADO.NET, sin EF Core. |
| Importancia | Alta |
| Estado | Modelos PowerDesigner editables. |


Tabla 24. Accesibilidad y compatibilidad


| RNF-006 | Accesibilidad y compatibilidad |
| --- | --- |
| Descripción | Teclado, etiquetas, contraste y diseño adaptable en web; controles nativos WPF/MAUI. JPEG, PNG y WebP en rutas compatibles. |
| Importancia | Media |
| Estado | Android físico no verificado en esta sesión. |


Tabla 25. Mantenimiento


| RNF-007 | Mantenimiento |
| --- | --- |
| Descripción | Separa contratos, reglas de negocio, servicios API y acceso a datos. Cierra conexiones, streams y respuestas HTTP en todos los caminos. |
| Importancia | Alta |
| Estado | 19 pruebas automatizadas pasan; cuatro clientes compilan. |



## 8.4 Otros Requerimientos


### 8.4.1 Restricciones de Diseño

Las vistas no ejecutan SQL. La API valida contratos, reglas de crédito, tipo y firma de imagen y utiliza consultas parametrizadas con ADO.NET.


### 8.4.2 Restricciones de Hardware

Los clientes requieren dispositivos compatibles y acceso al endpoint. La API necesita conectividad con SQL Server y con BanQuito para ventas de crédito.


### 8.4.3 Atributos de calidad

Integridad: factura y solicitud remota deben conservar su referencia. Seguridad: validar imagen JPEG, PNG o WebP, limitarla a 5 MiB y proteger SQL detrás de la API.


# 9. Requisitos de Rendimiento

La API debe atender consultas sin cargar imágenes completas al listar el catálogo. Las pruebas medirán latencia de catálogo, imágenes, facturación e integración bancaria.


## 9.1. Requisitos de Interfaces Externas


### 9.1.1. Interfaces de Usuario

Los formularios identifican campos obligatorios, explican el descuento, muestran fotos y permiten cargar, reemplazar o quitar una imagen con confirmación.


### 9.1.2. Interfaces de Hardware

El servidor requiere almacenamiento SQL persistente y conectividad HTTP con BanQuito. Los clientes necesitan una dirección alcanzable para la API.


### 9.1.3. Interfaces de Software

La variante integra .NET 10, REST, ADO.NET y SQL Server 2025 Express. El contrato remoto BanQuito se configura sin compartir ensamblados.


### 9.1.4. Interfaces de Comunicación

La falla de transporte no confirma crédito ni factura. El cliente informa el estado y permite consultar o reintentar según el identificador de operación.


# RECOMENDACIONES

Mantener DTO y contratos versionados, probar límites y fallos del servicio bancario, y comprobar que las conexiones y streams se liberen.


# CONCLUSIONES

La separación entre comercializadora y BanQuito mantiene dueños de datos claros. El catálogo, las fotos y las facturas viven en SQL Server de la tienda.


# BIBLIOGRAFÍA

Grupo 09. Examen práctico de celulares. Enunciado y rúbrica del examen complexivo, 2026.

Grupo 09. Proyecto CELULAR BDDLOCAL RESTFUL .NET: scripts SQL, API, clientes y modelos PowerDesigner.

Microsoft. Documentación de ASP.NET Core, .NET 10 y acceso a datos ADO.NET.

Microsoft. Documentación de SQL Server 2025 y tipos de datos binarios.
