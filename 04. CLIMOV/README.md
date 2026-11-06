# Cliente Android MAUI

Consume la API de la comercializadora. El menú lateral separa `Catálogo`, `Nueva venta` y `Facturas`; el catálogo presenta fotos, precio de lista y precio en efectivo. La ficha permite editar modelo, precio y foto. Venta muestra el total antes de confirmar; facturas permite consultar el comprobante y, para crédito, el detalle de amortización. Tema claro u oscuro se elige desde el menú y queda guardado en el dispositivo.

Para requisitos y ejecución, consulte `../05. DOCUMENTACION/ManualTecnico.md`.

El cliente móvil usa `CELULARES_API_URL` cuando está disponible y, si no, `http://192.168.137.1:19102/`. También permite cambiar el servidor desde la interfaz.

Para instalar el APK directamente con `adb install -r`, compile con `-p:EmbedAssembliesIntoApk=true`. Sin esta opción, un APK Debug puede requerir el despliegue rápido de ensamblados desde el IDE.

## Carpeta APK y scripts de generación

Cada vez que se compila o publica el proyecto, el archivo `.apk` firmado se copia automáticamente a la carpeta:
`04. CLIMOV/APK/ec.edu.monster.celulares.gr09-Signed.apk`

También puedes generar el APK Release con un solo clic ejecutando:
- `04. CLIMOV/generar-apk.bat` (doble clic en Windows)
- `04. CLIMOV/generar-apk.ps1` (PowerShell)
