# Cliente Android MAUI

Consume la API de la comercializadora. El menú lateral separa `Catálogo`, `Nueva venta` y `Facturas`; el catálogo presenta fotos, precio de lista y precio en efectivo. La ficha permite editar modelo, precio y foto. Venta muestra el total antes de confirmar; facturas permite consultar el comprobante y, para crédito, el detalle de amortización. Tema claro u oscuro se elige desde el menú y queda guardado en el dispositivo.

Para requisitos y ejecución, consulte `../05. DOCUMENTACION/ManualTecnico.md`.

La URL se configura internamente en `Celulares.Movil/MauiProgram.cs`, constante `DireccionServidor`. El emulador usa `http://10.0.2.2:19102/` y conecta automáticamente; no existe un campo de servidor en la interfaz.

Para instalar el APK directamente con `adb install -r`, compile con `-p:EmbedAssembliesIntoApk=true`. Sin esta opción, un APK Debug puede requerir el despliegue rápido de ensamblados desde el IDE.
