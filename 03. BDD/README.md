# Base de datos de la tienda

`01_CrearBases.sql` y `02_DatosPrueba.sql` crean y siembran `ComercializadoraCelulares`. Siga el manual técnico para inicializarla en SQLEXPRESS. Los scripts bancarios pertenecen al proyecto independiente BanQuito.

En una base existente, aplique `04_MigrarDetalleFactura.sql` antes de iniciar la API actualizada. El script agrega el detalle de las facturas existentes sin eliminar sus datos.

Desde la raíz del proyecto ejecute `powershell -ExecutionPolicy Bypass -File .\migrar-facturas.ps1`. El script usa la conexión configurada para la API; si usa otra instancia, proporciónela con `-Conexion` o con `ConnectionStrings__BaseDatos`.
