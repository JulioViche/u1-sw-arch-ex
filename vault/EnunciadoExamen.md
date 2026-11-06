Examen Práctico Celulares

Duración: 5 horas.

Sistema BanQuito Módulo de Crédito

El Banco BanQuito ha formado una alianza estratégica con una compañía comercializadora de teléfonos celulares, para proveer de una línea de crédito directo a los clientes del banco que deseen adquirir un teléfono de acuerdo a su capacidad de pago.

Como parte de este examen práctico usted deberá desarrollar las dos aplicaciones necesarias: Sistema de la Comercializadora de Teléfonos Celulares y Módulo de Crédito del Banco Banquito; a continuación se describen las características de las funcionalidades a ser desarrolladas para cada aplicación.

Sistema de la Comercializadora de Teléfonos Celulares

Para este sistema usted deberá definir un modelo de datos, que permita realizar la facturación de la venta de teléfonos celulares.

El sistema de facturación de la comercializadora NO deberá contar con inventario, basta con solo tener una entidad donde se encuentren los electrodomésticos disponibles para la venta.

Formas de pago; en virtud de la alianza con el banco Banquito la comercializadora de teléfonos celulares deberá permitir dos formas de pago: Efectivo y Crédito Directo; para el caso de compras en efectivo se deberá dar un descuento del 42% del precio de venta.

Cuando la forma de pago seleccionada sea Crédito Directo, el sistema deberá invocar un servicio web del Banco Banquito, para saber si el comprador es sujeto de crédito del banco Banquito, en caso de que la respuesta sea positiva y que el valor del teléfono que desea comprar sea menos o igual que el crédito máximo aprobado se procederá a realizar la facturación.

Módulo de Crédito del Banco BanQuito.

Este módulo permitirá verificar si una persona es sujeto o no de crédito para el Banco BanQuito, si la persona es sujeto de crédito se deberá verificar adicionalmente el monto máximo de crédito autorizado; si el precio del teléfono es menor o igual al monto máximo aprobado el módulo automáticamente deberá aprobar la transacción y registrar la respectiva tabla de amortización del préstamo.

Para verificar si una persona es sujeto de crédito se deberá seguir las siguientes reglas.

Verificar si el solicitante es cliente del banco.

Verificar que el cliente al menos posea una transacción de depósito en el último mes.

Verificar que el cliente no sea menor de 25 años si género es masculino

Verificar que el cliente actualmente no tenga un crédito activo en el banco.

Para obtener el monto máximo del crédito se deberá seguir las siguientes reglas.

Obtener un promedio de todos los depósitos de los últimos 3 meses.

Obtener un promedio de todos los retiros de los últimos 3 meses.

El monto máximo del crédito será del 35% de la diferencia de los promedios de depósito menos los promedios de retiro multiplicado por 6.

((Promedio Depósitos – Promedio Retiros) * 30%) * 6

Si el crédito es aprobado se procederá a crear la tabla de amortización, para esto se deberá tener en cuenta lo siguiente:

El tipo de crédito es cuota fija (todos los meses se debe pagar la misma cuota)

El pago de cada cuota es mensual

El cliente escoge el plazo del crédito el cual debe ser mayor o igual a 3 meses y menor o igual a 18 meses.

La tasa de interés es del 16.5% anual.

Para la elaboración de la tabla de amortización se recomienda seguir los siguientes pasos.

Calcular la cuota fija de pago, para esto se debe utilizar la siguiente fórmula.

Valor Teléfono

Cuota = ------------------------------------------------

1- ((1+TasaPeriodo)^ -NúmeroCuotas)

---------------------------------------------------

TasaPeriodo

Nota: Al ser el pago mensual la TasaPeriodo es igual a: 16.5%/12

Puede realizar la comprobación del valor utilizando la función “PAGO” de Excel.

Utilizar el siguiente formato para la elaboración de la tabla de amortización:


| # Cuota | Valor Cuota | Interés Pagado | Capital Pagado | Saldo |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |


Nota: La tabla de amortización debe reflejar todas las cuotas que el cliente pagará para cancelar la totalidad del crédito más los intereses generados.

Revise la tabla de amortización de ejemplo que se presenta para que pueda realizar sus pruebas.


### Consideraciones Adicionales:

El sistema CORE del Banco tiene el siguiente modelo de Base de Datos.

