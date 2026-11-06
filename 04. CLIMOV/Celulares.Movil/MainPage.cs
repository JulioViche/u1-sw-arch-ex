using System.Globalization;
using System.IO;
using Celulares.Cliente.Controladores;
using Celulares.Cliente.Servicios;
using Celulares.Contratos;
using Microsoft.Maui.Controls.Shapes;

namespace Celulares.Movil;

public sealed class MainPage : Shell
{
    private static readonly Color FondoClaro = Color.FromArgb("#F3F6FA");
    private static readonly Color PanelClaro = Colors.White;
    private static readonly Color CampoClaro = Color.FromArgb("#EAF1F6");
    private static readonly Color TextoClaro = Color.FromArgb("#1B293A");
    private static readonly Color SecundarioClaro = Color.FromArgb("#52657A");
    private static readonly Color AcentoClaro = Color.FromArgb("#087F78");
    private static readonly Color FondoOscuro = Color.FromArgb("#080D18");
    private static readonly Color PanelOscuro = Color.FromArgb("#121C30");
    private static readonly Color CampoOscuro = Color.FromArgb("#1C2940");
    private static readonly Color TextoOscuro = Color.FromArgb("#F5F7FB");
    private static readonly Color SecundarioOscuro = Color.FromArgb("#BAC8DB");
    private static readonly Color AcentoOscuro = Color.FromArgb("#52E0D2");

