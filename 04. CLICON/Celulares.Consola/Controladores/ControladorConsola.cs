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
            Console.WriteLine("  ─── 📱 CATÁLOGO & PRODUCTOS ──────────────────────────────────────────");
            Console.WriteLine("    [1] Ver catálogo completo          [2] Registrar o editar celular");
            Console.WriteLine("    [3] Desactivar celular             [10] Cargar o cambiar foto");
            Console.WriteLine("    [11] Quitar foto del celular");
            Console.WriteLine();
            Console.WriteLine("  ─── 💳 VENTAS & CRÉDITO BANQUITO ─────────────────────────────────────");
            Console.WriteLine("    [4] Registrar nueva venta          [5] Consultar elegibilidad BanQuito");
            Console.WriteLine("    [8] Reintentar venta pendiente     [9] Descartar venta pendiente");
            Console.WriteLine();
            Console.WriteLine("  ─── 📄 FACTURACIÓN & SISTEMA ─────────────────────────────────────────");
            Console.WriteLine("    [6] Buscar facturas por cédula     [7] Consultar comprobante por ID");
            Console.WriteLine("    [12] Alternar tema Claro / Oscuro  [0] Salir");
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
                    case "1": await MostrarCatalogo(); VistaConsola.Pausa(); break;
                    case "2": await GuardarCelular(); VistaConsola.Pausa(); break;
                    case "3": await DesactivarCelular(); VistaConsola.Pausa(); break;
                    case "4": await NuevaVenta(); VistaConsola.Pausa(); break;
                    case "5":
                        var cedula = VistaConsola.Texto("Ingrese la cédula a evaluar");
                        if (!string.IsNullOrWhiteSpace(cedula))
                        {
                            var eval = await tienda.EvaluarCredito(cedula);
                            Mostrar(eval);
                        }
                        VistaConsola.Pausa();
                        break;
                    case "6": await BuscarFacturas(); VistaConsola.Pausa(); break;
                    case "7":
                        var idFactura = VistaConsola.Entero("Número de factura a consultar");
                        await MostrarFactura(idFactura);
                        VistaConsola.Pausa();
                        break;
                    case "8":
                        if (pendiente is null)
                            VistaConsola.Info("No hay una venta pendiente en esta sesión.");
                        else
                            await EnviarPendiente();
                        VistaConsola.Pausa();
                        break;
                    case "9":
                        if (pendiente is not null && VistaConsola.Confirmar("¿Descartar la venta pendiente actual?"))
                        {
                            pendiente = null;
                            VistaConsola.Exito("Referencia pendiente descartada.");
                        }
                        else if (pendiente is null)
                        {
                            VistaConsola.Info("No hay ninguna venta pendiente.");
                        }
                        VistaConsola.Pausa();
                        break;
                    case "10": await GuardarFoto(); VistaConsola.Pausa(); break;
                    case "11": await QuitarFoto(); VistaConsola.Pausa(); break;
                    case "12": VistaConsola.CambiarTema(); break;
                    default:
                        VistaConsola.Aviso("Opción no válida. Por favor elija un número del menú.");
                        break;
                }
            }
            catch (Exception error) when (error is ArgumentException or FormatException or InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                VistaConsola.Error(error.Message);
                if (pendiente is not null)
                    VistaConsola.Aviso("La venta conserva su identificador en memoria. Puede reintentar con la opción [8].");
                VistaConsola.Pausa();
            }
        }
    }

    // =========================================================================
    // CATÁLOGO DE PRODUCTOS
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

    private async Task GuardarCelular()
    {
        await MostrarCatalogo();
        Console.WriteLine();
        var textoId = VistaConsola.Texto("ID existente para modificar (o presione ENTER para crear nuevo)");
        int? id = string.IsNullOrWhiteSpace(textoId) ? null :
            int.TryParse(textoId, out var existente) && existente > 0 ? existente : throw new ArgumentException("Ingrese un ID positivo del catálogo.");

        var nombre = VistaConsola.Texto("Nombre del modelo");
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es requerido.");

        var precio = VistaConsola.Dinero("Precio de lista (USD)");
        var celular = await tienda.Guardar(id, nombre, precio);

        VistaConsola.Exito($"Celular {(id is null ? "registrado" : "modificado")}: {celular.Nombre} · Precio lista: {celular.Precio:C2} · Efectivo (42% desc): {Math.Round(celular.Precio * 0.58m, 2):C2}");
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
            throw new InvalidOperationException("Hay una venta previa pendiente. Reinténtela con la opción [8] o descártela con la opción [9].");

        var celulares = await MostrarCatalogo();
        if (celulares.Length == 0)
            throw new InvalidOperationException("Catálogo vacío. Debe registrar al menos un celular antes de vender.");

        Console.WriteLine();
        VistaConsola.Seccion("NUEVA TRANSACCIÓN DE VENTA");
        var id = VistaConsola.Entero("Seleccione el ID del celular a comprar");
        var seleccionado = celulares.SingleOrDefault(c => c.Id == id)
            ?? throw new ArgumentException("El ID seleccionado no existe en el catálogo activo.");

        var cedula = VistaConsola.Texto("Cédula del comprador");
        if (string.IsNullOrWhiteSpace(cedula)) throw new ArgumentException("La cédula es requerida.");

        // Consulta de elegibilidad y autocompletado en BanQuito
        var evaluacion = await tienda.EvaluarCredito(cedula);
        Mostrar(evaluacion);

        var nombreSugerido = !string.IsNullOrWhiteSpace(evaluacion.Nombre) ? evaluacion.Nombre : "";
        var nombre = VistaConsola.Texto($"Nombre del comprador {(string.IsNullOrEmpty(nombreSugerido) ? "" : $"[Presione ENTER para usar '{nombreSugerido}']")}");
        if (string.IsNullOrWhiteSpace(nombre)) nombre = nombreSugerido;
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
            if (!evaluacion.SujetoCredito)
            {
                VistaConsola.Aviso($"BanQuito NO aprobó crédito para la cédula {cedula}: {evaluacion.Motivo}");
                if (!VistaConsola.Confirmar("¿Desea cambiar la forma de pago a EFECTIVO con 42% de descuento?"))
                    return;
                esCredito = false;
            }
            else if (seleccionado.Precio > evaluacion.MontoMaximo)
            {
                VistaConsola.Aviso($"El precio del modelo ({seleccionado.Precio:C2}) supera el cupo aprobado por BanQuito ({evaluacion.MontoMaximo:C2}).");
                if (!VistaConsola.Confirmar("¿Desea cambiar la forma de pago a EFECTIVO con 42% de descuento?"))
                    return;
                esCredito = false;
            }
            else
            {
                cuotas = VistaConsola.Entero("Número de cuotas mensuales (3 a 18)", 3, 18);
            }
        }

        var total = esCredito ? seleccionado.Precio : ControladorTienda.TotalEfectivo(seleccionado.Precio);
        var descuento = esCredito ? 0m : Math.Round(seleccionado.Precio * 0.42m, 2);

        Console.WriteLine();
        Console.WriteLine("  ╔══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║                      RESUMEN DE LA TRANSACCIÓN                           ║");
        Console.WriteLine("  ╠══════════════════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"  ║  Cliente:    {nombre} (Cédula: {cedula})".PadRight(75) + "║");
        Console.WriteLine($"  ║  Equipo:     {seleccionado.Nombre}".PadRight(75) + "║");
        Console.WriteLine($"  ║  Pago:       {(esCredito ? $"Crédito BanQuito ({cuotas} cuotas)" : "Efectivo al contado (42% desc.)")}".PadRight(75) + "║");
        Console.WriteLine($"  ║  Precio:     {seleccionado.Precio:C2} · Descuento: {descuento:C2} · TOTAL: {total:C2}".PadRight(75) + "║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════════════════════════════╝");

        if (!VistaConsola.Confirmar("¿Confirmar y emitir la factura?")) return;

        pendiente = new SolicitudFactura
        {
            Cedula = cedula.Trim(),
            NombreCliente = nombre.Trim(),
            IdCelular = seleccionado.Id,
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
        Console.WriteLine($"  ║  Equipo:      {factura.Celular}".PadRight(75) + "║");
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
