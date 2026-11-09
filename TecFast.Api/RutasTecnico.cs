using TecFast.Application;

namespace TecFast.Api;

internal static class RutasTecnico
{
    public static void MapearRutasTecnico(this WebApplication app)
    {
        var group = app.MapGroup("/api/staff")
            .AddEndpointFilter(new FiltroToken("X-Staff-Token", "STAFF_TOKEN", "tec"));
        group.MapGet("/appointments", (IServicioCitas appointments) => Results.Ok(appointments.ObtenerParaTecnico()));
        group.MapPatch("/appointments/{appointmentId:guid}", (Guid appointmentId, ActualizarDisponibilidadCita request, IServicioCitas appointments) =>
            Http.Convertir(appointments.DecidirDisponibilidad(appointmentId, request.Status)));
    }
}