    private readonly VerticalStackLayout tarjetasCatalogo = new() { Spacing = 14 };
    private readonly VerticalStackLayout listaFacturas = new() { Spacing = 12 };
    private readonly VerticalStackLayout amortizacion = new() { Spacing = 8 };
    private readonly Dictionary<string, Border> opcionesMenu = new();
    private readonly Entry nombre = Campo("Nombre del celular");
    private readonly Entry precio = Campo("Precio de lista · USD", Keyboard.Numeric);
    private readonly Entry cedula = Campo("Cédula del comprador", Keyboard.Numeric);
    private readonly Entry comprador = Campo("Nombre completo");
    private readonly Entry cuotas = Campo("Cuotas · de 3 a 18", Keyboard.Numeric);
    private readonly Entry busqueda = Campo("Cédula del comprador", Keyboard.Numeric);
    private readonly Picker celularVenta = new() { Title = "Seleccione un celular", ItemDisplayBinding = new Binding(nameof(Celular.Nombre)) };
    private readonly Picker forma = new() { Title = "Forma de pago", ItemsSource = new[] { "Efectivo", "Crédito directo" }, SelectedIndex = 0 };
    private readonly Image foto = new() { HeightRequest = 170, Aspect = Aspect.AspectFit, Source = "telefono.png" };
    private readonly Label avisoCatalogo = new() { FontSize = 13, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label avisoVenta = new() { FontSize = 13, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label avisoFacturas = new() { FontSize = 13, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label resumenPrecio = new() { FontSize = 14, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label elegibilidad = new() { FontSize = 13, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label totalVenta = new() { FontSize = 24, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End };
    private readonly Label totalFactura = new() { FontSize = 28, FontAttributes = FontAttributes.Bold };
    private readonly Label resumenFactura = new() { FontSize = 14, LineBreakMode = LineBreakMode.WordWrap };
    private readonly VerticalStackLayout camposCredito = new() { Spacing = 8 };
    private readonly Button quitarFoto;
    private readonly Button desactivar;
    private readonly Button reintentar;
    private readonly Button botonTema;
    private readonly ContentPage editor;
    private readonly ContentPage detalleFactura;
    private ControladorTienda? tienda;
    private HttpClient? http;
    private Celular[] celulares = [];
    private SolicitudFactura? pendiente;
    private int? editarId;
    private string seccionActual = "Catálogo";
    private bool temaClaro = Preferences.Default.Get("celulares.temaClaro", true);
    private byte[]? fotoPendiente;
    private string? nombreFotoPendiente;

    public MainPage()
    {
        FlyoutBehavior = FlyoutBehavior.Flyout;
        FlyoutBackgroundColor = Fondo;
        BackgroundColor = Fondo;
        Title = "Celulares GR09";
        quitarFoto = Boton("Quitar foto", QuitarFoto, secundario: true);
        desactivar = Boton("Desactivar celular", Desactivar, peligro: true);
        reintentar = Boton("Reintentar venta pendiente", Reintentar, secundario: true);
        reintentar.IsVisible = false;
        botonTema = Boton(temaClaro ? "Activar tema oscuro" : "Activar tema claro", CambiarTema, secundario: true);
        FlyoutHeader = CrearCabeceraFlyout();
        FlyoutFooter = CrearPieFlyout();

        var catalogo = CrearPaginaCatalogo();
        var venta = CrearPaginaVenta();
        var facturas = CrearPaginaFacturas();
        editor = CrearPaginaEditor();
        detalleFactura = CrearPaginaDetalle();
        AddDestino("Catálogo", "catalogo", "catalogo.png", catalogo);
        AddDestino("Nueva venta", "venta", "venta.png", venta);
        AddDestino("Facturas", "facturas", "facturas.png", facturas);
        FlyoutContent = CrearMenuFlyout();
        AplicarTema();
        foreach (var entrada in new[] { nombre, precio, cedula, comprador, cuotas, busqueda })
        {
            entrada.SetDynamicResource(Entry.TextColorProperty, "TextoDinamico");
            entrada.SetDynamicResource(Entry.PlaceholderColorProperty, "SecundarioDinamico");
            entrada.SetDynamicResource(VisualElement.BackgroundColorProperty, "CampoDinamico");
        }
        celularVenta.SelectedIndexChanged += (_, _) => ActualizarTotalVenta();
        forma.SelectedIndexChanged += (_, _) => ActualizarTotalVenta();
        camposCredito.Add(Boton("Consultar elegibilidad", ConsultarElegibilidad, secundario: true));
        camposCredito.Add(elegibilidad);
        camposCredito.Add(cuotas);
        Loaded += async (_, _) => { AplicarTema(); await Conectar(); };
    }

    private Color Fondo => temaClaro ? FondoClaro : FondoOscuro;
    private Color Panel => temaClaro ? PanelClaro : PanelOscuro;
    private Color CampoFondo => temaClaro ? CampoClaro : CampoOscuro;
    private Color Texto => temaClaro ? TextoClaro : TextoOscuro;
    private Color Secundario => temaClaro ? SecundarioClaro : SecundarioOscuro;
    private Color Acento => temaClaro ? AcentoClaro : AcentoOscuro;

    private void AddDestino(string titulo, string ruta, string icono, ContentPage pagina)
    {
        var item = new FlyoutItem { Title = titulo, Icon = icono, Route = ruta + "Menu", FlyoutDisplayOptions = FlyoutDisplayOptions.AsSingleItem };
        item.Items.Add(new ShellSection { Route = ruta + "Seccion", Items = { new ShellContent { Route = ruta, Icon = icono, Content = pagina } } });
        Items.Add(item);
    }

    private View CrearCabeceraFlyout() => new Border
    {
        Padding = new Thickness(22, 40, 22, 22), StrokeThickness = 0,
        Content = new VerticalStackLayout { Spacing = 8, Children =
        {
            new Image { Source = "telefono.png", HeightRequest = 76, WidthRequest = 68, HorizontalOptions = LayoutOptions.Start },
            TextoLabel("CELULARES", 21, FontAttributes.Bold, Acento),
            TextoLabel("COMERCIALIZADORA · GR09", 11, FontAttributes.Bold, Secundario)
        }}
    }.ConFondo(Panel);

    private View CrearMenuFlyout() => new VerticalStackLayout
    {
        Padding = new Thickness(14, 16), Spacing = 8,
        Children = { OpcionMenu("Catálogo", "catalogo.png", "//catalogoMenu/catalogoSeccion/catalogo"),
            OpcionMenu("Nueva venta", "venta.png", "//ventaMenu/ventaSeccion/venta"),
            OpcionMenu("Facturas", "facturas.png", "//facturasMenu/facturasSeccion/facturas") }
    };

    private View OpcionMenu(string titulo, string icono, string ruta)
    {
        var fila = new Grid { HeightRequest = 58, Padding = new Thickness(12, 0), ColumnDefinitions = { new ColumnDefinition(new GridLength(46)), new ColumnDefinition(GridLength.Star) } };
        fila.Add(new Image { Source = icono, HeightRequest = 24, WidthRequest = 24, HorizontalOptions = LayoutOptions.Center }, 0);
        fila.Add(TextoLabel(titulo, 16, FontAttributes.Bold, Texto), 1);
        var tarjeta = new Border { Padding = 0, StrokeShape = new RoundRectangle { CornerRadius = 13 }, Content = fila };
        tarjeta.BackgroundColor = titulo == "Catálogo" ? (temaClaro ? Color.FromArgb("#DCEBEF") : CampoOscuro) : Panel;
        tarjeta.Stroke = Colors.Transparent;
        tarjeta.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await NavegarMenu(titulo, ruta)) });
        opcionesMenu[titulo] = tarjeta;
        return tarjeta;
    }

    private async Task NavegarMenu(string titulo, string ruta)
    {
        await GoToAsync(ruta);
        FlyoutIsPresented = false;
        SeleccionarMenu(titulo);
    }

    private void SeleccionarMenu(string titulo)
    {
        seccionActual = titulo;
        foreach (var (nombre, fila) in opcionesMenu) fila.BackgroundColor = nombre == titulo ? (temaClaro ? Color.FromArgb("#DCEBEF") : CampoOscuro) : Panel;
    }

    private View CrearPieFlyout() => new VerticalStackLayout
    {
        Padding = new Thickness(20, 12, 20, 22), Spacing = 10,
        Children = { TextoLabel("Preferencias", 12, FontAttributes.Bold, Secundario), botonTema }
    };

    private ContentPage CrearPaginaCatalogo()
    {
        var pagina = Pagina("Catálogo", avisoCatalogo);
        pagina.ToolbarItems.Add(new ToolbarItem("Actualizar", null, async () => await Ejecutar(CargarCatalogo)));
        pagina.Content = Contenido(
            Encabezado("Encuentra tu próximo celular", "Precios claros, fotos reales y compra en pocos pasos."),
            Fila(TextoLabel("CATÁLOGO", 12, FontAttributes.Bold, Secundario), Boton("＋  Nuevo celular", NuevoCelular, secundario: true)),
            tarjetasCatalogo);
        return pagina;
    }

    private ContentPage CrearPaginaVenta()
    {
        var pagina = Pagina("Nueva venta", avisoVenta);
        resumenPrecio.SetDynamicResource(Label.TextColorProperty, "SecundarioDinamico");
        elegibilidad.SetDynamicResource(Label.TextColorProperty, "AcentoDinamico");
        totalVenta.SetDynamicResource(Label.TextColorProperty, "TextoDinamico");
        celularVenta.SetDynamicResource(Picker.TextColorProperty, "TextoDinamico");
        celularVenta.SetDynamicResource(Picker.TitleColorProperty, "SecundarioDinamico");
        forma.SetDynamicResource(Picker.TextColorProperty, "TextoDinamico");
        forma.SetDynamicResource(Picker.TitleColorProperty, "SecundarioDinamico");
        var contenido = Contenido(
            Encabezado("Nueva venta", "Selecciona producto y pago. Revisa el total antes de confirmar."),
            Seccion("01 · Producto", TextoLabel("Celular", 13, FontAttributes.Bold, Secundario), celularVenta, resumenPrecio),
            Seccion("02 · Comprador", cedula, comprador),
            Seccion("03 · Pago", forma, camposCredito),
            Seccion("Resumen", Fila(TextoLabel("TOTAL", 12, FontAttributes.Bold, Secundario), totalVenta)),
            Boton("Confirmar venta", Vender), reintentar);
        pagina.Content = contenido;
        return pagina;
    }

    private ContentPage CrearPaginaFacturas()
    {
        var pagina = Pagina("Facturas", avisoFacturas);
        pagina.Content = Contenido(
            Encabezado("Facturas", "Consulta compras y planes de pago por cédula."),
            Seccion("Buscar cliente", busqueda, Boton("Buscar facturas", BuscarFacturas)),
            listaFacturas);
        return pagina;
    }

    private ContentPage CrearPaginaEditor()
    {
        var pagina = Pagina("Celular", avisoCatalogo);
        quitarFoto.IsEnabled = false;
        desactivar.IsVisible = false;
        pagina.Content = Contenido(
            Encabezado("Ficha del celular", "Actualiza información y administra su foto."),
            Seccion("Foto", foto, TextoLabel("Usa una imagen JPEG, PNG o WebP · máximo 5 MB.", 12, FontAttributes.None, Secundario),
                Boton("Elegir foto", ElegirFoto, secundario: true), quitarFoto),
            Seccion("Información", TextoLabel("Nombre", 13, FontAttributes.Bold, Secundario), nombre,
                TextoLabel("Precio de lista · USD", 13, FontAttributes.Bold, Secundario), precio),
            Boton("Guardar celular", GuardarCelular), desactivar);
        return pagina;
    }

    private ContentPage CrearPaginaDetalle()
    {
        var pagina = Pagina("Detalle de factura", avisoFacturas);
        totalFactura.SetDynamicResource(Label.TextColorProperty, "AcentoDinamico");
        resumenFactura.SetDynamicResource(Label.TextColorProperty, "TextoDinamico");
        pagina.Content = Contenido(
            Encabezado("Detalle de compra", "Factura y cuotas registradas."),
            Seccion("Comprobante", totalFactura, resumenFactura),
            Encabezado("Plan de pagos", "Saldo inicial y amortización mensual."), amortizacion);
        return pagina;
    }

    private ContentPage Pagina(string titulo, Label aviso)
    {
        aviso.SetDynamicResource(Label.TextColorProperty, "AcentoDinamico");
        var pagina = new ContentPage { Title = titulo, BackgroundColor = Fondo };
        pagina.SetDynamicResource(Page.BackgroundColorProperty, "FondoDinamico");
        return pagina;
    }

    private static View Contenido(params View[] vistas)
    {
        var columna = new VerticalStackLayout { Padding = new Thickness(20, 18, 20, 30), Spacing = 18 };
        foreach (var vista in vistas) columna.Add(vista);
        return new ScrollView { Content = columna };
    }

    private View Encabezado(string titulo, string subtitulo) => new VerticalStackLayout
    {
        Spacing = 6,
        Children = { TextoLabel(titulo, 25, FontAttributes.Bold, Texto), TextoLabel(subtitulo, 14, FontAttributes.None, Secundario) }
    };

    private View Seccion(string titulo, params View[] vistas)
    {
        var contenido = new VerticalStackLayout { Spacing = 10 };
        contenido.Add(TextoLabel(titulo, 16, FontAttributes.Bold, Acento));
        foreach (var vista in vistas) contenido.Add(vista);
        return new Border { Padding = 16, StrokeShape = new RoundRectangle { CornerRadius = 18 }, Content = contenido }.ConFondo(Panel, Borde);
    }

    private View TarjetaCelular(Celular celular)
    {
        var imagen = new Image { HeightRequest = 184, Aspect = Aspect.AspectFit, Source = "telefono.png" };
        if (celular.ImagenUrl is not null)
        {
            var uri = new Uri(ClienteComercializadora.Direccion(MauiProgram.DireccionServidor), celular.ImagenUrl);
            imagen.Source = new UriImageSource { Uri = uri, CachingEnabled = false };
        }
        var precioEfectivo = ControladorTienda.TotalEfectivo(celular.Precio);
        var acciones = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 9 };
        var editar = Boton("Editar", async () => await EditarCelular(celular), secundario: true);
        var vender = Boton("Vender", async () =>
        {
            celularVenta.SelectedItem = celulares.FirstOrDefault(x => x.Id == celular.Id);
            ActualizarTotalVenta();
            await NavegarMenu("Nueva venta", "//ventaMenu/ventaSeccion/venta");
        });
        acciones.Add(editar, 0); acciones.Add(vender, 1);
        return new Border { Padding = 14, StrokeShape = new RoundRectangle { CornerRadius = 20 }, Content = new VerticalStackLayout { Spacing = 10,
            Children = { imagen, TextoLabel(celular.Nombre, 18, FontAttributes.Bold, Texto), TextoLabel($"{celular.Precio:C2}", 24, FontAttributes.Bold, Texto),
                TextoLabel($"En efectivo  {precioEfectivo:C2}  ·  Ahorra 42%", 13, FontAttributes.Bold, Acento), acciones }
        }}.ConFondo(Panel, Borde);
    }

    private async Task Conectar() => await Ejecutar(async () =>
    {
        http?.Dispose();
        http = new HttpClient { BaseAddress = ClienteComercializadora.Direccion(MauiProgram.DireccionServidor), Timeout = TimeSpan.FromSeconds(30) };
        tienda = new ControladorTienda(new ClienteComercializadora(http));
        await CargarCatalogo();
    });

    private async Task CargarCatalogo()
    {
        celulares = await Controlador().Catalogo();
        celularVenta.ItemsSource = celulares;
        tarjetasCatalogo.Clear();
        foreach (var celular in celulares) tarjetasCatalogo.Add(TarjetaCelular(celular));
        avisoCatalogo.Text = celulares.Length == 0 ? "No hay celulares activos. Agrega el primero." : $"{celulares.Length} modelos disponibles · API conectada";
        avisoVenta.Text = "El servidor valida cada venta y vuelve a consultar el crédito.";
        ActualizarTotalVenta();
    }

    private async Task NuevoCelular()
    {
        editarId = null; fotoPendiente = null; nombreFotoPendiente = null;
        nombre.Text = precio.Text = null; foto.Source = "telefono.png";
        quitarFoto.IsEnabled = false; desactivar.IsVisible = false;
        avisoCatalogo.Text = "Nuevo modelo · completa nombre y precio.";
        await Navigation.PushAsync(editor);
    }

    private async Task EditarCelular(Celular celular)
    {
        editarId = celular.Id; nombre.Text = celular.Nombre;
        precio.Text = celular.Precio.ToString("F2", CultureInfo.CurrentCulture);
        fotoPendiente = null; nombreFotoPendiente = null;
        quitarFoto.IsEnabled = celular.ImagenUrl is not null; desactivar.IsVisible = true;
        foto.Source = "telefono.png";
        if (celular.ImagenUrl is not null)
        {
            try { var bytes = await Controlador().DescargarImagen(celular); foto.Source = ImageSource.FromStream(() => new MemoryStream(bytes)); }
            catch (Exception e) { avisoCatalogo.Text = "No se pudo cargar la foto: " + e.Message; }
        }
        avisoCatalogo.Text = $"Editando {celular.Nombre}";
        await Navigation.PushAsync(editor);
    }

    private async Task GuardarCelular() => await Ejecutar(async () =>
    {
        var nombreLimpio = nombre.Text?.Trim() ?? "";
        if (nombreLimpio.Length is < 2 or > 100) throw new ArgumentException("El nombre debe tener entre 2 y 100 caracteres.");
        var resultado = await Controlador().Guardar(editarId, nombreLimpio, ControladorTienda.InterpretarPrecio(precio.Text));
        editarId = resultado.Id;
        if (fotoPendiente is not null)
        {
            using var stream = new MemoryStream(fotoPendiente);
            await Controlador().SubirImagen(resultado.Id, stream, nombreFotoPendiente ?? "foto.jpg", TipoImagen(nombreFotoPendiente ?? "foto.jpg"));
            fotoPendiente = null; nombreFotoPendiente = null;
        }
        await CargarCatalogo();
        avisoCatalogo.Text = $"Guardado · {resultado.Nombre} · {resultado.Precio:C2}";
        await Navigation.PopAsync();
    });

    private async Task ElegirFoto()
    {
        var archivo = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Elige una foto de celular", FileTypes = FilePickerFileType.Images });
        if (archivo is null) return;
        await Ejecutar(async () =>
        {
            _ = TipoImagen(archivo.FileName);
            var bytes = await LeerImagen(archivo);
            foto.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            if (editarId is int id)
            {
                using var stream = new MemoryStream(bytes);
                await Controlador().SubirImagen(id, stream, archivo.FileName, TipoImagen(archivo.FileName));
                quitarFoto.IsEnabled = true;
                await CargarCatalogo();
                avisoCatalogo.Text = "Foto guardada en SQL Server.";
            }
            else { fotoPendiente = bytes; nombreFotoPendiente = archivo.FileName; quitarFoto.IsEnabled = true; avisoCatalogo.Text = "Vista previa · guarda el celular para subir la foto."; }
        });
    }

    private async Task QuitarFoto()
    {
        if (fotoPendiente is not null)
        {
            fotoPendiente = null; nombreFotoPendiente = null; foto.Source = "telefono.png"; quitarFoto.IsEnabled = false;
            avisoCatalogo.Text = "Foto pendiente descartada."; return;
        }
        if (editarId is not int id || !await DisplayAlertAsync("Quitar foto", "¿Eliminar la foto de este celular?", "Quitar", "Cancelar")) return;
        await Ejecutar(async () => { await Controlador().QuitarImagen(id); quitarFoto.IsEnabled = false; foto.Source = "telefono.png"; await CargarCatalogo(); avisoCatalogo.Text = "Foto eliminada."; });
    }

    private async Task Desactivar()
    {
        if (editarId is not int id || !await DisplayAlertAsync("Desactivar celular", "El celular dejará de aparecer en el catálogo. Las facturas se conservan. ¿Continuar?", "Desactivar", "Cancelar")) return;
        await Ejecutar(async () => { await Controlador().Eliminar(id); await CargarCatalogo(); await Navigation.PopAsync(); });
    }

    private void ActualizarTotalVenta()
    {
        var credito = forma.SelectedIndex == 1;
        camposCredito.IsVisible = credito;
        var celular = celularVenta.SelectedItem as Celular;
        resumenPrecio.Text = celular is null ? "Elige un modelo del catálogo." : $"Precio de lista {celular.Precio:C2}";
        totalVenta.Text = celular is null ? "—" : (credito ? celular.Precio : ControladorTienda.TotalEfectivo(celular.Precio)).ToString("C2");
    }

    private async Task ConsultarElegibilidad() => await Ejecutar(async () =>
    {
        var resultado = await Controlador().EvaluarCredito(cedula.Text?.Trim() ?? "");
        elegibilidad.Text = resultado.SujetoCredito ? $"Elegible · máximo {resultado.MontoMaximo:C2}" : $"No elegible · {resultado.Motivo}";
    });

    private async Task Vender() => await Ejecutar(async () =>
    {
        if (celularVenta.SelectedItem is not Celular celular) throw new ArgumentException("Elige un celular.");
        var numeroCedula = cedula.Text?.Trim() ?? "";
        var cliente = comprador.Text?.Trim() ?? "";
        if (numeroCedula.Length is < 10 or > 13 || numeroCedula.Any(c => !char.IsDigit(c))) throw new ArgumentException("Ingresa una cédula válida de 10 a 13 dígitos.");
        if (cliente.Length is < 2 or > 100) throw new ArgumentException("Ingresa nombre completo del comprador.");
        var formaPago = forma.SelectedIndex == 1 ? "CREDITO_DIRECTO" : "EFECTIVO";
        var solicitud = new SolicitudFactura { Cedula = numeroCedula, NombreCliente = cliente, IdCelular = celular.Id, FormaPago = formaPago,
            NumeroCuotas = formaPago == "CREDITO_DIRECTO" ? Entero(cuotas.Text) : null, PrecioEsperado = celular.Precio };
        if (pendiente is not null && !MismaVenta(pendiente, solicitud)) throw new InvalidOperationException("Hay una venta pendiente. Reintenta esa operación antes de cambiar los datos.");
        if (formaPago == "CREDITO_DIRECTO" && pendiente is null)
        {
            var evaluacion = await Controlador().EvaluarCredito(numeroCedula);
            elegibilidad.Text = evaluacion.SujetoCredito ? $"Elegible · máximo {evaluacion.MontoMaximo:C2}" : $"No elegible · {evaluacion.Motivo}";
            if (!evaluacion.SujetoCredito || celular.Precio > evaluacion.MontoMaximo) throw new InvalidOperationException("El cliente no califica para este monto en BanQuito.");
        }
        var total = formaPago == "EFECTIVO" ? ControladorTienda.TotalEfectivo(celular.Precio) : celular.Precio;
        if (!await DisplayAlertAsync("Confirmar venta", $"{celular.Nombre}\n{cliente}\n{formaPago}\nTotal {total:C2}", "Confirmar", "Revisar")) return;
        pendiente ??= solicitud;
        reintentar.IsVisible = true;
        var factura = await Controlador().Facturar(pendiente);
        pendiente = null; reintentar.IsVisible = false;
        await MostrarFactura(factura);
        await GoToAsync("//facturasMenu/facturasSeccion/facturas");
        SeleccionarMenu("Facturas");
        await Navigation.PushAsync(detalleFactura);
    });

    private async Task Reintentar()
    {
        if (pendiente is null) return;
        await Ejecutar(async () => { var factura = await Controlador().Facturar(pendiente); pendiente = null; reintentar.IsVisible = false; await MostrarFactura(factura); await GoToAsync("//facturasMenu/facturasSeccion/facturas"); SeleccionarMenu("Facturas"); await Navigation.PushAsync(detalleFactura); });
    }

    private async Task BuscarFacturas() => await Ejecutar(async () =>
    {
        var numero = busqueda.Text?.Trim() ?? "";
        if (numero.Length is < 10 or > 13 || numero.Any(c => !char.IsDigit(c))) throw new ArgumentException("Ingresa la cédula de 10 a 13 dígitos.");
        var facturas = await Controlador().FacturasPorCedula(numero);
        listaFacturas.Clear();
        foreach (var factura in facturas) listaFacturas.Add(TarjetaFactura(factura));
        avisoFacturas.Text = facturas.Length == 0 ? "No encontramos facturas para esa cédula." : $"{facturas.Length} facturas · selecciona Ver detalle.";
    });

    private View TarjetaFactura(Factura factura) => new Border
    {
        Padding = 15, StrokeShape = new RoundRectangle { CornerRadius = 16 },
        Content = new VerticalStackLayout { Spacing = 7, Children =
        {
            TextoLabel($"FACTURA {factura.Id}  ·  {factura.Estado}", 12, FontAttributes.Bold, Acento),
            TextoLabel(factura.Celular, 17, FontAttributes.Bold, Texto),
            TextoLabel($"{factura.Fecha:dd MMM yyyy}  ·  {factura.FormaPago}", 13, FontAttributes.None, Secundario),
            Fila(TextoLabel(factura.NombreCliente, 14, FontAttributes.None, Texto), TextoLabel(factura.Total.ToString("C2"), 18, FontAttributes.Bold, Texto)),
            Boton("Ver detalle", async () => { await MostrarFactura(factura); await Navigation.PushAsync(detalleFactura); }, secundario: true)
        }}
    }.ConFondo(Panel, Borde);

    private async Task MostrarFactura(Factura factura)
    {
        totalFactura.Text = factura.Total.ToString("C2");
        resumenFactura.Text = $"Factura {factura.Id} · {factura.Estado}\n{factura.NombreCliente} · {factura.Cedula}\n{factura.Celular} · {factura.FormaPago}\nPrecio {factura.Precio:C2} · Descuento {factura.Descuento:C2}";
        amortizacion.Clear();
        if (factura.FormaPago != "CREDITO_DIRECTO" || factura.Estado != "APROBADA") return;
        try
        {
            foreach (var cuota in await Controlador().Amortizacion(factura.Id))
            {
                if (cuota.NumeroCuota == 0)
                {
                    amortizacion.Add(Seccion("Saldo inicial", TextoLabel($"Capital financiado  {cuota.Saldo:C2}", 15, FontAttributes.Bold, Texto)));
                    continue;
                }
                var reparto = $"Interés {cuota.InteresPagado:C2} · Capital {cuota.CapitalPagado:C2}";
                amortizacion.Add(Seccion($"Cuota {cuota.NumeroCuota} · {cuota.FechaPago:dd MMM yyyy}",
                    Fila(TextoLabel(reparto, 13, FontAttributes.None, Secundario), TextoLabel(cuota.ValorCuota.ToString("C2"), 16, FontAttributes.Bold, Texto)),
                    TextoLabel($"Saldo restante  {cuota.Saldo:C2}", 13, FontAttributes.Bold, Acento)));
            }
        }
        catch (Exception e) { avisoFacturas.Text = "Factura cargada; amortización no disponible: " + e.Message; }
    }

    private async Task Ejecutar(Func<Task> accion)
    {
        try { await accion(); }
        catch (Exception e)
        {
            var mensaje = e.Message;
            var pagina = CurrentPage;
            if (pagina is ContentPage cp && cp.Content is ScrollView scroll && scroll.Content is VerticalStackLayout col && col.Children.FirstOrDefault() is Label status)
                status.Text = mensaje;
            await (pagina ?? this).DisplayAlertAsync("Revisa la solicitud", mensaje, "Entendido");
        }
    }

    private async Task CambiarTema()
    {
        temaClaro = !temaClaro; Preferences.Default.Set("celulares.temaClaro", temaClaro);
        AplicarTema();
        botonTema.Text = temaClaro ? "Activar tema oscuro" : "Activar tema claro";
        await Task.CompletedTask;
    }

    private void AplicarTema()
    {
        var recursos = Application.Current!.Resources;
        recursos["FondoDinamico"] = Fondo;
        recursos["PanelDinamico"] = Panel;
        recursos["CampoDinamico"] = CampoFondo;
        recursos["TextoDinamico"] = Texto;
        recursos["SecundarioDinamico"] = Secundario;
        recursos["AcentoDinamico"] = Acento;
        recursos["BordeDinamico"] = Borde;
        recursos["PeligroDinamico"] = Color.FromArgb("#B4233B");
        recursos["TextoBotonDinamico"] = temaClaro ? Colors.White : Color.FromArgb("#09111F");
        BackgroundColor = Fondo;
        FlyoutBackgroundColor = Panel;
        Shell.SetBackgroundColor(this, Fondo);
        Shell.SetForegroundColor(this, Texto);
        Shell.SetTitleColor(this, Texto);
        Shell.SetUnselectedColor(this, Secundario);
        if (FlyoutContent is VisualElement menu) menu.BackgroundColor = Panel;
        foreach (var pagina in Items.OfType<ShellItem>().SelectMany(item => item.Items).SelectMany(section => section.Items)
                     .Select(content => content.Content).OfType<ContentPage>().Append(editor).Append(detalleFactura))
            pagina.BackgroundColor = Fondo;
        SeleccionarMenu(seccionActual);
        if (FlyoutHeader is Border header) header.SetDynamicResource(VisualElement.BackgroundColorProperty, "PanelDinamico");
#if ANDROID
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window is { } window && AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView) is { } bars)
        {
            var barraClaraSoportada = temaClaro && Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.VanillaIceCream;
            bars.AppearanceLightStatusBars = barraClaraSoportada;
            bars.AppearanceLightNavigationBars = temaClaro;
            var barraSistema = Android.Graphics.Color.ParseColor(barraClaraSoportada ? "#F3F6FA" : "#080D18");
            var barraNavegacion = Android.Graphics.Color.ParseColor(temaClaro ? "#F3F6FA" : "#080D18");
            // Keep legacy system-bar colors on Android 14 while retaining API 23 support.
#pragma warning disable CA1422
            window.SetStatusBarColor(barraSistema);
            window.SetNavigationBarColor(barraNavegacion);
#pragma warning restore CA1422
        }
#endif
    }

    private static async Task<byte[]> LeerImagen(FileResult archivo)
    {
        await using var origen = await archivo.OpenReadAsync(); using var destino = new MemoryStream(); var buffer = new byte[81920]; int leidos;
        while ((leidos = await origen.ReadAsync(buffer)) > 0) { if (destino.Length + leidos > 5 * 1024 * 1024) throw new ArgumentException("La foto debe pesar hasta 5 MB."); await destino.WriteAsync(buffer.AsMemory(0, leidos)); }
        if (destino.Length == 0) throw new ArgumentException("El archivo seleccionado está vacío.");
        return destino.ToArray();
    }

    private static string TipoImagen(string ruta) => System.IO.Path.GetExtension(ruta).ToLowerInvariant() switch
    { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => throw new ArgumentException("Usa una foto JPEG, PNG o WebP.") };
    private static int Entero(string? texto) => int.TryParse(texto, out var n) && n is >= 3 and <= 18 ? n : throw new ArgumentException("Ingresa entre 3 y 18 cuotas.");
    private static bool MismaVenta(SolicitudFactura a, SolicitudFactura b) => a.Cedula == b.Cedula && a.NombreCliente == b.NombreCliente && a.IdCelular == b.IdCelular && a.FormaPago == b.FormaPago && a.NumeroCuotas == b.NumeroCuotas && a.PrecioEsperado == b.PrecioEsperado;
    private ControladorTienda Controlador() => tienda ?? throw new InvalidOperationException("La comercializadora todavía no se conecta.");
    private Label TextoLabel(string texto, double tamano, FontAttributes estilo, Color color)
    {
        var label = new Label { Text = texto, FontSize = tamano, FontAttributes = estilo, LineBreakMode = LineBreakMode.WordWrap, VerticalOptions = LayoutOptions.Center };
        label.SetDynamicResource(Label.TextColorProperty, color == Acento ? "AcentoDinamico" : color == Secundario ? "SecundarioDinamico" : "TextoDinamico");
        return label;
    }
    private static Entry Campo(string placeholder, Keyboard? teclado = null) => new() { Placeholder = placeholder, Keyboard = teclado ?? Keyboard.Default, ClearButtonVisibility = ClearButtonVisibility.WhileEditing, MinimumHeightRequest = 48 };
    private View Fila(View izquierda, View derecha)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        grid.Add(izquierda, 0); grid.Add(derecha, 1); return grid;
    }
    private Button Boton(string texto, Func<Task> accion, bool secundario = false, bool peligro = false)
    {
        var boton = new Button { Text = texto, CornerRadius = 12, FontSize = 14, FontAttributes = FontAttributes.Bold, MinimumHeightRequest = 46, Padding = new Thickness(14, 9) };
        boton.SetDynamicResource(VisualElement.BackgroundColorProperty, peligro ? "PeligroDinamico" : secundario ? "CampoDinamico" : "AcentoDinamico");
        boton.SetDynamicResource(Button.TextColorProperty, secundario ? "TextoDinamico" : "TextoBotonDinamico");
        boton.Clicked += async (_, _) => { boton.IsEnabled = false; try { await accion(); } finally { boton.IsEnabled = true; } };
        return boton;
    }
    private Color Borde => temaClaro ? Color.FromArgb("#D7E2EA") : Color.FromArgb("#34445D");
}

internal static class EstiloVista
{
    internal static Border ConFondo(this Border borde, Color fondo, Color? linea = null)
    {
        borde.SetDynamicResource(VisualElement.BackgroundColorProperty, "PanelDinamico");
        borde.SetDynamicResource(Border.StrokeProperty, "BordeDinamico");
        return borde;
    }
}
