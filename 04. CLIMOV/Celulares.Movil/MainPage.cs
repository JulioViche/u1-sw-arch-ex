using System.Globalization;
using System.IO;
using Celulares.Cliente.Controladores;
using Celulares.Cliente.Servicios;
using Celulares.Contratos;
using Microsoft.Maui.Controls.Shapes;

namespace Celulares.Movil;

public sealed class MainPage : Shell
{
    // Paleta de colores sincronizada exactamente con Web y Desktop (WPF)
    private static readonly Color FondoClaro = Color.FromArgb("#F3F6FA");
    private static readonly Color PanelClaro = Colors.White;
    private static readonly Color CampoClaro = Color.FromArgb("#EAF1F6");
    private static readonly Color TextoClaro = Color.FromArgb("#1B293A");
    private static readonly Color SecundarioClaro = Color.FromArgb("#52657A");
    private static readonly Color AcentoClaro = Color.FromArgb("#087F78");
    private static readonly Color BordeClaro = Color.FromArgb("#D7E2EA");

    private static readonly Color FondoOscuro = Color.FromArgb("#080D18");
    private static readonly Color PanelOscuro = Color.FromArgb("#121C30");
    private static readonly Color CampoOscuro = Color.FromArgb("#1C2940");
    private static readonly Color TextoOscuro = Color.FromArgb("#F5F7FB");
    private static readonly Color SecundarioOscuro = Color.FromArgb("#BAC8DB");
    private static readonly Color AcentoOscuro = Color.FromArgb("#52E0D2");
    private static readonly Color BordeOscuro = Color.FromArgb("#34445D");

    private static readonly Color PeligroColor = Color.FromArgb("#FF6B8B");
    private static readonly Color ExitoColor = Color.FromArgb("#2ECC71");

    // Contenedores dinámicos
    private readonly VerticalStackLayout tarjetasCatalogo = new() { Spacing = 14 };
    private readonly VerticalStackLayout listaFacturas = new() { Spacing = 12 };
    private readonly VerticalStackLayout amortizacion = new() { Spacing = 10 };
    private readonly VerticalStackLayout contenedorVoucher = new() { Spacing = 12 };
    private readonly Border vacioCatalogoAviso;
    private readonly Dictionary<string, Border> opcionesMenu = new();

    // Campos de entrada
    private readonly Entry nombre = Campo("Nombre del modelo");
    private readonly Entry precio = Campo("Precio de lista · USD", Keyboard.Numeric);
    private readonly Label precioEfectivoPreview = new() { FontSize = 13, FontAttributes = FontAttributes.Bold };
    private readonly Entry cedula = Campo("Cédula del comprador (10 a 13 dígitos)", Keyboard.Numeric);
    private readonly Entry comprador = Campo("Nombre completo del comprador");
    private readonly Entry cuotas = Campo("Cuotas para crédito (de 3 a 18)", Keyboard.Numeric);
    private readonly Entry busqueda = Campo("Cédula del cliente a consultar", Keyboard.Numeric);
    private readonly VerticalStackLayout celularVenta = new() { Spacing = 4 };
    private readonly Dictionary<int, CheckBox> seleccionVenta = new();
    private readonly Picker forma = new() { Title = "Forma de pago", ItemsSource = new[] { "Efectivo (42% de descuento)", "Crédito directo BanQuito" }, SelectedIndex = 0 };
    private readonly Image foto = new() { HeightRequest = 180, Aspect = Aspect.AspectFit, Source = "telefono.png" };

    // Etiquetas de estado y totales
    private readonly Label avisoCatalogo = new() { FontSize = 13, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label avisoVenta = new() { FontSize = 13, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label avisoFacturas = new() { FontSize = 13, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label resumenPrecio = new() { FontSize = 14, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label elegibilidad = new() { FontSize = 13, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label totalVenta = new() { FontSize = 26, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End };
    private readonly VerticalStackLayout camposCredito = new() { Spacing = 10 };

    // Botones de acción
    private readonly Button quitarFoto;
    private readonly Button desactivar;
    private readonly Button reintentar;
    private readonly Button descartarVenta;
    private readonly Button botonTema;
    private readonly Button botonActualizarCatalogo;
    private readonly Button botonServidor;

    // Páginas secundarias de navegación
    private readonly ContentPage editor;
    private readonly ContentPage detalleFactura;

    // Estado interno
    private ControladorTienda? tienda;
    private HttpClient? http;
    private Celular[] celulares = [];
    private SolicitudFactura? pendiente;
    private int? editarId;
    private string seccionActual = "Catálogo";
    private bool temaClaro = Preferences.Default.Get("celulares.temaClaro", false); // Predeterminado oscuro como Web y Desktop
    private byte[]? fotoPendiente;
    private string? nombreFotoPendiente;

    public MainPage()
    {
        FlyoutBehavior = FlyoutBehavior.Flyout;
        FlyoutBackgroundColor = Fondo;
        BackgroundColor = Fondo;
        Title = "Celulares GR09";

        quitarFoto = Boton("🗑️  Quitar foto", QuitarFoto, secundario: true);
        desactivar = Boton("🗑️  Desactivar celular", Desactivar, peligro: true);
        reintentar = Boton("🔄  Reintentar venta pendiente", Reintentar, secundario: true);
        descartarVenta = Boton("✖  Descartar venta pendiente", DescartarVenta, secundario: true);
        reintentar.IsVisible = false;
        descartarVenta.IsVisible = false;

        botonTema = Boton(temaClaro ? "🌙  Activar tema oscuro" : "☀️  Activar tema claro", CambiarTema, secundario: true);
        botonActualizarCatalogo = Boton("🔄  Actualizar catálogo", async () => await Ejecutar(CargarCatalogo), secundario: true);
        botonServidor = Boton($"⚙️  Servidor: {ServidorCorto}", CambiarServidor, secundario: true);

        vacioCatalogoAviso = new Border
        {
            Padding = 30, StrokeShape = new RoundRectangle { CornerRadius = 18 }, IsVisible = false,
            Content = new VerticalStackLayout
            {
                Spacing = 8, HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = "📱", FontSize = 44, HorizontalTextAlignment = TextAlignment.Center },
                    TextoLabel("Catálogo vacío", 18, FontAttributes.Bold, Texto),
                    TextoLabel("No hay celulares activos actualmente. Registra uno nuevo para comenzar.", 13, FontAttributes.None, Secundario),
                    Boton("＋  Registrar nuevo celular", NuevoCelular)
                }
            }
        }.ConFondo(Panel, Borde);

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

        precio.TextChanged += (_, _) => ActualizarPreviewPrecioEditor();
        forma.SelectedIndexChanged += (_, _) => ActualizarTotalVenta();

        elegibilidad.Text = "Ingrese la cédula y consulte si es sujeto a crédito en BanQuito.";
        camposCredito.Add(TextoLabel("Número de cuotas mensuales (3 a 18):", 13, FontAttributes.Bold, Secundario));
        camposCredito.Add(cuotas);

        Loaded += async (_, _) => { AplicarTema(); await Conectar(); };
    }

    private Color Fondo => temaClaro ? FondoClaro : FondoOscuro;
    private Color Panel => temaClaro ? PanelClaro : PanelOscuro;
    private Color CampoFondo => temaClaro ? CampoClaro : CampoOscuro;
    private Color Texto => temaClaro ? TextoClaro : TextoOscuro;
    private Color Secundario => temaClaro ? SecundarioClaro : SecundarioOscuro;
    private Color Acento => temaClaro ? AcentoClaro : AcentoOscuro;
    private Color Borde => temaClaro ? BordeClaro : BordeOscuro;

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
            new Image { Source = "telefono.png", HeightRequest = 64, WidthRequest = 64, HorizontalOptions = LayoutOptions.Start },
            TextoLabel("CELULARES GR09", 22, FontAttributes.Bold, Acento),
            TextoLabel("Comercializadora oficial & BanQuito", 12, FontAttributes.None, Secundario)
        }}
    }.ConFondo(Panel);

