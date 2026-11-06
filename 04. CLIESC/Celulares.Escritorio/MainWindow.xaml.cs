using System.Globalization;
using System.IO;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Celulares.Cliente.Controladores;
using Celulares.Cliente.Servicios;
using Celulares.Contratos;

namespace Celulares.Escritorio;

public partial class MainWindow : Window
{
    private readonly ControladorTienda tienda;
    private SolicitudFactura? pendiente;
    private byte[]? fotoPendiente;
    private string? nombreFotoPendiente;
    private bool temaClaro;

    public MainWindow()
    {
        InitializeComponent();
        var cliente = new HttpClient
        {
            BaseAddress = ClienteComercializadora.Direccion(Environment.GetEnvironmentVariable("CELULARES_API_URL")),
            Timeout = TimeSpan.FromSeconds(30)
        };
        tienda = new ControladorTienda(new ClienteComercializadora(cliente));
    }

    private async void AlCargar(object sender, RoutedEventArgs e) => await CargarCatalogo();
    private async void ActualizarCatalogo(object sender, RoutedEventArgs e) => await CargarCatalogo();

    private async Task CargarCatalogo(int? seleccionarId = null)
    {
        try
        {
            var datos = await tienda.Catalogo();
            Catalogo.ItemsSource = datos;
            CelularVenta.ItemsSource = datos;
            Catalogo.SelectedItem = datos.FirstOrDefault(c => c.Id == seleccionarId);
            Estado.Text = datos.Length == 0 ? "Catálogo vacío. Registre un celular." : $"Catálogo actualizado · {datos.Length} celulares disponibles.";
            if (datos.Length == 0) PrecioSeleccionado.Text = "";
        }
        catch (Exception error) { Estado.Text = "No se pudo conectar con la comercializadora: " + error.Message; }
    }

    private async void SeleccionarCelular(object sender, SelectionChangedEventArgs e)
    {
        if (Catalogo.SelectedItem is not Celular celular) { FotoCelular.Source = fotoPendiente is null ? null : Imagen(fotoPendiente); FotoEstado.Text = fotoPendiente is null ? "Seleccione un celular para administrar su foto." : "Foto lista; se guardará al crear el celular."; BotonQuitarFoto.IsEnabled = fotoPendiente is not null; return; }
        NombreCelular.Text = celular.Nombre;
        PrecioCelular.Text = celular.Precio.ToString("F2", CultureInfo.CurrentCulture);
        await CargarFotoExistente(celular);
    }

    private async Task CargarFotoExistente(Celular celular)
    {
        FotoCelular.Source = null;
        BotonQuitarFoto.IsEnabled = celular.ImagenUrl is not null;
        if (celular.ImagenUrl is null) { FotoEstado.Text = "Este celular aún no tiene foto."; return; }
        try
        {
            var datos = await tienda.DescargarImagen(celular);
            if (Catalogo.SelectedItem is Celular actual && actual.Id == celular.Id) FotoCelular.Source = Imagen(datos);
            FotoEstado.Text = "Foto guardada en SQL Server.";
        }
        catch (Exception error) { FotoEstado.Text = "No se pudo cargar la foto: " + error.Message; }
    }

    private async void CargarFoto(object sender, RoutedEventArgs e)
    {
        var dialogo = new OpenFileDialog { Title = "Seleccione una foto del celular", Filter = "Imágenes (JPEG, PNG, WebP)|*.jpg;*.jpeg;*.png;*.webp", CheckFileExists = true };
        if (dialogo.ShowDialog() != true) return;
        try
        {
            if (new FileInfo(dialogo.FileName).Length > 5 * 1024 * 1024) throw new ArgumentException("La foto debe pesar hasta 5 MB.");
            var datos = await File.ReadAllBytesAsync(dialogo.FileName);
            var tipo = TipoImagen(dialogo.FileName);
            if (datos.Length is 0 or > 5 * 1024 * 1024) throw new ArgumentException("La foto debe pesar hasta 5 MB.");
            if (Catalogo.SelectedItem is Celular celular)
            {
                using var flujo = new MemoryStream(datos);
                await tienda.SubirImagen(celular.Id, flujo, Path.GetFileName(dialogo.FileName), tipo);
                await CargarCatalogo(celular.Id);
                FotoEstado.Text = "Foto guardada o reemplazada.";
                Estado.Text = $"Foto actualizada · {celular.Nombre}.";
            }
            else
            {
                fotoPendiente = datos;
                nombreFotoPendiente = Path.GetFileName(dialogo.FileName);
                FotoCelular.Source = Imagen(datos);
                FotoEstado.Text = "Vista previa. Guarde el celular para subir la foto.";
                BotonQuitarFoto.IsEnabled = true;
            }
        }
        catch (Exception error) { Estado.Text = error.Message; }
    }

