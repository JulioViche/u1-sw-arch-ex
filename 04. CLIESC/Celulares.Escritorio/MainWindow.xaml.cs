using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Celulares.Cliente.Controladores;
using Celulares.Cliente.Servicios;
using Celulares.Contratos;

namespace Celulares.Escritorio;

public class CelularCardModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; init; }
    public string Codigo => $"CELULAR · {Id:D3}";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public string PrecioListaTexto => Precio.ToString("C2");
    public decimal PrecioEfectivo => Math.Round(Precio * 0.58m, 2);
    public string PrecioEfectivoTexto => $"Efectivo (42% desc): {PrecioEfectivo:C2}";
    public string? ImagenUrl { get; set; }

    private ImageSource? _fotoSource;
    public ImageSource? FotoSource
    {
        get => _fotoSource;
        set
        {
            _fotoSource = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FotoSource)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FotoVisible)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlaceholderVisible)));
        }
    }

    public Visibility FotoVisible => FotoSource != null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PlaceholderVisible => FotoSource == null ? Visibility.Visible : Visibility.Collapsed;
    public Celular ModeloOriginal { get; init; } = default!;
}

public partial class MainWindow : Window
{
    private readonly ControladorTienda tienda;
    private SolicitudFactura? pendiente;
    private bool temaClaro;
    private CelularCardModel? celularEnEdicion;
    private CelularCardModel? celularEnDesactivacion;

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

    private async void AlCargar(object sender, RoutedEventArgs e)
    {
        ActualizarEstiloTabs(esCatalogo: true);
        await CargarCatalogo();
    }

    private async void ActualizarCatalogo(object sender, RoutedEventArgs e)
    {
        if (BotonActualizar != null)
        {
            BotonActualizar.IsEnabled = false;
            BotonActualizar.Content = "🔄  Actualizando...";
        }
        try
        {
            await CargarCatalogo();
        }
        finally
        {
            if (BotonActualizar != null)
            {
                BotonActualizar.IsEnabled = true;
                BotonActualizar.Content = "🔄  Actualizar";
            }
        }
    }

    // =========================================================================
    // NAVEGACIÓN ENTRE VISTAS (CATÁLOGO vs VENTAS vs FACTURA)
    // =========================================================================
    private void MostrarTabCatalogo(object sender, RoutedEventArgs e)
    {
        VistaCatalogo.Visibility = Visibility.Visible;
        VistaVentas.Visibility = Visibility.Collapsed;
        VistaDetalleFactura.Visibility = Visibility.Collapsed;
        ActualizarEstiloTabs(esCatalogo: true);
    }

    private void MostrarTabVentas(object sender, RoutedEventArgs e)
    {
        VistaCatalogo.Visibility = Visibility.Collapsed;
        VistaVentas.Visibility = Visibility.Visible;
        VistaDetalleFactura.Visibility = Visibility.Collapsed;
        ActualizarEstiloTabs(esCatalogo: false);
    }

    private void VolverDeFactura(object sender, RoutedEventArgs e)
    {
        VistaDetalleFactura.Visibility = Visibility.Collapsed;
        VistaVentas.Visibility = Visibility.Visible;
    }

    private void ActualizarEstiloTabs(bool esCatalogo)
    {
        if (TabCatalogoBtn == null || TabVentasBtn == null) return;
        if (esCatalogo)
        {
            TabCatalogoBtn.Background = (Brush)Resources["AccentBrush"];
            TabCatalogoBtn.Foreground = (Brush)Resources["ButtonTextBrush"];
            TabVentasBtn.Background = Brushes.Transparent;
            TabVentasBtn.Foreground = (Brush)Resources["MutedBrush"];
        }
        else
        {
            TabVentasBtn.Background = (Brush)Resources["AccentBrush"];
            TabVentasBtn.Foreground = (Brush)Resources["ButtonTextBrush"];
            TabCatalogoBtn.Background = Brushes.Transparent;
            TabCatalogoBtn.Foreground = (Brush)Resources["MutedBrush"];
        }
    }

