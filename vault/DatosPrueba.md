# Datos de Prueba para el Examen (Cédulas y Reglas de Negocio)

Este documento contiene los datos sembrados en la base de datos de **BanQuito** y la **Comercializadora** para realizar pruebas durante la defensa del examen.

---

## 1. Clientes Bancarios Sembrados (Elegibilidad y Crédito)

La base de datos `BanQuitoCore` cuenta con 5 clientes diseñados específicamente para evaluar cada regla de negocio solicitada en la rúbrica:

| Cédula | Nombre del Cliente | Género / Edad | Estado en BanQuito | Cupo Máximo | Regla Demostrada |
| :--- | :--- | :---: | :---: | :---: | :--- |
| **`0102030405`** | **Juan Perez** | M · 35 años | ✅ **Aprobado** | **$1.974,00** | Cliente regular: varón mayor de 25 años con depósitos recientes y sin créditos activos. |
| **`0203040506`** | **Carlos Gomez** | M · 24 años | ❌ **Rechazado** | $0,00 | **Restricción de edad:** Varón menor de 25 años. |
| **`0304050607`** | **Ana Lopez** | F · 23 años | ✅ **Aprobada** | **$2.079,00** | Cliente mujer mayor de 18 años con depósitos recientes (no le aplica restricción de 25 años). |
| **`0405060708`** | **Maria Torres** | F · 32 años | ❌ **Rechazada** | $0,00 | **Actividad bancaria:** No registra ningún depósito en los últimos 30 días (su último depósito fue hace 46 días). |
| **`0506070809`** | **Luis Sanchez** | M · 30 años | ✅ **Aprobado** | **$2.184,00** | Cliente con excelente promedio de depósitos; genera el cupo crediticio más alto. |
| *(Cualquiera)* | *No registrado* | - | ❌ **Rechazada** | $0,00 | **Existencia:** La persona no es cliente del banco. |

---

## 2. Celulares Registrados en el Catálogo

En la base de datos `ComercializadoraCelulares` se encuentran disponibles:

| ID | Modelo del Celular | Precio de Lista (Crédito) | Precio con Descuento (Efectivo 42%) |
| :---: | :--- | :---: | :---: |
| **001** | Celular Aurora 128 GB | $450.00 | **$261.00** |
| **002** | Celular Nova 256 GB | $750.00 | **$435.00** |
| **003** | Celular Prisma 64 GB | $250.00 | **$145.00** |

---

## 3. Escenarios de Prueba para la Defensa

### Escenario A: Compra en Efectivo (Descuento del 42%)
1. Seleccionar cualquier celular (ej. *Celular Aurora* a \$450.00).
2. Forma de pago: **Efectivo**.
3. Ingresar cualquier cédula y nombre.
4. **Resultado esperado:** La factura se genera inmediatamente por un total de **\$261.00** (aplica el 42% de descuento).

---

### Escenario B: Compra a Crédito Aprobada
1. Ingresar la cédula `0102030405`.
2. Hacer clic en **"Consultar elegibilidad en BanQuito"**.
3. **Resultado:** Aparece el mensaje en verde con cupo de **\$1.974,00**.
4. Seleccionar un plazo de cuotas (entre 3 y 18 meses, ej. 12 cuotas).
5. Confirmar venta.
6. **Resultado:** Factura aprobada. Al hacer clic en "Ver detalle", se visualiza la **tabla de amortización** completa con cuotas fijas al 16.5% anual.

---

### Escenario C: Compra a Crédito Rechazada (Por Edad)
1. Ingresar la cédula `0203040506`.
2. Consultar elegibilidad.
3. **Resultado:** BanQuito responde en rojo indicando que el cliente masculino no cumple la edad mínima de 25 años.

---

### Escenario D: Compra a Crédito Rechazada (Por Falta de Depósito)
1. Ingresar la cédula `0405060708`.
2. Consultar elegibilidad.
3. **Resultado:** BanQuito responde rechazado porque no registra depósitos en el último mes.

---

### Escenario E: Historial de Compras
1. En la sección **"Historial de compra"**, ingresar la cédula con la que realizaste una venta (ej. `0102030405`).
2. Presionar **"Buscar facturas"**.
3. **Resultado:** Se listan todas las facturas históricas asociadas a esa cédula.
