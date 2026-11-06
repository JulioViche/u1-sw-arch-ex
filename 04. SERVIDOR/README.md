# 04. SERVIDOR — Comercializadora

`Comercializadora.Api` administra catálogo y facturación. `Comercializadora.Dominio` contiene la regla de precio en efectivo; `Contratos` contiene modelos de tienda y DTOs del cliente HTTP.

En una venta a crédito, la API persiste la factura pendiente y llama por HTTP a BanQuito.Api, que vive en el proyecto bancario. Usa `Servicios:BanQuitoUrl` para configurar la URL. El proyecto celular no referencia código bancario ni contiene su inicializador.

Para elegibilidad, otorgamiento y amortización, consulta `U1/TG.1/1.4. PAO202651_RESTFUL_DOTNET_CONBDD_NRC36884_GR09/README.md`.
