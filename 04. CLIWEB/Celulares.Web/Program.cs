using Celulares.Cliente.Controladores;
using Celulares.Cliente.Servicios;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<ClienteComercializadora>(cliente =>
{
    cliente.BaseAddress = ClienteComercializadora.Direccion(builder.Configuration["CelularesApiUrl"]);
    cliente.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddTransient<ControladorTienda>();
var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Tienda}/{action=Index}/{id?}");
app.Run();
