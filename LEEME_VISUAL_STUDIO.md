# Examen Práctico Celulares (GR09) - Guía de Inicio Rápido para Visual Studio

Este proyecto ha sido organizado de forma 100% autónoma en esta carpeta para que puedas abrirlo, modificarlo, depurarlo y aprender a ejecutarlo directamente desde Visual Studio sin depender del repositorio original.

---

## 1. Abrir en Visual Studio

1. Abre el archivo **`ExamenCelulares.sln`** (o `ExamenCelulares.slnx`) con **Visual Studio 2022 o posterior**.
2. Verás todos los proyectos cargados en el **Explorador de soluciones**:
   * **Servicios Backend:**
     * `04. SERVIDOR/Comercializadora.Api`: API REST principal de la tienda (puerto `19102`).
     * `04. SERVIDOR/Comercializadora.Dominio`: Reglas de negocio (cálculo de 42% de descuento, validación de fotos).
     * `04. SERVIDOR/Contratos`: DTOs y modelos compartidos.
     * `04. SERVIDOR/BanQuito.Api`: API REST del banco para evaluación crediticia (puerto `19101`).
     * `04. SERVIDOR/BanQuito.Dominio`: Reglas bancarias (edad, depósitos/retiros, amortización al 16.5%).
   * **Clientes:**
     * `04. CLIWEB/Celulares.Web`: Tienda Web en ASP.NET Core MVC (puerto `19103`).
     * `04. CLIESC/Celulares.Escritorio`: Cliente de escritorio WPF.
     * `04. CLICON/Celulares.Consola`: Cliente CLI interactivo por consola.
     * `04. CLIMOV/Celulares.Movil`: Cliente móvil Android en .NET MAUI.
   * **Pruebas:**
     * `04. SERVIDOR/Pruebas`: 19 pruebas unitarias de la comercializadora.
     * `04. SERVIDOR/BanQuito.Pruebas`: 20 pruebas unitarias del banco.

---

## 2. Base de Datos (SQL Server LocalDB)

Las cadenas de conexión en `appsettings.json` ya vienen configuradas para:
```
Server=(localdb)\MSSQLLocalDB;Database=ComercializadoraCelulares;Integrated Security=True;Encrypt=False;TrustServerCertificate=True
```
y para BanQuito: `Database=BanQuitoCore`.

Si en algún momento necesitas recrear o reiniciar las bases desde cero, ejecuta en PowerShell:
```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -i ".\03. BDD\01_CrearBases.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -i ".\03. BDD\02_DatosPrueba.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -i ".\03. BDD\03_MigrarImagenes.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -i ".\03. BDD\BanQuito_01_CrearBase.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -i ".\03. BDD\BanQuito_02_DatosPrueba.sql"
```

---

## 3. Configurar Múltiples Proyectos de Inicio en Visual Studio (F5)

Para que al pulsar **F5** se levanten automáticamente el banco, la comercializadora y la interfaz:

1. Haz clic derecho en el nodo superior de la **Solución ('ExamenCelulares')** > **Propiedades**.
2. En el menú de la izquierda, selecciona **Proyectos de inicio comunes** > **Varios proyectos de inicio**.
3. Selecciona la acción **Iniciar** para los siguientes 3 proyectos:
   * `BanQuito.Api` -> **Iniciar**
   * `Comercializadora.Api` -> **Iniciar**
   * `Celulares.Web` (o `Celulares.Escritorio`) -> **Iniciar**
4. Haz clic en **Aceptar**.
5. Presiona **F5**: Visual Studio compilará los proyectos y abrirá las ventanas de depuración.

---

## 4. Datos de Prueba para Crédito

En `BanQuitoCore`, los clientes sembrados para probar compras a crédito son:
* **Juan Perez:** Cédula `0102030405` (Aprobado, cupo máx: \$1.974,00).
* **Carlos Gomez:** Cédula `0203040506` (Rechazado por ser menor de 25 años masculino).
* **Ana Lopez:** Cédula `0304050607` (Aprobada).