En base a la descripción funcional, usted deberá crear e implementar el modelo de Base de Datos del sistema de Facturación de la Comercializadora de Teléfonos Celulares.

En base a la descripción funcional, usted deberá crear e implementar el modelo de Base de Datos del módulo de Crédito en el mismo esquema del Core.  Para esto es necesario solo guardar los datos del crédito y la tabla de amortización.

Debe demostrar la existencia de dos aplicaciones Web diferentes.  La primera aplicación corresponde al Core Bancario del Banco BanQuito con el módulo de crédito, la cual solo expone los Servicios Web que serán invocados por la segunda aplicación de la comercializadora de teléfonos celulares.

En base a la descripción de los sistemas y como parte de este examen complexivo se solicita realizar las siguientes actividades; las cuales se especifican con la respectiva rúbrica de evaluación.

Forma de Evaluación

Una vez que el tiempo del examen ha concluido se sorteará a cada estudiante un turno, para la respectiva defensa de lo desarrollado ante el tribunal de evaluación.

Todos los puntos que constituyen la rúbrica de evaluación deberán ser mostrados y defendidos ante el tribunal de evaluación; en la exposición de cada punto cualquier miembro del tribunal tendrá la potestad de realizar las preguntas que considere necesario al estudiante.

Importante:

Antes de iniciar con el desarrollo del examen por favor asegúrese de leer y entender claramente lo que se solicita realizar en la Rúbrica de Evaluación.

NO SE DEBE DESARROLLAR LA PROGRAMACIÓN DE TODO EL SISTEMA BANQUITO CORE.

Los evaluadores solo tomarán en cuenta para la calificación lo que se especifica en la rúbrica del examen.

Si el estudiante realiza funcionalidades adicionales a las solicitadas, estas no serán tomadas en cuenta y no constituirán un valor adicional a la nota final.

El estudiante es libre de seleccionar herramientas, plataformas, “frameworks” o lenguajes de programación que considere necesarios para resolver el examen.  Adicionalmente el estudiante puede utilizar el motor de base de datos con el que se sienta más cómodo trabajando.

Es necesario grabar en un CD/DVD todo lo desarrollado por el estudiante que será sometido a evaluación.

Ejemplo de Tabla de Amortización

REGISTRO DE EVALUACIÓN

NOMBRE: __________________________________________    ID BANNER _______________________

FECHA: ___________________________________	CALIFICACIÓN: ___________________


| Ítem | Puntaje |
| --- | --- |
| Diagrama de Casos de Uso del Módulo de Crédito del Banco BanQuito | / 2.0 |
| Descripción del Caso de Uso que verifica si el cliente es sujeto de crédito | / 1.0 |
| Implementación de la Base de Datos del sistema CORE con al menos: 5 Clientes, 5 cuentas, 50 movimientos.  Incluir entidades para registro de crédi | / 2.0 |
| Diagrama Entidad – Relación del Sistema de la Comercializadora de Teléfonos | / 1.0 |
| Implementación del servicio Web que valida si una persona es sujeto de crédito en base a su cédula. | / 1.5 |
| Implementación del Servicio Web que retorna el monto máximo de crédito para una persona en base a su código de cliente. | / 1.5 |
| Implementación del Servicio Web que otorga el crédito y crea la tabla de amortización en base a los siguientes parámetros: Código de Cliente, Precio del Teléfono Celular, Número de Cuotas. | / 2.0 |
| Implementación de funcionalidad para dar mantenimiento al catálogo de teléfonos celulares que se encuentran a la venta. | / 1.0 |
| Implementación de la funcionalidad de facturación de la Comercializadora de Teléfonos Celulares. Esta funcionalidad contempla:  Venta con Pago en Efectivo,  Venta con Crédito Directo. Debe hacerlo para los cuatro clientes | / 2.0 / 3.0 |
| Implementación de servicio SOAP o REST dependiendo las indicaciones de su docente que retorna la tabla de amortización para un crédito otorgado.  Debe hacerlo para los cuatro clientes | / 1.0 |
| Consulta de tabla de amortización de un préstamo de un cliente desde la aplicación de la Comercializadora de Teléfonos Celulares | / 1.0 |
| Diagrama de Arquitectura de la solución completa | / 1.0 |
| Total | / 20.0 |