    // =========================================================================
    // GESTIÓN DEL CATÁLOGO (REJILLA DE TARJETAS IDÉNTICA A LA WEB)
    // =========================================================================
    private async Task CargarCatalogo()
    {
        try
        {
            var datos = await tienda.Catalogo();
            CelularVenta.ItemsSource = datos;

            if (datos.Length == 0)
            {
                EmptyCatalogoAviso.Visibility = Visibility.Visible;
                ListaTarjetasCatalogo.Visibility = Visibility.Collapsed;
                Estado.Text = "Catálogo vacío. Registre un celular para comenzar.";
                PrecioSeleccionado.Text = "Elige un celular para ver el total.";
                return;
            }

            EmptyCatalogoAviso.Visibility = Visibility.Collapsed;
            ListaTarjetasCatalogo.Visibility = Visibility.Visible;

            var listaCards = datos.Select(c => new CelularCardModel
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Precio = c.Precio,
                ImagenUrl = c.ImagenUrl,
                ModeloOriginal = c
            }).ToList();

            ListaTarjetasCatalogo.ItemsSource = listaCards;
            Estado.Text = $"Catálogo actualizado · {datos.Length} celulares activos disponibles.";

            // Carga asíncrona de fotos en paralelo para no bloquear la interfaz
            foreach (var card in listaCards.Where(c => c.ImagenUrl != null))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var bytes = await tienda.DescargarImagen(card.ModeloOriginal);
                        if (bytes.Length > 0)
                        {
                            var imagen = Imagen(bytes);
                            Dispatcher.Invoke(() => card.FotoSource = imagen);
                        }
                    }
                    catch { /* omitir si la imagen no está disponible */ }
                });
            }
        }
        catch (Exception error)
        {
            Estado.Text = "No se pudo conectar con la comercializadora: " + error.Message;
        }
    }

    // =========================================================================
    // ACCIONES DE CADA TARJETA: FOTO, EDICIÓN Y DESACTIVACIÓN
    // =========================================================================
    private async void CargarFotoTarjeta(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CelularCardModel card) return;

        var dialogo = new OpenFileDialog
        {
            Title = $"Seleccione una foto para {card.Nombre}",
            Filter = "Imágenes (JPEG, PNG, WebP)|*.jpg;*.jpeg;*.png;*.webp",
            CheckFileExists = true
        };

        if (dialogo.ShowDialog() != true) return;

        try
        {
            var info = new FileInfo(dialogo.FileName);
            if (info.Length > 5 * 1024 * 1024) throw new ArgumentException("La foto debe pesar hasta 5 MB.");

            var datos = await File.ReadAllBytesAsync(dialogo.FileName);
            var tipo = TipoImagen(dialogo.FileName);

            using var flujo = new MemoryStream(datos);
            await tienda.SubirImagen(card.Id, flujo, Path.GetFileName(dialogo.FileName), tipo);

            card.FotoSource = Imagen(datos);
            Estado.Text = $"Foto guardada en SQL Server para '{card.Nombre}'.";
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "Error al subir foto", MessageBoxButton.OK, MessageBoxImage.Warning);
            Estado.Text = error.Message;
        }
    }

    private async void QuitarFotoTarjeta(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CelularCardModel card) return;

        if (MessageBox.Show($"¿Eliminar la foto de {card.Nombre}?", "Quitar foto", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            await tienda.QuitarImagen(card.Id);
            card.FotoSource = null;
            Estado.Text = $"Foto eliminada de {card.Nombre}.";
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            Estado.Text = error.Message;
        }
    }

    private void EditarCelularTarjeta(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CelularCardModel card) return;

        celularEnEdicion = card;
        EditModalTag.Text = card.Codigo;
        EditModalNombre.Text = card.Nombre;
        EditModalPrecio.Text = card.Precio.ToString("F2", CultureInfo.InvariantCulture);
        ActualizarPreviewModalEditar();
        ModalEditar.Visibility = Visibility.Visible;
        EditModalNombre.Focus();
    }

    private void DesactivarCelularTarjeta(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CelularCardModel card) return;

        celularEnDesactivacion = card;
        ModalDesactivarNombre.Text = card.Nombre;
        ModalDesactivar.Visibility = Visibility.Visible;
    }

    // =========================================================================
    // MODALES OVERLAY (NUEVO, EDITAR, DESACTIVAR)
    // =========================================================================
    private void AbrirModalNuevo(object sender, RoutedEventArgs e)
    {
        NuevoModalNombre.Clear();
        NuevoModalPrecio.Clear();
        NuevoModalPreview.Text = "Efectivo (42% desc): $0.00";
        ModalNuevo.Visibility = Visibility.Visible;
        NuevoModalNombre.Focus();
    }

    private void CerrarModales(object sender, RoutedEventArgs e)
    {
        ModalNuevo.Visibility = Visibility.Collapsed;
        ModalEditar.Visibility = Visibility.Collapsed;
        ModalDesactivar.Visibility = Visibility.Collapsed;
        celularEnEdicion = null;
        celularEnDesactivacion = null;
    }

    private void NuevoPrecioCambio(object sender, TextChangedEventArgs e)
    {
        if (decimal.TryParse(NuevoModalPrecio.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) && p > 0)
            NuevoModalPreview.Text = $"Efectivo (42% desc): {(p * 0.58m):C2}";
        else
            NuevoModalPreview.Text = "Efectivo (42% desc): $0.00";
    }

    private async void ConfirmarModalNuevo(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevoModalNombre.Text))
            {
                MessageBox.Show("El nombre del modelo es requerido.", "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Information);
                NuevoModalNombre.Focus();
                return;
            }

            var precio = ControladorTienda.InterpretarPrecio(NuevoModalPrecio.Text);
            var creado = await tienda.Guardar(null, NuevoModalNombre.Text, precio);

            ModalNuevo.Visibility = Visibility.Collapsed;
            await CargarCatalogo();
            Estado.Text = $"Celular '{creado.Nombre}' registrado exitosamente en el catálogo.";
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "Error al registrar celular", MessageBoxButton.OK, MessageBoxImage.Warning);
            Estado.Text = "Error al crear celular: " + error.Message;
        }
    }

    private void EditarPrecioCambio(object sender, TextChangedEventArgs e) => ActualizarPreviewModalEditar();

    private void ActualizarPreviewModalEditar()
    {
        if (decimal.TryParse(EditModalPrecio.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) && p > 0)
            EditModalPreview.Text = $"Efectivo (42% desc): {(p * 0.58m):C2}";
        else
            EditModalPreview.Text = "Efectivo (42% desc): $0.00";
    }

    private async void ConfirmarModalEditar(object sender, RoutedEventArgs e)
    {
        if (celularEnEdicion == null) return;
        try
        {
            if (string.IsNullOrWhiteSpace(EditModalNombre.Text))
            {
                MessageBox.Show("El nombre del modelo es requerido.", "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Information);
                EditModalNombre.Focus();
                return;
            }

            var precio = ControladorTienda.InterpretarPrecio(EditModalPrecio.Text);
            var actualizado = await tienda.Guardar(celularEnEdicion.Id, EditModalNombre.Text, precio);

            ModalEditar.Visibility = Visibility.Collapsed;
            celularEnEdicion = null;
            await CargarCatalogo();
            Estado.Text = $"Celular '{actualizado.Nombre}' modificado correctamente.";
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "Error al guardar cambios", MessageBoxButton.OK, MessageBoxImage.Warning);
            Estado.Text = "Error al guardar cambios: " + error.Message;
        }
    }

    private async void ConfirmarModalDesactivar(object sender, RoutedEventArgs e)
    {
        if (celularEnDesactivacion == null) return;
        try
        {
            await tienda.Eliminar(celularEnDesactivacion.Id);
            ModalDesactivar.Visibility = Visibility.Collapsed;
            var nombre = celularEnDesactivacion.Nombre;
            celularEnDesactivacion = null;
            await CargarCatalogo();
            Estado.Text = $"Celular '{nombre}' desactivado del catálogo.";
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "Error al desactivar", MessageBoxButton.OK, MessageBoxImage.Warning);
            Estado.Text = "Error al desactivar: " + error.Message;
        }
    }

    // =========================================================================
    // UTILIDADES DE IMAGEN
    // =========================================================================
    private static BitmapImage Imagen(byte[] datos)
    {
        using var flujo = new MemoryStream(datos);
        var imagen = new BitmapImage();
        imagen.BeginInit();
        imagen.CacheOption = BitmapCacheOption.OnLoad;
        imagen.StreamSource = flujo;
        imagen.EndInit();
        imagen.Freeze();
        return imagen;
    }

    private static string TipoImagen(string ruta) => Path.GetExtension(ruta).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => throw new ArgumentException("Use un archivo JPEG, PNG o WebP.")
    };

    // =========================================================================
    // TEMAS VISUALES (CLARO Y OSCURO)
    // =========================================================================
    private void CambiarTema(object sender, RoutedEventArgs e)
    {
        temaClaro = !temaClaro;
        var tonos = temaClaro
            ? new Dictionary<string, string>
            {
                ["WindowBrush"] = "#F3F6FA", ["TextBrush"] = "#1B293A", ["MutedBrush"] = "#52657A",
                ["AccentBrush"] = "#087F78", ["WarningBrush"] = "#76520A", ["ButtonTextBrush"] = "#FFFFFF",
                ["InputBrush"] = "#FFFFFF", ["GridBrush"] = "#FFFFFF", ["RowBrush"] = "#F8FAFC",
                ["AltRowBrush"] = "#EAF0F6", ["HeaderBrush"] = "#DFE8F1", ["PanelBrush"] = "#FFFFFF",
                ["CardBrush"] = "#F8FAFC", ["EdgeBrush"] = "#CBD5E1", ["SuccessBrush"] = "#16A34A",
                ["DangerBrush"] = "#DC2626"
            }
            : new Dictionary<string, string>
            {
                ["WindowBrush"] = "#080D18", ["TextBrush"] = "#F5F7FB", ["MutedBrush"] = "#C6D0E0",
                ["AccentBrush"] = "#52E0D2", ["WarningBrush"] = "#FFCD70", ["ButtonTextBrush"] = "#09111F",
                ["InputBrush"] = "#10182A", ["GridBrush"] = "#10182A", ["RowBrush"] = "#111B2D",
                ["AltRowBrush"] = "#16233B", ["HeaderBrush"] = "#18263E", ["PanelBrush"] = "#0E182A",
                ["CardBrush"] = "#121F35", ["EdgeBrush"] = "#2D3E56", ["SuccessBrush"] = "#4ADE80",
                ["DangerBrush"] = "#F87171"
            };

        foreach (var (nombre, color) in tonos)
            Resources[nombre] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

        ThemeToggle.Content = temaClaro ? "☀️  Tema oscuro" : "🌓  Tema claro";
        ActualizarEstiloTabs(VistaCatalogo.Visibility == Visibility.Visible);
    }

    // =========================================================================
    // VENTAS Y EVALUACIÓN DE CRÉDITO BANQUITO
    // =========================================================================
    private void CelularVentaSeleccion(object sender, SelectionChangedEventArgs e) => ActualizarPrecioVenta();

    private void CambioFormaPago(object sender, SelectionChangedEventArgs e)
    {
        var esCredito = (FormaPago.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "CREDITO_DIRECTO";
        if (GrupoCuotas != null) GrupoCuotas.Visibility = esCredito ? Visibility.Visible : Visibility.Collapsed;
        ActualizarPrecioVenta();
    }

    private void ActualizarPrecioVenta()
    {
        if (CelularVenta.SelectedItem is not Celular celular || FormaPago.SelectedItem is not ComboBoxItem pago)
        {
            PrecioSeleccionado.Text = "Elige un celular para ver el total.";
            return;
        }

        if (pago.Tag?.ToString() == "EFECTIVO")
        {
            var total = ControladorTienda.TotalEfectivo(celular.Precio);
            PrecioSeleccionado.Text = $"Precio de lista {celular.Precio:C2} · Descuento 42% · Total en efectivo: {total:C2}";
        }
        else
        {
            PrecioSeleccionado.Text = $"Precio financiado {celular.Precio:C2} · Interés anual 16,5% · 3 a 18 cuotas fijas.";
        }
    }

    private async void EvaluarCredito(object sender, RoutedEventArgs e)
    {
        var cedula = Cedula.Text.Trim();
        if (string.IsNullOrWhiteSpace(cedula))
        {
            MessageBox.Show("Ingrese el número de cédula del comprador para consultar con BanQuito.", "Cédula requerida", MessageBoxButton.OK, MessageBoxImage.Information);
            Cedula.Focus();
            return;
        }

        try
        {
            var evaluacion = await tienda.EvaluarCredito(cedula);
            Mostrar(evaluacion);
            if (!string.IsNullOrWhiteSpace(evaluacion.Nombre)) Comprador.Text = evaluacion.Nombre;
        }
        catch (Exception error)
        {
            Elegibilidad.Text = "No se pudo verificar con BanQuito: " + error.Message;
            MessageBox.Show($"Error al consultar BanQuito:\n\n{error.Message}", "Consulta de crédito", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Vender(object sender, RoutedEventArgs e)
    {
        BotonVender.IsEnabled = false;
        try
        {
            var cedula = Cedula.Text.Trim();
            if (string.IsNullOrWhiteSpace(cedula))
            {
                MessageBox.Show("Ingrese la cédula del comprador antes de continuar.", "Cédula requerida", MessageBoxButton.OK, MessageBoxImage.Information);
                Cedula.Focus();
                return;
            }

            var comprador = Comprador.Text.Trim();
            if (string.IsNullOrWhiteSpace(comprador))
            {
                MessageBox.Show("Ingrese el nombre completo del comprador.", "Nombre requerido", MessageBoxButton.OK, MessageBoxImage.Information);
                Comprador.Focus();
                return;
            }

            if (CelularVenta.SelectedItem is not Celular celular)
            {
                MessageBox.Show("Seleccione un celular del catálogo para registrar la venta.", "Celular no seleccionado", MessageBoxButton.OK, MessageBoxImage.Information);
                CelularVenta.Focus();
                return;
            }

            var forma = (FormaPago.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "EFECTIVO";
            int? cuotas = null;

            if (forma == "CREDITO_DIRECTO")
            {
                if (!int.TryParse(Cuotas.Text.Trim(), out var nCuotas) || nCuotas is < 3 or > 18)
                {
                    MessageBox.Show("Para crédito BanQuito debe ingresar un número de cuotas entre 3 y 18 meses.", "Plazo de cuotas requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Cuotas.Focus();
                    return;
                }
                cuotas = nCuotas;

                // Evaluación directa con BanQuito
                var evaluacion = await tienda.EvaluarCredito(cedula);
                Mostrar(evaluacion);

                if (!evaluacion.SujetoCredito)
                {
                    MessageBox.Show($"BanQuito no aprobó la solicitud de crédito para la cédula {cedula}:\n\nMotivo: {evaluacion.Motivo}\n\nPara completar la venta puede seleccionar pago en Efectivo (con 42% de descuento) o usar una cédula aprobada (ej. 0102030405).", "Crédito no elegible en BanQuito", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Estado.Text = $"BanQuito: Crédito rechazado · {evaluacion.Motivo}";
                    return;
                }

                if (celular.Precio > evaluacion.MontoMaximo)
                {
                    MessageBox.Show($"El precio del modelo ({celular.Precio:C2}) excede el cupo crediticio máximo aprobado por BanQuito ({evaluacion.MontoMaximo:C2}) para este cliente.\n\nPor favor elija un modelo de menor valor o cambie la forma de pago a Efectivo.", "Cupo insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Estado.Text = $"BanQuito: Precio supera el cupo de {evaluacion.MontoMaximo:C2}.";
                    return;
                }
            }

            var reintento = pendiente is not null;
            var actual = new SolicitudFactura
            {
                Cedula = cedula,
                NombreCliente = comprador,
                IdCelular = celular.Id,
                FormaPago = forma,
                NumeroCuotas = cuotas,
                PrecioEsperado = celular.Precio
            };

            if (pendiente is not null && (pendiente.Cedula != actual.Cedula || pendiente.NombreCliente != actual.NombreCliente ||
                pendiente.IdCelular != actual.IdCelular || pendiente.FormaPago != actual.FormaPago ||
                pendiente.NumeroCuotas != actual.NumeroCuotas || pendiente.PrecioEsperado != actual.PrecioEsperado))
            {
                MessageBox.Show("Hay una venta previa pendiente de confirmación. Reinténtela con los mismos datos o inicie una 'Nueva venta' para descartar esa referencia.", "Venta pendiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var total = forma == "EFECTIVO" ? ControladorTienda.TotalEfectivo(celular.Precio) : celular.Precio;
            var mensajeConfirmacion = forma == "EFECTIVO"
                ? $"{celular.Nombre}\nForma de pago: Efectivo (42% desc. aplicado)\nTotal a facturar: {total:C2}\n\n¿Confirmar la venta y emitir la factura?"
                : $"{celular.Nombre}\nForma de pago: Crédito BanQuito ({cuotas} cuotas fijas)\nTotal financiado: {total:C2}\n\n¿Confirmar la venta y emitir la factura?";

            if (MessageBox.Show(mensajeConfirmacion, "Confirmar transacción", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            pendiente ??= actual;
            await EnviarPendiente();
        }
        catch (Exception error)
        {
            MessageBox.Show($"No se pudo registrar la venta:\n\n{error.Message}", "Aviso de facturación", MessageBoxButton.OK, MessageBoxImage.Warning);
            Estado.Text = "No se pudo completar la venta: " + error.Message;
        }
        finally
        {
            BotonVender.IsEnabled = true;
        }
    }

    private async void ReintentarVenta(object sender, RoutedEventArgs e)
    {
        if (pendiente is null)
        {
            MessageBox.Show("No hay una venta pendiente en esta sesión.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        BotonReintentar.IsEnabled = false;
        try { await EnviarPendiente(); }
        catch (Exception error)
        {
            MessageBox.Show($"La venta sigue pendiente. Reintente con los mismos datos:\n\n{error.Message}", "Reintento de venta", MessageBoxButton.OK, MessageBoxImage.Warning);
            Estado.Text = "La venta sigue pendiente: " + error.Message;
        }
        finally { BotonReintentar.IsEnabled = true; }
    }

    private void NuevaVenta(object sender, RoutedEventArgs e)
    {
        if (pendiente is not null && MessageBox.Show("La venta pudo registrarse. Reintente o consulte sus facturas antes de descartarla. ¿Descartar esta referencia?", "Venta pendiente", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        pendiente = null;
        Cedula.Clear(); Comprador.Clear(); Cuotas.Clear(); CelularVenta.SelectedItem = null;
        Elegibilidad.Text = "Ingrese la cédula y consulte si es sujeto a crédito en BanQuito.";
        Elegibilidad.Foreground = (Brush)Resources["AccentBrush"];
        ElegibilidadBox.BorderBrush = (Brush)Resources["EdgeBrush"];
        Estado.Text = "Nueva venta lista.";
    }

    // =========================================================================
    // CONSULTA DE COMPROBANTES Y HISTORIAL
    // =========================================================================
    private async void BuscarFacturas(object sender, RoutedEventArgs e)
    {
        var cedula = CedulaBusqueda.Text.Trim();
        if (string.IsNullOrWhiteSpace(cedula))
        {
            MessageBox.Show("Ingrese una cédula para consultar sus facturas históricas.", "Cédula requerida", MessageBoxButton.OK, MessageBoxImage.Information);
            CedulaBusqueda.Focus();
            return;
        }

        try
        {
            var facturas = await tienda.FacturasPorCedula(cedula);
            FacturasEncontradas.ItemsSource = facturas;
            HintBusqueda.Visibility = Visibility.Collapsed;
            Estado.Text = facturas.Length == 0 ? $"No hay facturas para la cédula {cedula}." : $"{facturas.Length} facturas encontradas. Seleccione una para ver detalle.";
            if (facturas.Length == 0)
            {
                MessageBox.Show($"No se registraron facturas para la cédula {cedula}.", "Sin facturas", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception error)
        {
            MessageBox.Show($"Error al consultar facturas: {error.Message}", "Error de consulta", MessageBoxButton.OK, MessageBoxImage.Warning);
            Estado.Text = error.Message;
        }
    }

    private async void AbrirFactura(object sender, RoutedEventArgs e)
    {
        if (FacturasEncontradas.SelectedItem is not Factura factura)
        {
            MessageBox.Show("Por favor seleccione una factura de la tabla para ver su comprobante.", "Factura no seleccionada", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await MostrarFactura(factura.Id);
            Estado.Text = $"Comprobante de Factura #{factura.Id} cargado.";
        }
        catch (Exception error)
        {
            MessageBox.Show($"Error al abrir la factura #{factura.Id}: {error.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            Estado.Text = error.Message;
        }
    }

    private async Task EnviarPendiente()
    {
        var factura = await tienda.Facturar(pendiente!);
        pendiente = null;
        await MostrarFactura(factura.Id);
        Estado.Text = $"Factura #{factura.Id} · {factura.Estado} · total {factura.Total:C2}.";
    }

    private async Task MostrarFactura(int id)
    {
        var factura = await tienda.Factura(id);
        FacturaSubtituloTexto.Text = $"COMPROBANTE · {factura.Fecha:yyyy-MM-dd}";
        FacturaIdTexto.Text = $"Factura #{factura.Id}";
        FacturaEstadoTexto.Text = factura.Estado;
        FacturaClienteTexto.Text = factura.NombreCliente;
        FacturaCedulaTexto.Text = $"Cédula {factura.Cedula}";
        FacturaCelularTexto.Text = factura.Celular;
        FacturaPagoTexto.Text = factura.FormaPago;
        FacturaPrecioTexto.Text = factura.Precio.ToString("C2");
        FacturaDescuentoTexto.Text = factura.Descuento > 0 ? $"-{factura.Descuento:C2}" : "$0.00";
        FacturaTotalTexto.Text = factura.Total.ToString("C2");

        var cuotas = factura.FormaPago == "CREDITO_DIRECTO" && factura.Estado == "APROBADA"
            ? await tienda.Amortizacion(id) : [];
        TablaCuotas.ItemsSource = cuotas;
        SeccionAmortizacion.Visibility = cuotas.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Mostrar la vista del voucher
        VistaCatalogo.Visibility = Visibility.Collapsed;
        VistaVentas.Visibility = Visibility.Collapsed;
        VistaDetalleFactura.Visibility = Visibility.Visible;
    }

    private void Mostrar(EvaluacionCredito evaluacion)
    {
        if (evaluacion.SujetoCredito)
        {
            Elegibilidad.Text = $"✓ BanQuito: Elegible · Cupo máximo: {evaluacion.MontoMaximo:C2}";
            Elegibilidad.Foreground = (Brush)Resources["SuccessBrush"];
            ElegibilidadBox.BorderBrush = (Brush)Resources["SuccessBrush"];
        }
        else
        {
            Elegibilidad.Text = $"✗ BanQuito: No elegible · {evaluacion.Motivo}";
            Elegibilidad.Foreground = (Brush)Resources["DangerBrush"];
            ElegibilidadBox.BorderBrush = (Brush)Resources["DangerBrush"];
        }
    }
}
