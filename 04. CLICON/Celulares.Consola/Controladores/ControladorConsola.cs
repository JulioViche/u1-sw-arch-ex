using System.IO;
using Celulares.Cliente.Controladores;
using Celulares.Consola.Vistas;
using Celulares.Contratos;

namespace Celulares.Consola.Controladores;

internal sealed class ControladorConsola(ControladorTienda tienda)
{
    private SolicitudFactura? pendiente;

    public async Task Ejecutar()
    {
        Console.Title = "Comercializadora de Celulares GR09 · Cliente de Consola";
        VistaConsola.AplicarTema();

        while (true)
        {
            VistaConsola.Encabezado();

            if (pendiente is not null)
            {
                VistaConsola.Aviso($"HAY UNA VENTA PENDIENTE (Ref: {pendiente.IdOperacion}) · Cédula: {pendiente.Cedula} · Celular ID: {pendiente.IdCelular}");
            }

            Console.WriteLine();
            Console.WriteLine("  ─── 📋 MENÚ PRINCIPAL ────────────────────────────────────────────────");
            Console.WriteLine("    [1] 📱 Catálogo & Productos");
            Console.WriteLine("    [2] 💳 Ventas & Crédito BanQuito");
            Console.WriteLine("    [3] 📄 Facturación & Consultas");
            Console.WriteLine("    [4] 🌓 Alternar tema Claro / Oscuro");
            Console.WriteLine("    [0] 🚪 Salir de la aplicación");
            Console.WriteLine("  ──────────────────────────────────────────────────────────────────────");

            var opcion = VistaConsola.Texto("Seleccione una opción");
            if (opcion == "0")
            {
                VistaConsola.Info("Cerrando sesión. ¡Hasta pronto!");
                return;
            }

            try
            {
                switch (opcion)
                {
                    case "1": await MenuCatalogo(); break;
                    case "2": await MenuVentas(); break;
                    case "3": await MenuFacturacion(); break;
                    case "4": VistaConsola.CambiarTema(); break;
                    default:
                        VistaConsola.Aviso("Opción no válida. Elija una opción entre 0 y 4.");
                        break;
                }
            }
            catch (Exception error) when (error is ArgumentException or FormatException or InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                VistaConsola.Error(error.Message);
                VistaConsola.Pausa();
            }
        }
    }

    // =========================================================================
    // SUBMENÚ 1: CATÁLOGO & PRODUCTOS
    // =========================================================================
    private async Task MenuCatalogo()
    {
        while (true)
        {
            VistaConsola.Encabezado();
            VistaConsola.Seccion("📱 CATÁLOGO & PRODUCTOS");
            Console.WriteLine("    [1] Ver catálogo completo");
            Console.WriteLine("    [2] Registrar nuevo celular");
            Console.WriteLine("    [3] Modificar celular existente");
            Console.WriteLine("    [4] Cargar o cambiar foto");
            Console.WriteLine("    [5] Quitar foto de celular");
            Console.WriteLine("    [6] Desactivar celular");
            Console.WriteLine("    [0] ⬅️  Volver al menú principal");
            Console.WriteLine("  ──────────────────────────────────────────────────────────────────────");

            var subOpcion = VistaConsola.Texto("Seleccione una opción");
            if (subOpcion == "0") return;

            try
            {
                switch (subOpcion)
                {
                    case "1": await MostrarCatalogo(); VistaConsola.Pausa(); break;
                    case "2": await CrearNuevoCelular(); VistaConsola.Pausa(); break;
                    case "3": await EditarCelularExistente(); VistaConsola.Pausa(); break;
                    case "4": await GuardarFoto(); VistaConsola.Pausa(); break;
                    case "5": await QuitarFoto(); VistaConsola.Pausa(); break;
                    case "6": await DesactivarCelular(); VistaConsola.Pausa(); break;
                    default:
                        VistaConsola.Aviso("Opción no válida. Elija una opción entre 0 y 6.");
                        break;
                }
            }
            catch (Exception error) when (error is ArgumentException or FormatException or InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                VistaConsola.Error(error.Message);
                VistaConsola.Pausa();
            }
        }
    }