    private async void QuitarFoto(object sender, RoutedEventArgs e)
    {
        if (fotoPendiente is not null && Catalogo.SelectedItem is not Celular)
        {
            fotoPendiente = null; nombreFotoPendiente = null; FotoCelular.Source = null;
            FotoEstado.Text = "Foto pendiente descartada."; BotonQuitarFoto.IsEnabled = false; return;
        }
        if (Catalogo.SelectedItem is not Celular celular || celular.ImagenUrl is null) { FotoEstado.Text = "Seleccione un celular con foto."; return; }
        if (MessageBox.Show($"¿Quitar la foto de {celular.Nombre}?", "Quitar foto", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { await tienda.QuitarImagen(celular.Id); await CargarCatalogo(celular.Id); Estado.Text = "Foto eliminada."; }
        catch (Exception error) { Estado.Text = error.Message; }
    }

    private static BitmapImage Imagen(byte[] datos)
    {
        using var flujo = new MemoryStream(datos);
        var imagen = new BitmapImage();
        imagen.BeginInit(); imagen.CacheOption = BitmapCacheOption.OnLoad; imagen.StreamSource = flujo;
        imagen.EndInit(); imagen.Freeze();
        return imagen;
    }

    private static string TipoImagen(string ruta) => Path.GetExtension(ruta).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => throw new ArgumentException("Use un archivo JPEG, PNG o WebP.")
    };

    private void CambiarTema(object sender, RoutedEventArgs e)
    {
        temaClaro = !temaClaro;
        var tonos = temaClaro
            ? new Dictionary<string, string> { ["WindowBrush"]="#F3F6FA", ["TextBrush"]="#1B293A", ["MutedBrush"]="#52657A", ["AccentBrush"]="#087F78", ["WarningBrush"]="#76520A", ["ButtonTextBrush"]="#FFFFFF", ["InputBrush"]="#FFFFFF", ["GridBrush"]="#FFFFFF", ["RowBrush"]="#F8FAFC", ["AltRowBrush"]="#EAF0F6", ["HeaderBrush"]="#DFE8F1", ["PanelBrush"]="#EEF3F8", ["EdgeBrush"]="#AAB9CA" }
            : new Dictionary<string, string> { ["WindowBrush"]="#080D18", ["TextBrush"]="#F5F7FB", ["MutedBrush"]="#C6D0E0", ["AccentBrush"]="#52E0D2", ["WarningBrush"]="#FFCD70", ["ButtonTextBrush"]="#09111F", ["InputBrush"]="#10182A", ["GridBrush"]="#111B2D", ["RowBrush"]="#121E32", ["AltRowBrush"]="#18263B", ["HeaderBrush"]="#1C2C43", ["PanelBrush"]="#D1121C30", ["EdgeBrush"]="#52627A" };
        foreach (var (nombre, color) in tonos) Resources[nombre] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        ThemeToggle.Content = temaClaro ? "Tema oscuro" : "Tema claro";
    }
    private void CelularVentaSeleccion(object sender, SelectionChangedEventArgs e) => ActualizarPrecioVenta();
    private void CambioFormaPago(object sender, SelectionChangedEventArgs e) => ActualizarPrecioVenta();

    private void ActualizarPrecioVenta()
    {
        if (CelularVenta.SelectedItem is not Celular celular || FormaPago.SelectedItem is not ComboBoxItem pago)
        {
            PrecioSeleccionado.Text = "Seleccione un celular para ver su precio.";
            return;
        }

        if (pago.Tag?.ToString() == "EFECTIVO")
        {
            var total = ControladorTienda.TotalEfectivo(celular.Precio);
            PrecioSeleccionado.Text = $"Lista {celular.Precio:C2} · descuento 42% · total {total:C2}";
        }
        else PrecioSeleccionado.Text = $"Crédito {celular.Precio:C2} · 16,5% anual · 3 a 18 cuotas";
    }

    private void NuevoCelular(object sender, RoutedEventArgs e)
    {
        Catalogo.SelectedItem = null;
        NombreCelular.Clear();
        PrecioCelular.Clear();
        Estado.Text = "Nuevo celular. Complete nombre y precio de lista.";
    }

    private async void GuardarCelular(object sender, RoutedEventArgs e)
    {
        try
        {
            var precio = ControladorTienda.InterpretarPrecio(PrecioCelular.Text);
            var id = (Catalogo.SelectedItem as Celular)?.Id;
            var celular = await tienda.Guardar(id, NombreCelular.Text, precio);
            if (fotoPendiente is not null)
            {
                using var flujo = new MemoryStream(fotoPendiente);
                await tienda.SubirImagen(celular.Id, flujo, nombreFotoPendiente ?? "foto.jpg", TipoImagen(nombreFotoPendiente ?? "foto.jpg"));
                fotoPendiente = null; nombreFotoPendiente = null;
            }
            await CargarCatalogo(celular.Id);
            Estado.Text = $"Celular guardado: {celular.Nombre} · {celular.Precio:C2}.";
        }
        catch (Exception error) { Estado.Text = error.Message; }
    }

    private async void DesactivarCelular(object sender, RoutedEventArgs e)
    {
        if (Catalogo.SelectedItem is not Celular celular) { Estado.Text = "Seleccione un celular del catálogo."; return; }
        if (MessageBox.Show($"¿Desactivar {celular.Nombre}?", "Confirmar desactivación", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { await tienda.Eliminar(celular.Id); await CargarCatalogo(); Estado.Text = "Celular desactivado."; }
        catch (Exception error) { Estado.Text = error.Message; }
    }

    private async void EvaluarCredito(object sender, RoutedEventArgs e)
    {
        try { Mostrar(await tienda.EvaluarCredito(Cedula.Text)); }
        catch (Exception error) { Elegibilidad.Text = "No se pudo verificar con BanQuito: " + error.Message; }
    }

    private async void Vender(object sender, RoutedEventArgs e)
    {
        BotonVender.IsEnabled = false;
        try
        {
            var reintento = pendiente is not null;
            if (CelularVenta.SelectedItem is not Celular celular) throw new ArgumentException("Seleccione un celular del catálogo.");
            var forma = (FormaPago.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "EFECTIVO";
            int? cuotas = forma == "CREDITO_DIRECTO" ? Entero(Cuotas.Text) : null;
            var actual = new SolicitudFactura
            {
                Cedula = Cedula.Text.Trim(), NombreCliente = Comprador.Text.Trim(), IdCelular = celular.Id,
                FormaPago = forma, NumeroCuotas = cuotas, PrecioEsperado = celular.Precio
            };
            if (pendiente is not null && (pendiente.Cedula != actual.Cedula || pendiente.NombreCliente != actual.NombreCliente ||
                pendiente.IdCelular != actual.IdCelular || pendiente.FormaPago != actual.FormaPago ||
                pendiente.NumeroCuotas != actual.NumeroCuotas || pendiente.PrecioEsperado != actual.PrecioEsperado))
                throw new InvalidOperationException("Hay una venta pendiente. Reintente la misma operación o resuélvala antes de cambiar los datos.");

            if (forma == "CREDITO_DIRECTO" && !reintento)
            {
                var evaluacion = await tienda.EvaluarCredito(actual.Cedula);
                Mostrar(evaluacion);
                if (!evaluacion.SujetoCredito || celular.Precio > evaluacion.MontoMaximo)
                    throw new InvalidOperationException("Crédito no elegible o precio superior al máximo de BanQuito.");
            }

            var total = forma == "EFECTIVO" ? ControladorTienda.TotalEfectivo(celular.Precio) : celular.Precio;
            if (MessageBox.Show($"{celular.Nombre}\n{forma}\nTotal: {total:C2}\n\n¿Confirmar venta?", "Confirmar venta", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            pendiente ??= actual;
            await EnviarPendiente();
        }
        catch (Exception error) { Estado.Text = "No se pudo completar la venta: " + error.Message; }
        finally { BotonVender.IsEnabled = true; }
    }

    private async void ReintentarVenta(object sender, RoutedEventArgs e)
    {
        if (pendiente is null) { Estado.Text = "No hay una venta pendiente en esta sesión."; return; }
        BotonReintentar.IsEnabled = false;
        try { await EnviarPendiente(); }
        catch (Exception error) { Estado.Text = "La venta sigue pendiente. Reintente con los mismos datos: " + error.Message; }
        finally { BotonReintentar.IsEnabled = true; }
    }

    private void NuevaVenta(object sender, RoutedEventArgs e)
    {
        if (pendiente is not null && MessageBox.Show("La venta pudo registrarse. Reintente o consulte sus facturas antes de descartarla. ¿Descartar esta referencia?", "Venta pendiente", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        pendiente = null;
        Cedula.Clear(); Comprador.Clear(); Cuotas.Clear(); CelularVenta.SelectedItem = null;
        Elegibilidad.Text = "";
        Estado.Text = "Nueva venta lista.";
    }

    private async void BuscarFacturas(object sender, RoutedEventArgs e)
    {
        try
        {
            var facturas = await tienda.FacturasPorCedula(CedulaBusqueda.Text);
            FacturasEncontradas.ItemsSource = facturas;
            Estado.Text = facturas.Length == 0 ? "No hay facturas para esa cédula." : $"{facturas.Length} facturas encontradas. Seleccione una para ver detalle.";
        }
        catch (Exception error) { Estado.Text = error.Message; }
    }

    private async void AbrirFactura(object sender, RoutedEventArgs e)
    {
        if (FacturasEncontradas.SelectedItem is not Factura factura) { Estado.Text = "Busque y seleccione una factura."; return; }
        try { await MostrarFactura(factura.Id); Estado.Text = $"Factura {factura.Id} consultada."; }
        catch (Exception error) { Estado.Text = error.Message; }
    }

    private async Task EnviarPendiente()
    {
        var factura = await tienda.Facturar(pendiente!);
        pendiente = null;
        await MostrarFactura(factura.Id);
        Estado.Text = $"Factura {factura.Id} · {factura.Estado} · total {factura.Total:C2}.";
    }

    private async Task MostrarFactura(int id)
    {
        var factura = await tienda.Factura(id);
        DetalleFactura.Text = $"FACTURA {factura.Id} · {factura.Estado}\n{factura.NombreCliente} · {factura.Cedula} · {factura.Celular}\n{factura.FormaPago} · precio {factura.Precio:C2} · descuento {factura.Descuento:C2} · total {factura.Total:C2}";
        TablaCuotas.ItemsSource = factura.FormaPago == "CREDITO_DIRECTO" && factura.Estado == "APROBADA"
            ? await tienda.Amortizacion(id) : [];
    }

    private void Mostrar(EvaluacionCredito evaluacion) => Elegibilidad.Text = evaluacion.SujetoCredito
        ? $"BanQuito: elegible · máximo {evaluacion.MontoMaximo:C2}. La venta vuelve a validar el crédito."
        : $"BanQuito: no elegible · {evaluacion.Motivo}";

    private static int Entero(string? valor) => int.TryParse(valor, out var numero) && numero is >= 3 and <= 18
        ? numero : throw new ArgumentException("Ingrese entre 3 y 18 cuotas.");
}