    private View CrearMenuFlyout() => new VerticalStackLayout
    {
        Padding = new Thickness(14, 16), Spacing = 8,
        Children =
        {
            OpcionMenu("Catálogo", "catalogo.png", "//catalogoMenu/catalogoSeccion/catalogo"),
            OpcionMenu("Nueva venta", "venta.png", "//ventaMenu/ventaSeccion/venta"),
            OpcionMenu("Facturas", "facturas.png", "//facturasMenu/facturasSeccion/facturas")
        }
    };

    private View OpcionMenu(string titulo, string icono, string ruta)
    {
        var fila = new Grid { HeightRequest = 56, Padding = new Thickness(14, 0), ColumnDefinitions = { new ColumnDefinition(new GridLength(42)), new ColumnDefinition(GridLength.Star) } };
        fila.Add(new Image { Source = icono, HeightRequest = 22, WidthRequest = 22, HorizontalOptions = LayoutOptions.Center }, 0);
        fila.Add(TextoLabel(titulo, 15, FontAttributes.Bold, Texto), 1);
        var tarjeta = new Border { Padding = 0, StrokeShape = new RoundRectangle { CornerRadius = 14 }, Content = fila };
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
        foreach (var (nombreMenu, fila) in opcionesMenu)
            fila.BackgroundColor = nombreMenu == titulo ? (temaClaro ? Color.FromArgb("#DCEBEF") : CampoOscuro) : Panel;
    }

    private View CrearPieFlyout() => new VerticalStackLayout
    {
        Padding = new Thickness(20, 12, 20, 24), Spacing = 10,
        Children =
        {
            TextoLabel("PREFERENCIAS & RED", 11, FontAttributes.Bold, Secundario),
            botonTema,
            botonServidor
        }
    };

    // =========================================================================
    // VISTA 1: CATÁLOGO DE CELULARES (IGUAL A INDEX.CSHTML Y MAINWINDOW)
    // =========================================================================
    private ContentPage CrearPaginaCatalogo()
    {
        var pagina = Pagina("Catálogo", avisoCatalogo);
        pagina.ToolbarItems.Add(new ToolbarItem("Actualizar", null, async () => await Ejecutar(CargarCatalogo)));

        var filaAccionesHero = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 8
        };
        filaAccionesHero.Add(botonActualizarCatalogo, 0);
        var botonConfigIp = Boton("⚙️ IP", CambiarServidor, secundario: true);
        botonConfigIp.Padding = new Thickness(14, 9);
        filaAccionesHero.Add(botonConfigIp, 1);