    // =========================================================================
    // SUBMENÚ 2: VENTAS & CRÉDITO BANQUITO
    // =========================================================================
    private async Task MenuVentas()
    {
        while (true)
        {
            VistaConsola.Encabezado();
            VistaConsola.Seccion("💳 VENTAS & CRÉDITO BANQUITO");
            if (pendiente is not null)
            {
                VistaConsola.Aviso($"VENTA PENDIENTE ACTIVA: Cédula {pendiente.Cedula} · Celular ID {pendiente.IdCelular} · Pago: {pendiente.FormaPago}");
            }

            Console.WriteLine("    [1] Registrar nueva venta");
            Console.WriteLine("    [2] Consultar elegibilidad de crédito BanQuito");
            Console.WriteLine("    [3] Reintentar venta pendiente");
            Console.WriteLine("    [4] Descartar venta pendiente");
            Console.WriteLine("    [0] ⬅️  Volver al menú principal");
            Console.WriteLine("  ──────────────────────────────────────────────────────────────────────");

            var subOpcion = VistaConsola.Texto("Seleccione una opción");
            if (subOpcion == "0") return;

            try
            {
                switch (subOpcion)
                {
                    case "1": await NuevaVenta(); VistaConsola.Pausa(); break;
                    case "2":
                        var cedula = VistaConsola.Texto("Ingrese la cédula a evaluar en BanQuito");
                        if (!string.IsNullOrWhiteSpace(cedula))
                        {
                            var eval = await tienda.EvaluarCredito(cedula);
                            Mostrar(eval);
                        }
                        VistaConsola.Pausa();
                        break;
                    case "3":
                        if (pendiente is null)
                            VistaConsola.Info("No hay una venta pendiente en esta sesión.");
                        else
                            await EnviarPendiente();
                        VistaConsola.Pausa();
                        break;
                    case "4":
                        if (pendiente is not null && VistaConsola.Confirmar("¿Desea descartar la venta pendiente actual?"))
                        {
                            pendiente = null;
                            VistaConsola.Exito("Referencia de venta pendiente descartada.");
                        }
                        else if (pendiente is null)
                        {
                            VistaConsola.Info("No hay ninguna venta pendiente.");
                        }
                        VistaConsola.Pausa();
                        break;
                    default:
                        VistaConsola.Aviso("Opción no válida. Elija una opción entre 0 y 4.");
                        break;
                }
            }
            catch (Exception error) when (error is ArgumentException or FormatException or InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                VistaConsola.Error(error.Message);
                if (pendiente is not null)
                    VistaConsola.Aviso("La venta conserva su identificador en memoria. Puede reintentar con la opción [3].");
                VistaConsola.Pausa();
            }
        }
    }

    // =========================================================================
    // SUBMENÚ 3: FACTURACIÓN & CONSULTAS
    // =========================================================================
    private async Task MenuFacturacion()
    {
        while (true)
        {
            VistaConsola.Encabezado();
            VistaConsola.Seccion("📄 FACTURACIÓN & CONSULTAS");
            Console.WriteLine("    [1] Buscar facturas históricas por cédula");
            Console.WriteLine("    [2] Consultar comprobante y amortización por ID");
            Console.WriteLine("    [0] ⬅️  Volver al menú principal");
            Console.WriteLine("  ──────────────────────────────────────────────────────────────────────");

            var subOpcion = VistaConsola.Texto("Seleccione una opción");
            if (subOpcion == "0") return;

            try
            {
                switch (subOpcion)
                {
                    case "1": await BuscarFacturas(); VistaConsola.Pausa(); break;
                    case "2":
                        var id = VistaConsola.Entero("Número de factura a consultar");
                        await MostrarFactura(id);
                        VistaConsola.Pausa();
                        break;
                    default:
                        VistaConsola.Aviso("Opción no válida. Elija una opción entre 0 y 2.");
                        break;
                }
            }
            catch (Exception error) when (error is ArgumentException or FormatException or InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                VistaConsola.Error(error.Message);
                VistaConsola.Pausa();
            }
        }
    }

