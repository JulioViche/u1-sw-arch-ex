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
        Console.Title = "Comercializadora de celulares · GR09";
        VistaConsola.AplicarTema();
        while (true)
        {
            VistaConsola.Encabezado();
            Console.WriteLine("1 Catálogo  ·  2 Guardar celular  ·  3 Desactivar celular");
            Console.WriteLine("4 Nueva venta  ·  5 Elegibilidad de crédito  ·  6 Buscar facturas por cédula");
            Console.WriteLine("7 Consultar factura  ·  8 Reintentar venta  ·  9 Descartar pendiente");
            Console.WriteLine("10 Cargar o reemplazar foto  ·  11 Quitar foto  ·  12 Cambiar tema  ·  0 Salir");
            var opcion = VistaConsola.Texto("Opción");
            if (opcion == "0") return;
            try
            {
                switch (opcion)
                {
                    case "1": await MostrarCatalogo(); break;
                    case "2": await GuardarCelular(); break;
                    case "3": await DesactivarCelular(); break;
                    case "4": await NuevaVenta(); break;
                    case "5": Mostrar(await tienda.EvaluarCredito(VistaConsola.Texto("Cédula"))); break;
                    case "6": await BuscarFacturas(); break;
                    case "7": await MostrarFactura(VistaConsola.Entero("Número de factura")); break;
                    case "8":
                        if (pendiente is null) Console.WriteLine("No hay una venta pendiente en esta sesión.");
                        else await EnviarPendiente();
                        break;
                    case "9":
                        if (pendiente is not null && VistaConsola.Confirmar("La venta pudo registrarse. Consulte antes de descartarla. ¿Continuar?")) pendiente = null;
                        break;
                    case "10": await GuardarFoto(); break;
                    case "11": await QuitarFoto(); break;
                    case "12": VistaConsola.CambiarTema(); break;
                    default: Console.WriteLine("Seleccione una opción del menú."); break;
                }
            }
            catch (Exception error) when (error is ArgumentException or FormatException or InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                Console.WriteLine("\nNo se pudo completar la operación: " + error.Message);
                if (pendiente is not null) Console.WriteLine("La venta conserva su identificador. Reintente con la opción 8.");
            }
        }
    }

    private async Task MostrarCatalogo()
    {
        var celulares = await tienda.Catalogo();
        if (celulares.Length == 0) { Console.WriteLine("Catálogo vacío."); return; }
        foreach (var celular in celulares) Console.WriteLine($"{celular.Id} · {celular.Nombre} · USD {celular.Precio:F2} · {(celular.ImagenUrl is null ? "sin foto" : "con foto")}");
    }

    private async Task GuardarCelular()
    {
        await MostrarCatalogo();
        var textoId = VistaConsola.Texto("ID existente para editar; vacío para crear");
        int? id = string.IsNullOrWhiteSpace(textoId) ? null :
            int.TryParse(textoId, out var existente) && existente > 0 ? existente : throw new ArgumentException("Ingrese un ID positivo del catálogo.");
        var celular = await tienda.Guardar(id, VistaConsola.Texto("Nombre"), VistaConsola.Dinero("Precio de lista"));
        Console.WriteLine($"Guardado: {celular.Nombre} · ${celular.Precio:F2}");
    }

    private async Task DesactivarCelular()
    {
        await MostrarCatalogo();
        var id = VistaConsola.Entero("ID del celular");
        if (!VistaConsola.Confirmar("¿Desactivar el celular?")) return;
        await tienda.Eliminar(id);
        Console.WriteLine("Celular desactivado.");
    }

    private async Task GuardarFoto()
    {
        var celulares = await tienda.Catalogo();
        foreach (var celular in celulares)
            Console.WriteLine($"{celular.Id} · {celular.Nombre} · {(celular.ImagenUrl is null ? "sin foto" : "con foto")}");
        var id = VistaConsola.Entero("ID del celular");
        var seleccionado = celulares.SingleOrDefault(c => c.Id == id)
            ?? throw new ArgumentException("Seleccione un ID del catálogo mostrado.");
        var ruta = VistaConsola.Texto("Ruta de la foto JPEG, PNG o WebP");
        var tipo = TipoImagen(ruta);
        var archivo = new FileInfo(ruta);
        if (!archivo.Exists) throw new ArgumentException("No se encontró el archivo indicado.");
        if (archivo.Length is 0 or > 5 * 1024 * 1024) throw new ArgumentException("La foto debe pesar hasta 5 MB.");
        await using var flujo = archivo.OpenRead();
        await tienda.SubirImagen(seleccionado.Id, flujo, archivo.Name, tipo);
        Console.WriteLine($"Foto guardada o reemplazada · {seleccionado.Nombre}.");
    }

    private async Task QuitarFoto()
    {
        var celulares = await tienda.Catalogo();
        foreach (var celular in celulares)
            Console.WriteLine($"{celular.Id} · {celular.Nombre} · {(celular.ImagenUrl is null ? "sin foto" : "con foto")}");
        var id = VistaConsola.Entero("ID del celular");
        var seleccionado = celulares.SingleOrDefault(c => c.Id == id)
            ?? throw new ArgumentException("Seleccione un ID del catálogo mostrado.");
        if (seleccionado.ImagenUrl is null) throw new InvalidOperationException("El celular no tiene foto.");
        if (!VistaConsola.Confirmar($"¿Quitar la foto de {seleccionado.Nombre}?")) return;
        await tienda.QuitarImagen(seleccionado.Id);
        Console.WriteLine("Foto eliminada.");
    }

    private static string TipoImagen(string ruta) => Path.GetExtension(ruta).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => throw new ArgumentException("Use una foto JPEG, PNG o WebP.")
    };
    private async Task NuevaVenta()
    {
        if (pendiente is not null) throw new InvalidOperationException("Resuelva la venta pendiente con la opción 8 o 9.");
        var celulares = await tienda.Catalogo();
        if (celulares.Length == 0) throw new InvalidOperationException("Catálogo vacío. Registre un celular antes de vender.");
        foreach (var celular in celulares) Console.WriteLine($"{celular.Id} · {celular.Nombre} · USD {celular.Precio:F2} · {(celular.ImagenUrl is null ? "sin foto" : "con foto")}");
        var id = VistaConsola.Entero("Seleccione ID del celular");
        var seleccionado = celulares.SingleOrDefault(c => c.Id == id)
            ?? throw new ArgumentException("Seleccione un ID del catálogo mostrado.");
        var cedula = VistaConsola.Texto("Cédula");
        var nombre = VistaConsola.Texto("Nombre del comprador");
        var forma = VistaConsola.Texto("Pago: 1 efectivo, 2 crédito");
        if (forma is not ("1" or "2")) throw new ArgumentException("Seleccione 1 o 2.");
        var credito = forma == "2";
        var cuotas = credito ? VistaConsola.Entero("Cuotas (3 a 18)") : (int?)null;
        var total = credito ? seleccionado.Precio : ControladorTienda.TotalEfectivo(seleccionado.Precio);
        Console.WriteLine($"\n{seleccionado.Nombre} · lista ${seleccionado.Precio:F2}");
        Console.WriteLine(credito ? "Crédito: 16,5% anual · plazo 3–18 meses." : $"Efectivo: total ${total:F2} · descuento ${seleccionado.Precio - total:F2}.");
        if (credito)
        {
            var evaluacion = await tienda.EvaluarCredito(cedula);
            Mostrar(evaluacion);
            if (!evaluacion.SujetoCredito || seleccionado.Precio > evaluacion.MontoMaximo)
                throw new InvalidOperationException("El crédito no cumple la elegibilidad o el monto máximo vigente.");
        }

        if (!VistaConsola.Confirmar("¿Confirmar la venta?")) return;
        pendiente = new SolicitudFactura
        {
            Cedula = cedula.Trim(), NombreCliente = nombre.Trim(), IdCelular = seleccionado.Id,
            FormaPago = credito ? "CREDITO_DIRECTO" : "EFECTIVO", NumeroCuotas = cuotas,
            PrecioEsperado = seleccionado.Precio
        };
        await EnviarPendiente();
    }

    private async Task BuscarFacturas()
    {
        var facturas = await tienda.FacturasPorCedula(VistaConsola.Texto("Cédula"));
        if (facturas.Length == 0) { Console.WriteLine("No hay facturas para esa cédula."); return; }
        foreach (var factura in facturas)
            Console.WriteLine($"{factura.Id} · {factura.Fecha:yyyy-MM-dd} · {factura.Celular} · ${factura.Total:F2} · {factura.Estado}");
        await MostrarFactura(VistaConsola.Entero("Seleccione número de factura"));
    }

    private async Task MostrarFactura(int id)
    {
        var factura = await tienda.Factura(id);
        Console.WriteLine($"\nFACTURA {factura.Id} · {factura.Estado}");
        Console.WriteLine($"{factura.NombreCliente} · {factura.Cedula} · {factura.Celular}");
        Console.WriteLine($"{factura.FormaPago} · precio ${factura.Precio:F2} · descuento ${factura.Descuento:F2} · total ${factura.Total:F2}");
        if (factura.FormaPago == "CREDITO_DIRECTO" && factura.Estado == "APROBADA")
            foreach (var cuota in await tienda.Amortizacion(id))
                Console.WriteLine($"Cuota {cuota.NumeroCuota} · {cuota.FechaPago:yyyy-MM-dd} · ${cuota.ValorCuota:F2} · interés ${cuota.InteresPagado:F2} · capital ${cuota.CapitalPagado:F2} · saldo ${cuota.Saldo:F2}");
    }

    private async Task EnviarPendiente()
    {
        var factura = await tienda.Facturar(pendiente!);
        pendiente = null;
        Console.WriteLine($"\nVenta {factura.Estado}: factura {factura.Id} · ${factura.Total:F2}.");
        await MostrarFactura(factura.Id);
    }

    private static void Mostrar(EvaluacionCredito evaluacion) =>
        Console.WriteLine(evaluacion.SujetoCredito
            ? $"BanQuito: elegible · máximo ${evaluacion.MontoMaximo:F2}. Sujeto a aprobación al vender."
            : $"BanQuito: no elegible · {evaluacion.Motivo}");
}
