# Base BanQuitoCore

Estos scripts pertenecen al proyecto bancario:

1. `BanQuito_01_CrearBase.sql`: crea BanQuitoCore y las tablas del core de crédito.
2. `BanQuito_02_DatosPrueba.sql`: inserta cinco clientes, cinco cuentas y cincuenta movimientos si todavía no existen.

Los scripts son idempotentes y no borran datos. Para ejecutarlos, desde la raíz del proyecto bancario configure `BANQUITO_SQL_MASTER` y use `BanQuito.Administracion`, como indica el README principal. No ejecute estos scripts desde el proyecto de celulares.

El esquema existente de EurekaBank no se modifica con estos scripts; BanQuitoCore permanece como base de crédito de este servicio bancario.