        var heroBanner = new Border
        {
            Padding = 18, StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    TextoLabel("COMERCIALIZADORA DE CELULARES · GR09", 11, FontAttributes.Bold, Acento),
                    TextoLabel("Catálogo oficial y financiamiento BanQuito", 21, FontAttributes.Bold, Texto),
                    TextoLabel($"Conectado a API: {ServidorCorto}", 12, FontAttributes.None, Secundario),
                    filaAccionesHero
                }
            }
        }.ConFondo(Panel, Borde);

        var cabeceraSeccion = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        cabeceraSeccion.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                TextoLabel("EQUIPOS DISPONIBLES", 11, FontAttributes.Bold, Acento),
                TextoLabel("Modelos en catálogo", 19, FontAttributes.Bold, Texto)
            }
        }, 0);
        cabeceraSeccion.Add(Boton("＋  Nuevo celular", NuevoCelular), 1);

        pagina.Content = Contenido(heroBanner, cabeceraSeccion, vacioCatalogoAviso, tarjetasCatalogo);
        return pagina;
    }

    private View TarjetaCelular(Celular celular)
    {
        var imagen = new Image { HeightRequest = 180, Aspect = Aspect.AspectFit, Source = "telefono.png" };
        if (celular.ImagenUrl is not null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var bytes = await Controlador().DescargarImagen(celular);
                    if (bytes.Length > 0)
                        Dispatcher.Dispatch(() => imagen.Source = ImageSource.FromStream(() => new MemoryStream(bytes)));
                }
                catch { /* omitir si la imagen no responde */ }
            });
        }

        var precioEfectivo = ControladorTienda.TotalEfectivo(celular.Precio);
        var acciones = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        var editar = Boton("✏️  Editar datos", async () => await EditarCelular(celular), secundario: true);
        var vender = Boton("🛒  Vender", async () =>
        {
            if (seleccionVenta.TryGetValue(celular.Id, out var casilla)) casilla.IsChecked = true;
            ActualizarTotalVenta();
            await NavegarMenu("Nueva venta", "//ventaMenu/ventaSeccion/venta");
        });
        acciones.Add(editar, 0);
        acciones.Add(vender, 1);

        var bloquePrecios = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                TextoLabel($"Precio de lista: {celular.Precio:C2}", 14, FontAttributes.None, Secundario),
                TextoLabel($"En efectivo (42% desc): {precioEfectivo:C2}", 18, FontAttributes.Bold, Acento)
            }
        };

        return new Border
        {
            Padding = 16, StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    TextoLabel($"CELULAR · #{celular.Id:D3}", 11, FontAttributes.Bold, Acento),
                    imagen,
                    TextoLabel(celular.Nombre, 19, FontAttributes.Bold, Texto),
                    bloquePrecios,
                    acciones
                }
            }
        }.ConFondo(Panel, Borde);
    }

    // =========================================================================
    // VISTA 2: NUEVA VENTA (IGUAL A VENTAS.CSHTML Y MAINWINDOW)
    // =========================================================================
    private ContentPage CrearPaginaVenta()
    {
        var pagina = Pagina("Nueva venta", avisoVenta);
        resumenPrecio.SetDynamicResource(Label.TextColorProperty, "SecundarioDinamico");
        elegibilidad.SetDynamicResource(Label.TextColorProperty, "AcentoDinamico");
        totalVenta.SetDynamicResource(Label.TextColorProperty, "AcentoDinamico");
        forma.SetDynamicResource(Picker.TextColorProperty, "TextoDinamico");
        forma.SetDynamicResource(Picker.TitleColorProperty, "SecundarioDinamico");

        var heroVenta = new Border
        {
            Padding = 18, StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    TextoLabel("COMERCIALIZADORA GR09 · FACTURACIÓN", 11, FontAttributes.Bold, Acento),
                    TextoLabel("Gestión de Ventas y Facturación", 22, FontAttributes.Bold, Texto),
                    TextoLabel("Efectivo: 42% de descuento. Crédito directo BanQuito: 16.5% anual de 3 a 18 cuotas.", 13, FontAttributes.None, Secundario)
                }
            }
        }.ConFondo(Panel, Borde);

        var filaCedula = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 8
        };
        filaCedula.Add(cedula, 0);
        var btnConsultarBanQuito = Boton("🔍  Consultar", ConsultarElegibilidad, secundario: true);
        btnConsultarBanQuito.Padding = new Thickness(14, 8);
        filaCedula.Add(btnConsultarBanQuito, 1);

        var cajaElegibilidad = new Border
        {
            Padding = 12,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = elegibilidad
        }.ConFondo(Panel, Borde);

        var seccionComprador = Seccion("01 · Comprador & Elegibilidad BanQuito",
            TextoLabel("Cédula de identidad del cliente:", 13, FontAttributes.Bold, Secundario),
            filaCedula,
            cajaElegibilidad,
            TextoLabel("Nombre completo del comprador:", 13, FontAttributes.Bold, Secundario),
            comprador);

        var seccionProducto = Seccion("02 · Selección de Celular",
            TextoLabel("Equipo a comprar:", 13, FontAttributes.Bold, Secundario),
            celularVenta,
            resumenPrecio);

        var seccionPago = Seccion("03 · Forma de Pago y Financiamiento",
            TextoLabel("Forma de pago:", 13, FontAttributes.Bold, Secundario),
            forma,
            camposCredito);

        var seccionResumen = Seccion("Resumen de la Venta",
            Fila(TextoLabel("TOTAL FACTURADO:", 14, FontAttributes.Bold, Secundario), totalVenta));

        pagina.Content = Contenido(heroVenta, seccionComprador, seccionProducto, seccionPago, seccionResumen,
            Boton("✓  Confirmar y emitir factura", Vender), reintentar, descartarVenta);
        return pagina;
    }

    // =========================================================================
    // VISTA 3: FACTURAS & AMORTIZACIÓN (IGUAL A FACTURA.CSHTML Y MAINWINDOW)
    // =========================================================================
    private ContentPage CrearPaginaFacturas()
    {
        var pagina = Pagina("Facturas", avisoFacturas);
        var heroFacturas = new Border
        {
            Padding = 18, StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    TextoLabel("CONSULTAS", 11, FontAttributes.Bold, Acento),
                    TextoLabel("Historial de compras por cédula", 22, FontAttributes.Bold, Texto),
                    TextoLabel("Ingresa la cédula del comprador para consultar todas sus facturas históricas y amortizaciones BanQuito.", 13, FontAttributes.None, Secundario)
                }
            }
        }.ConFondo(Panel, Borde);

        var buscador = Seccion("Búsqueda de Facturas",
            busqueda,
            Boton("🔎  Buscar facturas", BuscarFacturas));

        pagina.Content = Contenido(heroFacturas, buscador, listaFacturas);
        return pagina;
    }

    private View TarjetaFactura(Factura factura)
    {
        var esEfectivo = factura.FormaPago == "EFECTIVO";
        var esAprobada = factura.Estado == "APROBADA";
        var estadoColor = esAprobada ? ExitoColor : PeligroColor;

        var filaTitulo = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        filaTitulo.Add(TextoLabel($"FACTURA #{factura.Id}", 13, FontAttributes.Bold, Acento), 0);
        filaTitulo.Add(TextoLabel(factura.Estado, 12, FontAttributes.Bold, estadoColor), 1);

        return new Border
        {
            Padding = 16, StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    filaTitulo,
                    TextoLabel(factura.Celular, 18, FontAttributes.Bold, Texto),
                    TextoLabel($"{factura.Fecha:yyyy-MM-dd}  ·  {(esEfectivo ? "Efectivo (42% desc)" : "Crédito directo BanQuito")}", 13, FontAttributes.None, Secundario),
                    Fila(
                        TextoLabel(factura.NombreCliente, 14, FontAttributes.None, Texto),
                        TextoLabel(factura.Total.ToString("C2"), 18, FontAttributes.Bold, Acento)
                    ),
                    Boton("Ver detalle del comprobante  ➡️", async () => { await MostrarFactura(factura); await Navigation.PushAsync(detalleFactura); }, secundario: true)
                }
            }
        }.ConFondo(Panel, Borde);
    }

    private ContentPage CrearPaginaDetalle()
    {
        var pagina = Pagina("Comprobante de Factura", avisoFacturas);
        var volverBtn = Boton("⬅️  Volver a facturas", async () => await Navigation.PopAsync(), secundario: true);

        var voucherCard = new Border
        {
            Padding = 20, StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = contenedorVoucher
        }.ConFondo(Panel, Borde);

        var planAmortizacion = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                TextoLabel("TABLA DE AMORTIZACIÓN BANQUITO", 12, FontAttributes.Bold, Acento),
                TextoLabel("Plan de pagos con cuota fija francesa · 16.5% anual", 13, FontAttributes.None, Secundario),
                amortizacion
            }
        };

        pagina.Content = Contenido(volverBtn, voucherCard, planAmortizacion);
        return pagina;
    }

    private async Task MostrarFactura(Factura factura)
    {
        contenedorVoucher.Clear();
        amortizacion.Clear();

        var esEfectivo = factura.FormaPago == "EFECTIVO";
        var esAprobada = factura.Estado == "APROBADA";
        var estadoColor = esAprobada ? ExitoColor : PeligroColor;

        var filaHeader = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        filaHeader.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                TextoLabel($"COMPROBANTE · {factura.Fecha:yyyy-MM-dd}", 11, FontAttributes.Bold, Acento),
                TextoLabel($"Factura #{factura.Id}", 24, FontAttributes.Bold, Texto)
            }
        }, 0);
        filaHeader.Add(TextoLabel(factura.Estado, 13, FontAttributes.Bold, estadoColor), 1);

        contenedorVoucher.Add(filaHeader);
        contenedorVoucher.Add(Separador());
        contenedorVoucher.Add(Fila(TextoLabel("Cliente:", 13, FontAttributes.Bold, Secundario), TextoLabel(factura.NombreCliente, 14, FontAttributes.Bold, Texto)));
        contenedorVoucher.Add(Fila(TextoLabel("Cédula:", 13, FontAttributes.Bold, Secundario), TextoLabel(factura.Cedula, 14, FontAttributes.None, Texto)));
        foreach (var detalle in factura.Detalles)
            contenedorVoucher.Add(Fila(TextoLabel(detalle.Celular, 13, FontAttributes.Bold, Secundario), TextoLabel(detalle.Precio.ToString("C2"), 14, FontAttributes.Bold, Texto)));
        contenedorVoucher.Add(Fila(TextoLabel("Forma de pago:", 13, FontAttributes.Bold, Secundario),
            TextoLabel(esEfectivo ? "Efectivo" : $"Crédito directo ({factura.NumeroCuotas} cuotas)", 14, FontAttributes.None, Texto)));
        contenedorVoucher.Add(Separador());
        contenedorVoucher.Add(Fila(TextoLabel("Precio de lista:", 13, FontAttributes.None, Secundario), TextoLabel(factura.Precio.ToString("C2"), 14, FontAttributes.None, Texto)));
        if (factura.Descuento > 0)
        {
            contenedorVoucher.Add(Fila(TextoLabel("Descuento (42%):", 13, FontAttributes.None, Secundario), TextoLabel($"-{factura.Descuento:C2}", 14, FontAttributes.Bold, Acento)));
        }
        contenedorVoucher.Add(Separador());
        contenedorVoucher.Add(Fila(TextoLabel("TOTAL FACTURADO:", 15, FontAttributes.Bold, Secundario), TextoLabel(factura.Total.ToString("C2"), 22, FontAttributes.Bold, Acento)));

        if (!esEfectivo && esAprobada)
        {
            try
            {
                var cuotasData = await Controlador().Amortizacion(factura.Id);
                foreach (var cuota in cuotasData)
                {
                    if (cuota.NumeroCuota == 0)
                    {
                        amortizacion.Add(new Border
                        {
                            Padding = 14, StrokeShape = new RoundRectangle { CornerRadius = 14 },
                            Content = Fila(TextoLabel("Saldo Inicial (Capital financiado):", 13, FontAttributes.Bold, Secundario),
                                           TextoLabel(cuota.Saldo.ToString("C2"), 16, FontAttributes.Bold, Texto))
                        }.ConFondo(Panel, Borde));
                        continue;
                    }

                    var tarjetaCuota = new Border
                    {
                        Padding = 14, StrokeShape = new RoundRectangle { CornerRadius = 14 },
                        Content = new VerticalStackLayout
                        {
                            Spacing = 6,
                            Children =
                            {
                                Fila(TextoLabel($"Cuota #{cuota.NumeroCuota}", 13, FontAttributes.Bold, Acento),
                                     TextoLabel($"Fecha: {cuota.FechaPago:yyyy-MM-dd}", 12, FontAttributes.None, Secundario)),
                                Fila(TextoLabel("Valor Cuota mensual:", 13, FontAttributes.Bold, Texto),
                                     TextoLabel(cuota.ValorCuota.ToString("C2"), 16, FontAttributes.Bold, Acento)),
                                Fila(TextoLabel($"Interés pagado: {cuota.InteresPagado:C2}", 12, FontAttributes.None, Secundario),
                                     TextoLabel($"Capital pagado: {cuota.CapitalPagado:C2}", 12, FontAttributes.None, Secundario)),
                                Fila(TextoLabel("Saldo restante:", 12, FontAttributes.Bold, Secundario),
                                     TextoLabel(cuota.Saldo.ToString("C2"), 13, FontAttributes.Bold, Texto))
                            }
                        }
                    }.ConFondo(Panel, Borde);

                    amortizacion.Add(tarjetaCuota);
                }
            }
            catch (Exception ex)
            {
                amortizacion.Add(TextoLabel("No se pudo cargar la tabla de amortización: " + ex.Message, 13, FontAttributes.None, PeligroColor));
            }
        }
    }

    // =========================================================================
    // VISTA 4: EDITOR DE MODELO Y FOTO (IGUAL AL MODAL WEB/DESKTOP)
    // =========================================================================
    private ContentPage CrearPaginaEditor()
    {
        var pagina = Pagina("Administrar Celular", avisoCatalogo);
        quitarFoto.IsEnabled = false;
        desactivar.IsVisible = false;

        var volver = Boton("⬅️  Volver al catálogo", async () => await Navigation.PopAsync(), secundario: true);

        var seccionFoto = Seccion("Fotografía del Celular",
            foto,
            TextoLabel("Formatos admitidos: JPG, PNG o WebP (máx. 5 MB).", 12, FontAttributes.None, Secundario),
            Boton("📷  Cargar o reemplazar foto", ElegirFoto, secundario: true),
            quitarFoto);

        var seccionDatos = Seccion("Información del Equipo",
            TextoLabel("Nombre del modelo:", 13, FontAttributes.Bold, Secundario),
            nombre,
            TextoLabel("Precio de lista (USD):", 13, FontAttributes.Bold, Secundario),
            precio,
            precioEfectivoPreview);

        pagina.Content = Contenido(volver, seccionFoto, seccionDatos, Boton("💾  Guardar celular", GuardarCelular), desactivar);
        return pagina;
    }

    private void ActualizarPreviewPrecioEditor()
    {
        if (decimal.TryParse(precio.Text?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var p) && p > 0)
        {
            var desc = ControladorTienda.TotalEfectivo(p);
            precioEfectivoPreview.Text = $"Efectivo (42% desc): {desc:C2}";
            precioEfectivoPreview.SetDynamicResource(Label.TextColorProperty, "AcentoDinamico");
        }
        else
        {
            precioEfectivoPreview.Text = "Efectivo (42% desc): $0.00";
            precioEfectivoPreview.SetDynamicResource(Label.TextColorProperty, "SecundarioDinamico");
        }
    }

    private string ServidorActual
    {
        get
        {
            var desdeEntorno = Environment.GetEnvironmentVariable("CELULARES_API_URL");
            return string.IsNullOrWhiteSpace(desdeEntorno)
                ? Preferences.Default.Get("celulares.servidor", MauiProgram.DireccionServidor)
                : MauiProgram.DireccionServidor;
        }
        set => Preferences.Default.Set("celulares.servidor", value);
    }

    private string ServidorCorto => ServidorActual.Replace("http://", "").Replace("https://", "").TrimEnd('/');

    private async Task CambiarServidor()
    {
        var nuevo = await DisplayPromptAsync(
            "Configurar IP del Servidor",
            "Ingresa la IP o URL de la API Comercializadora:\n\n• Hotspot PC: 192.168.137.1:19102\n• Wi-Fi PC: 192.168.9.106:19102\n• Emulador: 10.0.2.2:19102",
            "Conectar",
            "Cancelar",
            initialValue: ServidorActual,
            keyboard: Keyboard.Url);

        if (string.IsNullOrWhiteSpace(nuevo)) return;
        nuevo = nuevo.Trim();
        if (!nuevo.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !nuevo.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            nuevo = "http://" + nuevo;
        }
        if (!nuevo.EndsWith("/"))
        {
            nuevo += "/";
        }

        ServidorActual = nuevo;
        if (botonServidor != null)
            botonServidor.Text = $"⚙️  Servidor: {ServidorCorto}";

        await Conectar();
    }

    // =========================================================================
    // LÓGICA DE NEGOCIO Y CONECTIVIDAD
    // =========================================================================
    private async Task Conectar()
    {
        try
        {
            avisoCatalogo.Text = $"Conectando a {ServidorCorto}...";
            http?.Dispose();
            http = new HttpClient
            {
                BaseAddress = ClienteComercializadora.Direccion(ServidorActual),
                Timeout = TimeSpan.FromSeconds(10)
            };
            tienda = new ControladorTienda(new ClienteComercializadora(http));
            await CargarCatalogo();
        }
        catch (Exception ex)
        {
            avisoCatalogo.Text = $"⚠️ Sin conexión a {ServidorCorto}. Presiona '⚙️ IP' para configurar la dirección de tu PC.";
            var cambiar = await DisplayAlertAsync(
                "Sin conexión a la API",
                $"No se pudo conectar al servidor en:\n{ServidorActual}\n\nDetalle: {ex.Message}\n\n¿Deseas cambiar la IP?",
                "Cambiar IP",
                "Reintentar");

            if (cambiar)
            {
                await CambiarServidor();
            }
        }
    }

    private async Task CargarCatalogo()
    {
        botonActualizarCatalogo.IsEnabled = false;
        botonActualizarCatalogo.Text = "🔄  Actualizando...";
        try
        {
            celulares = await Controlador().Catalogo();
            var anteriores = seleccionVenta.Where(p => p.Value.IsChecked).Select(p => p.Key).ToHashSet();
            seleccionVenta.Clear();
            celularVenta.Clear();
            foreach (var cel in celulares)
            {
                var casilla = new CheckBox { IsChecked = anteriores.Contains(cel.Id) };
                casilla.CheckedChanged += (_, _) => ActualizarTotalVenta();
                seleccionVenta[cel.Id] = casilla;
                celularVenta.Add(Fila(TextoLabel($"{cel.Nombre} · {cel.Precio:C2}", 14, FontAttributes.None, Texto), casilla));
            }
            tarjetasCatalogo.Clear();

            if (celulares.Length == 0)
            {
                vacioCatalogoAviso.IsVisible = true;
                tarjetasCatalogo.IsVisible = false;
                avisoCatalogo.Text = "Catálogo vacío. Registre un celular para comenzar.";
            }
            else
            {
                vacioCatalogoAviso.IsVisible = false;
                tarjetasCatalogo.IsVisible = true;
                foreach (var cel in celulares)
                    tarjetasCatalogo.Add(TarjetaCelular(cel));

                avisoCatalogo.Text = $"Catálogo actualizado · {celulares.Length} celulares activos disponibles.";
            }
            ActualizarTotalVenta();
        }
        finally
        {
            botonActualizarCatalogo.IsEnabled = true;
            botonActualizarCatalogo.Text = "🔄  Actualizar catálogo";
        }
    }

    private async Task NuevoCelular()
    {
        editarId = null;
        fotoPendiente = null;
        nombreFotoPendiente = null;
        nombre.Text = null;
        precio.Text = null;
        foto.Source = "telefono.png";
        quitarFoto.IsEnabled = false;
        desactivar.IsVisible = false;
        ActualizarPreviewPrecioEditor();
        avisoCatalogo.Text = "Nuevo celular · Ingresa nombre y precio de lista.";
        await Navigation.PushAsync(editor);
    }

    private async Task EditarCelular(Celular cel)
    {
        editarId = cel.Id;
        nombre.Text = cel.Nombre;
        precio.Text = cel.Precio.ToString("F2", CultureInfo.InvariantCulture);
        fotoPendiente = null;
        nombreFotoPendiente = null;
        quitarFoto.IsEnabled = cel.ImagenUrl is not null;
        desactivar.IsVisible = true;
        foto.Source = "telefono.png";
        ActualizarPreviewPrecioEditor();

        if (cel.ImagenUrl is not null)
        {
            try
            {
                var bytes = await Controlador().DescargarImagen(cel);
                foto.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch (Exception ex)
            {
                avisoCatalogo.Text = "No se pudo descargar la foto: " + ex.Message;
            }
        }
        avisoCatalogo.Text = $"Modificando equipo #{cel.Id} · {cel.Nombre}";
        await Navigation.PushAsync(editor);
    }

    private async Task GuardarCelular() => await Ejecutar(async () =>
    {
        var nombreLimpio = nombre.Text?.Trim() ?? "";
        if (nombreLimpio.Length is < 2 or > 100)
            throw new ArgumentException("El nombre del modelo debe tener entre 2 y 100 caracteres.");

        var resultado = await Controlador().Guardar(editarId, nombreLimpio, ControladorTienda.InterpretarPrecio(precio.Text));
        editarId = resultado.Id;

        if (fotoPendiente is not null)
        {
            using var stream = new MemoryStream(fotoPendiente);
            await Controlador().SubirImagen(resultado.Id, stream, nombreFotoPendiente ?? "foto.jpg", TipoImagen(nombreFotoPendiente ?? "foto.jpg"));
            fotoPendiente = null;
            nombreFotoPendiente = null;
        }

        await CargarCatalogo();
        await Navigation.PopAsync();
        await DisplayAlertAsync("Éxito", $"Celular {resultado.Nombre} guardado correctamente.", "Aceptar");
    });

    private async Task ElegirFoto()
    {
        var archivo = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Selecciona una fotografía del celular", FileTypes = FilePickerFileType.Images });
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
                avisoCatalogo.Text = "Fotografía actualizada en base de datos SQL Server.";
            }
            else
            {
                fotoPendiente = bytes;
                nombreFotoPendiente = archivo.FileName;
                quitarFoto.IsEnabled = true;
                avisoCatalogo.Text = "Vista previa cargada · Presiona Guardar celular para confirmar.";
            }
        });
    }

    private async Task QuitarFoto()
    {
        if (fotoPendiente is not null)
        {
            fotoPendiente = null;
            nombreFotoPendiente = null;
            foto.Source = "telefono.png";
            quitarFoto.IsEnabled = false;
            avisoCatalogo.Text = "Foto pendiente descartada.";
            return;
        }

        if (editarId is not int id || !await DisplayAlertAsync("Quitar fotografía", "¿Deseas eliminar la foto asignada a este celular?", "Eliminar", "Cancelar")) return;

        await Ejecutar(async () =>
        {
            await Controlador().QuitarImagen(id);
            quitarFoto.IsEnabled = false;
            foto.Source = "telefono.png";
            await CargarCatalogo();
            avisoCatalogo.Text = "Fotografía eliminada de la base de datos.";
        });
    }

    private async Task Desactivar()
    {
        if (editarId is not int id || !await DisplayAlertAsync("Desactivar celular", "¿Estás seguro de desactivar este equipo? Las facturas históricas ya emitidas se conservarán.", "Desactivar", "Cancelar")) return;

        await Ejecutar(async () =>
        {
            await Controlador().Eliminar(id);
            await CargarCatalogo();
            await Navigation.PopAsync();
            await DisplayAlertAsync("Equipo desactivado", "El celular ya no estará disponible para nuevas ventas.", "Aceptar");
        });
    }

    private void ActualizarTotalVenta()
    {
        var esCredito = forma.SelectedIndex == 1;
        camposCredito.IsVisible = esCredito;
        var seleccionados = celulares.Where(c => seleccionVenta.TryGetValue(c.Id, out var casilla) && casilla.IsChecked).ToArray();

        if (seleccionados.Length == 0)
        {
            resumenPrecio.Text = "Selecciona un celular para ver su precio.";
            totalVenta.Text = "$0.00";
            return;
        }

        var precioLista = seleccionados.Sum(c => c.Precio);
        var total = esCredito ? precioLista : ControladorTienda.TotalEfectivo(precioLista);
        resumenPrecio.Text = $"Precio de lista: {precioLista:C2}" + (esCredito ? "" : $"  ·  Descuento 42%: -{(precioLista - total):C2}");
        totalVenta.Text = total.ToString("C2");
    }

    private async Task ConsultarElegibilidad() => await Ejecutar(async () =>
    {
        var ced = cedula.Text?.Trim() ?? "";
        if (ced.Length is < 10 or > 13 || ced.Any(c => !char.IsDigit(c)))
            throw new ArgumentException("Ingresa una cédula válida de 10 a 13 dígitos para consultar.");

        elegibilidad.Text = "Consultando elegibilidad con BanQuito...";
        elegibilidad.SetDynamicResource(Label.TextColorProperty, "SecundarioDinamico");

        var resultado = await Controlador().EvaluarCredito(ced);
        if (resultado.SujetoCredito)
        {
            elegibilidad.Text = $"✓ Sujeto de crédito aprobado en BanQuito · Monto máximo: {resultado.MontoMaximo:C2}";
            elegibilidad.SetDynamicResource(Label.TextColorProperty, "AcentoDinamico");
            if (!string.IsNullOrWhiteSpace(resultado.Nombre))
                comprador.Text = resultado.Nombre;
        }
        else
        {
            elegibilidad.Text = $"✗ No elegible · {resultado.Motivo ?? "No cumple con las reglas de crédito de BanQuito."}";
            elegibilidad.TextColor = PeligroColor;
            if (!string.IsNullOrWhiteSpace(resultado.Nombre))
                comprador.Text = resultado.Nombre;
        }
    });

    private async Task Vender() => await Ejecutar(async () =>
    {
        var seleccionados = celulares.Where(c => seleccionVenta.TryGetValue(c.Id, out var casilla) && casilla.IsChecked).OrderBy(c => c.Id).ToArray();
        if (seleccionados.Length == 0)
            throw new ArgumentException("Elige un celular del catálogo.");
        var celular = seleccionados[0];
        var precioLista = seleccionados.Sum(c => c.Precio);

        var numCedula = cedula.Text?.Trim() ?? "";
        var nomCliente = comprador.Text?.Trim() ?? "";

        if (numCedula.Length is < 10 or > 13 || numCedula.Any(c => !char.IsDigit(c)))
            throw new ArgumentException("Ingresa una cédula válida de 10 a 13 dígitos.");

        if (nomCliente.Length is < 2 or > 100)
            throw new ArgumentException("Ingresa el nombre completo del comprador.");

        var esCredito = forma.SelectedIndex == 1;
        var formaPago = esCredito ? "CREDITO_DIRECTO" : "EFECTIVO";
        var cuotasNum = esCredito ? Entero(cuotas.Text) : (int?)null;

        var solicitud = new SolicitudFactura
        {
            Cedula = numCedula,
            NombreCliente = nomCliente,
            IdCelular = celular.Id,
            Detalles = seleccionados.Select(c => new DetalleSolicitudFactura(c.Id, c.Precio)).ToList(),
            FormaPago = formaPago,
            NumeroCuotas = cuotasNum,
            PrecioEsperado = celular.Precio
        };

        if (pendiente is not null && !MismaVenta(pendiente, solicitud))
            throw new InvalidOperationException("Hay una venta pendiente. Reintenta esa operación o descártala antes de registrar otra.");

        if (esCredito && pendiente is null)
        {
            var evaluacion = await Controlador().EvaluarCredito(numCedula);
            if (!evaluacion.SujetoCredito)
            {
                elegibilidad.Text = $"✗ No elegible · {evaluacion.Motivo}";
                elegibilidad.TextColor = PeligroColor;
                throw new InvalidOperationException(evaluacion.Motivo ?? "El cliente no califica para crédito según las reglas de BanQuito.");
            }
            if (precioLista > evaluacion.MontoMaximo)
            {
                elegibilidad.Text = $"✗ Crédito insuficiente · Máximo aprobado: {evaluacion.MontoMaximo:C2}";
                elegibilidad.TextColor = PeligroColor;
                throw new InvalidOperationException($"El total ({precioLista:C2}) supera el monto máximo de crédito aprobado por BanQuito ({evaluacion.MontoMaximo:C2}).");
            }
        }

        var total = esCredito ? precioLista : ControladorTienda.TotalEfectivo(precioLista);
        var detalleModal = $"{string.Join(", ", seleccionados.Select(c => c.Nombre))}\nComprador: {nomCliente}\nPago: {(esCredito ? $"Crédito BanQuito ({cuotasNum} cuotas)" : "Efectivo (42% desc)")}\nTotal a pagar: {total:C2}";

        if (!await DisplayAlertAsync("Confirmar venta", detalleModal, "Emitir factura", "Cancelar")) return;

        pendiente = solicitud;
        reintentar.IsVisible = true;
        descartarVenta.IsVisible = true;

        var factura = await Controlador().Facturar(pendiente);
        pendiente = null;
        reintentar.IsVisible = false;
        descartarVenta.IsVisible = false;

        await MostrarFactura(factura);
        await GoToAsync("//facturasMenu/facturasSeccion/facturas");
        SeleccionarMenu("Facturas");
        await Navigation.PushAsync(detalleFactura);
    });

    private async Task Reintentar()
    {
        if (pendiente is null) return;
        await Ejecutar(async () =>
        {
            var factura = await Controlador().Facturar(pendiente);
            pendiente = null;
            reintentar.IsVisible = false;
            descartarVenta.IsVisible = false;
            await MostrarFactura(factura);
            await GoToAsync("//facturasMenu/facturasSeccion/facturas");
            SeleccionarMenu("Facturas");
            await Navigation.PushAsync(detalleFactura);
        });
    }

    private async Task DescartarVenta()
    {
        if (pendiente is null) return;
        if (!await DisplayAlertAsync("Descartar venta", "¿Deseas descartar la venta pendiente actual?", "Descartar", "Volver")) return;
        pendiente = null;
        reintentar.IsVisible = false;
        descartarVenta.IsVisible = false;
        avisoVenta.Text = "Venta pendiente descartada.";
    }

    private async Task BuscarFacturas() => await Ejecutar(async () =>
    {
        var numero = busqueda.Text?.Trim() ?? "";
        if (numero.Length is < 10 or > 13 || numero.Any(c => !char.IsDigit(c)))
            throw new ArgumentException("Ingresa una cédula válida de 10 a 13 dígitos para buscar facturas.");

        var facturas = await Controlador().FacturasPorCedula(numero);
        listaFacturas.Clear();
        foreach (var fact in facturas)
            listaFacturas.Add(TarjetaFactura(fact));

        avisoFacturas.Text = facturas.Length == 0
            ? "No se encontraron facturas para la cédula ingresada."
            : $"Se encontraron {facturas.Length} factura(s) asociadas · Presiona 'Ver detalle' para consultar el comprobante.";
    });

    private async Task Ejecutar(Func<Task> accion)
    {
        try { await accion(); }
        catch (Exception ex)
        {
            var mensaje = ex.Message;
            await DisplayAlertAsync("Aviso del Sistema", mensaje, "Entendido");
        }
    }

    private async Task CambiarTema()
    {
        temaClaro = !temaClaro;
        Preferences.Default.Set("celulares.temaClaro", temaClaro);
        AplicarTema();
        botonTema.Text = temaClaro ? "🌙  Activar tema oscuro" : "☀️  Activar tema claro";
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
        recursos["PeligroDinamico"] = PeligroColor;
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
            var barraColor = Android.Graphics.Color.ParseColor(barraClaraSoportada ? "#F3F6FA" : "#080D18");
#pragma warning disable CA1422
            window.SetStatusBarColor(barraColor);
            window.SetNavigationBarColor(barraColor);
#pragma warning restore CA1422
        }
