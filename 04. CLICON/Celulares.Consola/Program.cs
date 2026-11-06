using Celulares.Cliente.Controladores;
using Celulares.Cliente.Servicios;
using Celulares.Consola.Controladores;

using var http = new HttpClient
{
    BaseAddress = ClienteComercializadora.Direccion(Environment.GetEnvironmentVariable("CELULARES_API_URL")),
    Timeout = TimeSpan.FromSeconds(30)
};
await new ControladorConsola(new ControladorTienda(new ClienteComercializadora(http))).Ejecutar();