    // =========================================================================
    // MÉTODOS DE CATÁLOGO
    // =========================================================================
    private async Task<Celular[]> MostrarCatalogo()
    {
        VistaConsola.Seccion("EQUIPOS EN CATÁLOGO");
        var celulares = await tienda.Catalogo();
        if (celulares.Length == 0)
        {
            VistaConsola.Aviso("El catálogo está vacío actualmente. Registre un nuevo celular con la opción [2].");
            return celulares;
        }

        Console.WriteLine("  ┌────┬──────────────────────────────┬──────────────┬──────────────┬─────────────┐");
        Console.WriteLine("  │ ID │ Modelo                       │ Precio Lista │ Efectivo 42% │ Foto        │");
        Console.WriteLine("  ├────┼──────────────────────────────┼──────────────┼──────────────┼─────────────┤");

        foreach (var c in celulares)
        {
            var nombreCortado = c.Nombre.Length > 28 ? c.Nombre[..25] + "..." : c.Nombre.PadRight(28);
            var precioLista = $"{c.Precio:C2}".PadLeft(12);
            var precioEfectivo = $"{Math.Round(c.Precio * 0.58m, 2):C2}".PadLeft(12);
            var foto = c.ImagenUrl is not null ? "✓ Con foto" : "─ Sin foto";
            Console.WriteLine($"  │ {c.Id,2} │ {nombreCortado} │ {precioLista} │ {precioEfectivo} │ {foto,-11} │");
        }

        Console.WriteLine("  └────┴──────────────────────────────┴──────────────┴──────────────┴─────────────┘");
        return celulares;
    }

    private async Task CrearNuevoCelular()
    {
        VistaConsola.Seccion("REGISTRAR NUEVO CELULAR");
        var nombre = VistaConsola.Texto("Nombre del modelo");
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es requerido.");

        var precio = VistaConsola.Dinero("Precio de lista (USD)");
        var celular = await tienda.Guardar(null, nombre, precio);

        VistaConsola.Exito($"Celular registrado exitosamente: {celular.Nombre} · Precio lista: {celular.Precio:C2} · Efectivo (42% desc): {Math.Round(celular.Precio * 0.58m, 2):C2}");
    }

    private async Task EditarCelularExistente()
    {
        var celulares = await MostrarCatalogo();
        if (celulares.Length == 0) return;

        Console.WriteLine();
        var id = VistaConsola.Entero("ID del celular que desea modificar");
        var actual = celulares.SingleOrDefault(c => c.Id == id)
            ?? throw new ArgumentException("El ID ingresado no existe en el catálogo.");

        VistaConsola.Info($"Editando equipo #{actual.Id}: '{actual.Nombre}' (${actual.Precio:F2})");
        var nombre = VistaConsola.Texto($"Nuevo nombre [ENTER para mantener '{actual.Nombre}']");
        if (string.IsNullOrWhiteSpace(nombre)) nombre = actual.Nombre;

        var precioTexto = VistaConsola.Texto($"Nuevo precio de lista [ENTER para mantener '{actual.Precio:F2}']");
        var precio = string.IsNullOrWhiteSpace(precioTexto) ? actual.Precio : ControladorTienda.InterpretarPrecio(precioTexto);

        var celular = await tienda.Guardar(id, nombre, precio);
        VistaConsola.Exito($"Celular modificado: {celular.Nombre} · Precio lista: {celular.Precio:C2} · Efectivo (42% desc): {Math.Round(celular.Precio * 0.58m, 2):C2}");
    }

    private async Task DesactivarCelular()
    {
        var celulares = await MostrarCatalogo();
        if (celulares.Length == 0) return;

        Console.WriteLine();
        var id = VistaConsola.Entero("ID del celular a desactivar");
        var seleccionado = celulares.SingleOrDefault(c => c.Id == id)
            ?? throw new ArgumentException("El ID ingresado no coincide con ningún celular activo.");

        if (!VistaConsola.Confirmar($"¿Seguro que desea desactivar '{seleccionado.Nombre}' del catálogo?")) return;

        await tienda.Eliminar(id);
        VistaConsola.Exito($"El celular '{seleccionado.Nombre}' ha sido desactivado del catálogo.");
    }

    // =========================================================================
    // ADMINISTRACIÓN DE FOTOS
    // =========================================================================
    private async Task GuardarFoto()
    {
        var celulares = await MostrarCatalogo();
        if (celulares.Length == 0) return;

        Console.WriteLine();
        var id = VistaConsola.Entero("ID del celular al que asignará la foto");
        var seleccionado = celulares.SingleOrDefault(c => c.Id == id)
            ?? throw new ArgumentException("Seleccione un ID del catálogo activo.");

        var ruta = VistaConsola.Texto("Ruta absoluta del archivo de foto (JPEG, PNG o WebP)");
        var tipo = TipoImagen(ruta);
        var archivo = new FileInfo(ruta);
        if (!archivo.Exists) throw new ArgumentException("No se encontró el archivo de imagen en la ruta indicada.");
        if (archivo.Length is 0 or > 5 * 1024 * 1024) throw new ArgumentException("La foto debe pesar hasta 5 MB.");

        await using var flujo = archivo.OpenRead();
        await tienda.SubirImagen(seleccionado.Id, flujo, archivo.Name, tipo);
        VistaConsola.Exito($"Foto guardada en base de datos para '{seleccionado.Nombre}' ({archivo.Length / 1024} KB).");
    }

