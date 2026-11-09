using TecFast.Application;

namespace TecFast.Api;

internal static class RutasPublicas
{
    public static void MapearRutasPublicas(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "tecfast-api" }));
        app.MapGet("/api/services", (IServicioCatalogo catalog) => Results.Ok(catalog.ObtenerServicios()));
        app.MapGet("/api/symptoms", (IServicioCatalogo catalog) => Results.Ok(catalog.ObtenerSintomas()));
        app.MapGet("/api/models", (IServicioCatalogo catalog) => Results.Ok(catalog.ObtenerModelos()));
        app.MapPost("/api/chat", (SolicitudChat request, IServicioCotizaciones quotes) => Http.Convertir(quotes.CrearCotizacion(request)));
        app.MapPost("/api/appointments", (NuevaCita request, IServicioCitas appointments) =>
            Http.Convertir(appointments.Crear(request), appointment => $"/api/appointments/{appointment.Id}"));
    }
}