#endif
    }

    private static async Task<byte[]> LeerImagen(FileResult archivo)
    {
        await using var origen = await archivo.OpenReadAsync();
        using var destino = new MemoryStream();
        var buffer = new byte[81920];
        int leidos;
        while ((leidos = await origen.ReadAsync(buffer)) > 0)
        {
            if (destino.Length + leidos > 5 * 1024 * 1024) throw new ArgumentException("La fotografía no debe superar 5 MB.");
            await destino.WriteAsync(buffer.AsMemory(0, leidos));
        }
        if (destino.Length == 0) throw new ArgumentException("El archivo seleccionado está vacío.");
        return destino.ToArray();
    }

    private static string TipoImagen(string ruta) => System.IO.Path.GetExtension(ruta).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => throw new ArgumentException("Usa una fotografía en formato JPG, PNG o WebP.")
    };

    private static int Entero(string? texto) => int.TryParse(texto, out var n) && n is >= 3 and <= 18 ? n : throw new ArgumentException("El plazo de crédito debe ser entre 3 y 18 cuotas mensuales.");
    private static bool MismaVenta(SolicitudFactura a, SolicitudFactura b) => a.Cedula == b.Cedula && a.NombreCliente == b.NombreCliente && a.FormaPago == b.FormaPago && a.NumeroCuotas == b.NumeroCuotas && a.Detalles.SequenceEqual(b.Detalles);
    private ControladorTienda Controlador() => tienda ?? throw new InvalidOperationException("La comercializadora todavía no se conecta.");

    private Label TextoLabel(string texto, double tamano, FontAttributes estilo, Color color)
    {
        var label = new Label { Text = texto, FontSize = tamano, FontAttributes = estilo, LineBreakMode = LineBreakMode.WordWrap, VerticalOptions = LayoutOptions.Center };
        label.SetDynamicResource(Label.TextColorProperty, color == Acento ? "AcentoDinamico" : color == Secundario ? "SecundarioDinamico" : "TextoDinamico");
        return label;
    }

    private static Entry Campo(string placeholder, Keyboard? teclado = null) =>
        new() { Placeholder = placeholder, Keyboard = teclado ?? Keyboard.Default, ClearButtonVisibility = ClearButtonVisibility.WhileEditing, MinimumHeightRequest = 48 };

    private static View Separador() => new BoxView { HeightRequest = 1, Color = Color.FromArgb("#338EA5C2"), Margin = new Thickness(0, 4) };

    private View Fila(View izquierda, View derecha)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
        grid.Add(izquierda, 0);
        grid.Add(derecha, 1);
        return grid;
    }

    private Button Boton(string texto, Func<Task> accion, bool secundario = false, bool peligro = false)
    {
        var boton = new Button { Text = texto, CornerRadius = 12, FontSize = 14, FontAttributes = FontAttributes.Bold, MinimumHeightRequest = 46, Padding = new Thickness(14, 9) };
        boton.SetDynamicResource(VisualElement.BackgroundColorProperty, peligro ? "PeligroDinamico" : secundario ? "CampoDinamico" : "AcentoDinamico");
        boton.SetDynamicResource(Button.TextColorProperty, secundario ? "TextoDinamico" : "TextoBotonDinamico");
        boton.Clicked += async (_, _) =>
        {
            boton.IsEnabled = false;
            try { await accion(); }
            finally { boton.IsEnabled = true; }
        };
        return boton;
    }

    private static View Contenido(params View[] vistas)
    {
        var columna = new VerticalStackLayout { Padding = new Thickness(18, 16, 18, 28), Spacing = 16 };
        foreach (var vista in vistas) columna.Add(vista);
        return new ScrollView { Content = columna };
    }

    private View Seccion(string titulo, params View[] vistas)
    {
        var contenido = new VerticalStackLayout { Spacing = 10 };
        contenido.Add(TextoLabel(titulo, 15, FontAttributes.Bold, Acento));
        foreach (var vista in vistas) contenido.Add(vista);
        return new Border { Padding = 16, StrokeShape = new RoundRectangle { CornerRadius = 18 }, Content = contenido }.ConFondo(Panel, Borde);
    }

    private ContentPage Pagina(string titulo, Label aviso)
    {
        aviso.SetDynamicResource(Label.TextColorProperty, "AcentoDinamico");
        var pagina = new ContentPage { Title = titulo, BackgroundColor = Fondo };
        pagina.SetDynamicResource(Page.BackgroundColorProperty, "FondoDinamico");
        return pagina;
    }
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
