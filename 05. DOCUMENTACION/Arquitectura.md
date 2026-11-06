# Arquitectura de la solución

La comercializadora y BanQuito son aplicaciones independientes. El detalle editable está en los modelos PowerDesigner de `01. UML` y `02. MER`; este diagrama Mermaid resume el límite de integración.

~~~mermaid
flowchart LR
    C[Consola] --> R[API comercializadora REST :19102]
    E[WPF] --> R
    M[MAUI Android] --> R
    W[Web ASP.NET Core MVC :19103] --> R
    R --> V[(ComercializadoraCelulares SQL Server)]
    R -- HTTP / REST --> B[BanQuito.Api :19101]
    B --> D[(BanQuitoCore, base bancaria independiente)]
~~~

La tienda coordina catálogo y facturas. BanQuito evalúa elegibilidad, calcula y registra crédito y amortización. Cada API conserva su base y reglas. `COD_CREDITO` es una referencia remota, no una clave foránea entre bases.

La web usa MVC. Los otros clientes mantienen vista, controlador y servicio. La API separa controladores, servicios y reglas de dominio, con dependencias inyectadas. La persistencia de la tienda usa ADO.NET parametrizado; no usa Entity Framework Core.