    private async Task QuitarFoto()
    {
        var celulares = await MostrarCatalogo();
        if (celulares.Length == 0) return;

        Console.WriteLine();
        var id = VistaConsola.Entero("ID del celular al que quitará la foto");
        var seleccionado = celulares.SingleOrDefault(c => c.Id == id)
            ?? throw new ArgumentException("Seleccione un ID del catálogo activo.");

        if (seleccionado.ImagenUrl is null)
        {
            VistaConsola.Aviso($"El celular '{seleccionado.Nombre}' no tiene ninguna foto asignada.");
            return;
        }

        if (!VistaConsola.Confirmar($"¿Eliminar la foto de '{seleccionado.Nombre}' de la base de datos?")) return;

        await tienda.QuitarImagen(seleccionado.Id);
        VistaConsola.Exito($"Foto eliminada de '{seleccionado.Nombre}'.");
    }

    private static string TipoImagen(string ruta) => Path.GetExtension(ruta).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => throw new ArgumentException("Formato no soportado. Debe usar un archivo JPEG, PNG o WebP.")
    };

    // =========================================================================
    // VENTAS Y FACTURACIÓN
    // =========================================================================
    private async Task NuevaVenta()
    {
        if (pendiente is not null)
            throw new InvalidOperationException("Hay una venta previa pendiente. Reinténtela o descártela antes de iniciar otra.");

        var celulares = await MostrarCatalogo();
        if (celulares.Length == 0)
            throw new InvalidOperationException("Catálogo vacío. Debe registrar al menos un celular antes de vender.");

        Console.WriteLine();
        VistaConsola.Seccion("NUEVA TRANSACCIÓN DE VENTA");
        var ids = VistaConsola.Texto("IDs de celulares a comprar, separados por coma").Split(',');
        if (ids.Length == 0 || ids.Any(x => !int.TryParse(x.Trim(), out var n) || n <= 0))
            throw new ArgumentException("Ingrese IDs válidos separados por coma.");
        var numeros = ids.Select(x => int.Parse(x.Trim())).ToArray();
        if (numeros.Distinct().Count() != numeros.Length) throw new ArgumentException("No repita modelos.");
        var seleccionados = numeros.Select(id => celulares.SingleOrDefault(c => c.Id == id)
            ?? throw new ArgumentException("Un ID no existe en el catálogo activo.")).OrderBy(c => c.Id).ToArray();
        var seleccionado = seleccionados[0];
        var precio = seleccionados.Sum(c => c.Precio);

        var cedula = VistaConsola.Texto("Cédula del comprador");
        if (string.IsNullOrWhiteSpace(cedula)) throw new ArgumentException("La cédula es requerida.");

        var nombre = VistaConsola.Texto("Nombre del comprador");
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre del comprador es requerido.");

        Console.WriteLine();
        Console.WriteLine("  Formas de pago:");
        Console.WriteLine("    [1] Efectivo (42% de descuento inmediato)");
        Console.WriteLine("    [2] Crédito directo BanQuito (16.5% anual, financiado de 3 a 18 cuotas)");
        var forma = VistaConsola.Texto("Seleccione forma de pago (1 o 2)");
        if (forma is not ("1" or "2")) throw new ArgumentException("Opción de pago no válida. Elija 1 o 2.");

        var esCredito = forma == "2";
        int? cuotas = null;

        if (esCredito)
        {
            var evaluacion = await tienda.EvaluarCredito(cedula);
            Mostrar(evaluacion);
            if (!evaluacion.SujetoCredito)
            {
                VistaConsola.Aviso($"BanQuito NO aprobó crédito para la cédula {cedula}: {evaluacion.Motivo}");
                if (!VistaConsola.Confirmar("¿Desea cambiar la forma de pago a EFECTIVO con 42% de descuento?"))
                    return;
                esCredito = false;
            }
            else if (precio > evaluacion.MontoMaximo)
            {
                VistaConsola.Aviso($"El total ({precio:C2}) supera el cupo aprobado por BanQuito ({evaluacion.MontoMaximo:C2}).");
                if (!VistaConsola.Confirmar("¿Desea cambiar la forma de pago a EFECTIVO con 42% de descuento?"))
                    return;
                esCredito = false;
            }
            else
            {
                cuotas = VistaConsola.Entero("Número de cuotas mensuales (3 a 18)", 3, 18);
            }
        }

        var total = esCredito ? precio : ControladorTienda.TotalEfectivo(precio);
        var descuento = precio - total;

        Console.WriteLine();
        Console.WriteLine("  ╔══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║                      RESUMEN DE LA TRANSACCIÓN                           ║");
        Console.WriteLine("  ╠══════════════════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"  ║  Cliente:    {nombre} (Cédula: {cedula})".PadRight(75) + "║");
        foreach (var cel in seleccionados) Console.WriteLine($"  ║  Equipo:     {cel.Nombre} · {cel.Precio:C2}".PadRight(75) + "║");
        Console.WriteLine($"  ║  Pago:       {(esCredito ? $"Crédito BanQuito ({cuotas} cuotas)" : "Efectivo al contado (42% desc.)")}".PadRight(75) + "║");
        Console.WriteLine($"  ║  Precio:     {precio:C2} · Descuento: {descuento:C2} · TOTAL: {total:C2}".PadRight(75) + "║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════════════════════════════╝");

        if (!VistaConsola.Confirmar("¿Confirmar y emitir la factura?")) return;

        pendiente = new SolicitudFactura
        {
            Cedula = cedula.Trim(),
            NombreCliente = nombre.Trim(),
            IdCelular = seleccionado.Id,
            Detalles = seleccionados.Select(c => new DetalleSolicitudFactura(c.Id, c.Precio)).ToList(),
            FormaPago = esCredito ? "CREDITO_DIRECTO" : "EFECTIVO",
            NumeroCuotas = cuotas,
            PrecioEsperado = seleccionado.Precio
        };

        await EnviarPendiente();
    }

    private async Task EnviarPendiente()
    {
        var factura = await tienda.Facturar(pendiente!);
        pendiente = null;
        VistaConsola.Exito($"Venta registrada con éxito: Factura #{factura.Id} · Estado: {factura.Estado} · Total: {factura.Total:C2}");
        await MostrarFactura(factura.Id);
    }

    // =========================================================================
    // CONSULTA Y COMPROBANTES DE FACTURA
    // =========================================================================
    private async Task BuscarFacturas()
    {
        var cedula = VistaConsola.Texto("Ingrese la cédula del comprador a consultar");
        if (string.IsNullOrWhiteSpace(cedula)) throw new ArgumentException("La cédula es requerida.");

        var facturas = await tienda.FacturasPorCedula(cedula);
        if (facturas.Length == 0)
        {
            VistaConsola.Info($"No se encontraron facturas emitidas para la cédula {cedula}.");
            return;
        }

        VistaConsola.Seccion($"HISTORIAL DE FACTURAS · CÉDULA {cedula}");
        Console.WriteLine("  ┌──────┬────────────┬────────────────────────┬─────────────────┬───────────┬──────────┐");
        Console.WriteLine("  │  #ID │ Fecha      │ Celular                │ Pago            │     Total │ Estado   │");
        Console.WriteLine("  ├──────┼────────────┼────────────────────────┼─────────────────┼───────────┼──────────┤");

        foreach (var f in facturas)
        {
            var cel = f.Celular.Length > 22 ? f.Celular[..19] + "..." : f.Celular.PadRight(22);
            var pago = f.FormaPago == "EFECTIVO" ? "Efectivo" : "Crédito BanQuito";
            Console.WriteLine($"  │ {f.Id,4} │ {f.Fecha:yyyy-MM-dd} │ {cel} │ {pago,-15} │ {f.Total,9:C2} │ {f.Estado,-8} │");
        }

        Console.WriteLine("  └──────┴────────────┴────────────────────────┴─────────────────┴───────────┴──────────┘");

        if (VistaConsola.Confirmar("¿Desea ver el comprobante y desglose de alguna factura de la lista?"))
        {
            var id = VistaConsola.Entero("Ingrese el ID de la factura");
            await MostrarFactura(id);
        }
    }

    private async Task MostrarFactura(int id)
    {
        var factura = await tienda.Factura(id);

        Console.WriteLine();
        Console.WriteLine("  ╔══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║                      COMPROBANTE ELECTRÓNICO                             ║");
        Console.WriteLine($"  ║                 Factura Electrónica #{factura.Id} · {factura.Estado}".PadRight(75) + "║");
        Console.WriteLine("  ╠══════════════════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"  ║  Emisión:     {factura.Fecha:yyyy-MM-dd HH:mm:ss}".PadRight(75) + "║");
        Console.WriteLine($"  ║  Comprador:   {factura.NombreCliente} (Cédula: {factura.Cedula})".PadRight(75) + "║");
        foreach (var detalle in factura.Detalles) Console.WriteLine($"  ║  Equipo:      {detalle.Celular} · {detalle.Precio:C2}".PadRight(75) + "║");
        Console.WriteLine($"  ║  Forma pago:  {(factura.FormaPago == "EFECTIVO" ? "Efectivo al contado (42% desc.)" : $"Crédito directo BanQuito ({factura.NumeroCuotas} cuotas)")}".PadRight(75) + "║");
        Console.WriteLine("  ╟──────────────────────────────────────────────────────────────────────────╢");
        Console.WriteLine($"  ║  Precio de lista:                                         {factura.Precio,14:C2} ║");
        Console.WriteLine($"  ║  Descuento aplicado:                                     -{(factura.Descuento > 0 ? factura.Descuento : 0m),13:C2} ║");
        Console.WriteLine($"  ║  TOTAL FACTURADO:                                         {factura.Total,14:C2} ║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════════════════════════════╝");

        if (factura.FormaPago == "CREDITO_DIRECTO" && factura.Estado == "APROBADA")
        {
            var cuotas = await tienda.Amortizacion(id);
            if (cuotas.Length > 0)
            {
                VistaConsola.Seccion("PLAN DE AMORTIZACIÓN BANQUITO · TASA 16.5% ANUAL");
                Console.WriteLine("  ┌───────┬────────────┬─────────────┬─────────────┬─────────────┬─────────────┐");
                Console.WriteLine("  │ Cuota │ Fecha Pago │ Valor Cuota │   Interés   │   Capital   │ Saldo Deuda │");
                Console.WriteLine("  ├───────┼────────────┼─────────────┼─────────────┼─────────────┼─────────────┤");
                foreach (var c in cuotas)
                {
                    Console.WriteLine($"  │ {c.NumeroCuota,5} │ {c.FechaPago:yyyy-MM-dd} │ {c.ValorCuota,11:C2} │ {c.InteresPagado,11:C2} │ {c.CapitalPagado,11:C2} │ {c.Saldo,11:C2} │");
                }
                Console.WriteLine("  └───────┴────────────┴─────────────┴─────────────┴─────────────┴─────────────┘");
            }
        }
    }

    private static void Mostrar(EvaluacionCredito evaluacion)
    {
        Console.WriteLine();
        if (evaluacion.SujetoCredito)
        {
            Console.WriteLine("  ╔══════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║ ✓ BANQUITO: CLIENTE SUJETO A CRÉDITO DIRECTO                             ║");
            Console.WriteLine($"  ║   Cliente:      {evaluacion.Nombre} (Código: {evaluacion.CodigoCliente})".PadRight(75) + "║");
            Console.WriteLine($"  ║   Cupo máximo:  {evaluacion.MontoMaximo:C2}".PadRight(75) + "║");
            Console.WriteLine("  ║   Condición:    Tasa 16.5% anual, financiamiento de 3 a 18 cuotas.       ║");
            Console.WriteLine("  ╚══════════════════════════════════════════════════════════════════════════╝");
        }
        else
        {
            Console.WriteLine("  ╔══════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║ ✗ BANQUITO: CLIENTE NO ELEGIBLE PARA CRÉDITO DIRECTO                     ║");
            Console.WriteLine($"  ║   Motivo:  {evaluacion.Motivo}".PadRight(75) + "║");
            Console.WriteLine("  ║   Aviso:   Solo puede realizar compras con forma de pago en Efectivo.    ║");
            Console.WriteLine("  ╚══════════════════════════════════════════════════════════════════════════╝");
        }
    }
}
