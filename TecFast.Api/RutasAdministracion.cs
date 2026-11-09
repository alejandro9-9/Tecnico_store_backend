using TecFast.Application;

namespace TecFast.Api;

internal static class RutasAdministracion
{
    public static void MapearRutasAdministracion(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin")
            .AddEndpointFilter(new FiltroToken("X-Admin-Token", "ADMIN_TOKEN", "adm"));
        group.MapGet("/appointments", (IServicioAdministracion admin) => Results.Ok(admin.ObtenerCitas()));
        group.MapGet("/model-prices", (IServicioAdministracion admin) => Results.Ok(admin.ObtenerPreciosModelo()));
        group.MapPut("/model-prices", (ActualizarPrecioReparacionModelo request, IServicioAdministracion admin) =>
            Http.Convertir(admin.GuardarPrecioModelo(request)));
        group.MapGet("/blocks", (IServicioAdministracion admin) => Results.Ok(admin.ObtenerBloqueos()));
        group.MapPost("/blocks", (NuevoBloqueHorario request, IServicioAdministracion admin) =>
            Http.Convertir(admin.CrearBloqueo(request), value => $"/api/admin/blocks/{value.Id}"));
        group.MapDelete("/blocks/{blockId:guid}", (Guid blockId, IServicioAdministracion admin) =>
        {
            var result = admin.EliminarBloqueo(blockId);
            return result.Succeeded ? Results.NoContent() : Results.NotFound(new { detail = result.Error });
        });
    }
}
