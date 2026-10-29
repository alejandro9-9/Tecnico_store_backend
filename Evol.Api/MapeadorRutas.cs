using System.Security.Cryptography;
using System.Text;
using Evol.Application;

namespace Evol.Api;

internal static class MapeadorRutas
{
    public static void MapearRutasEvol(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "evol-repair-api" }));
        app.MapGet("/api/services", (IServicioCatalogo catalog) => Results.Ok(catalog.ObtenerServicios()));
        app.MapGet("/api/symptoms", (IServicioCatalogo catalog) => Results.Ok(catalog.ObtenerSintomas()));
        app.MapGet("/api/models", (IServicioCatalogo catalog) => Results.Ok(catalog.ObtenerModelos()));
        app.MapPost("/api/chat", (SolicitudChat request, IServicioCotizaciones quotes) => ConvertirResultadoHttp(quotes.CrearCotizacion(request)));
        app.MapPost("/api/appointments", (NuevaCita request, IServicioCitas appointments) =>
            ConvertirResultadoHttp(appointments.Crear(request), id => $"/api/appointments/{id}"));
        app.MapGet("/api/staff/appointments", (HttpContext context, IServicioCitas appointments, IConfiguration config) =>
            EsTecnico(context, config) ? Results.Ok(appointments.ObtenerParaTecnico()) : Results.Unauthorized());
        app.MapPatch("/api/staff/appointments/{appointmentId:guid}", (Guid appointmentId, ActualizarDisponibilidadCita request,
            HttpContext context, IServicioCitas appointments, IConfiguration config) =>
            !EsTecnico(context, config) ? Results.Unauthorized() : ConvertirResultadoHttp(appointments.DecidirDisponibilidad(appointmentId, request.Status)));
        app.MapGet("/api/admin/appointments", (HttpContext context, IServicioAdministracion admin, IConfiguration config) =>
            EsAdministrador(context, config) ? Results.Ok(admin.ObtenerCitas()) : Results.Unauthorized());
        app.MapGet("/api/admin/model-prices", (HttpContext context, IServicioAdministracion admin, IConfiguration config) =>
            EsAdministrador(context, config) ? Results.Ok(admin.ObtenerPreciosModelo()) : Results.Unauthorized());
        app.MapPut("/api/admin/model-prices", (ActualizarPrecioReparacionModelo request, HttpContext context, IServicioAdministracion admin, IConfiguration config) =>
            !EsAdministrador(context, config) ? Results.Unauthorized() : ConvertirResultadoHttp(admin.GuardarPrecioModelo(request)));
        app.MapGet("/api/admin/blocks", (HttpContext context, IServicioAdministracion admin, IConfiguration config) =>
            EsAdministrador(context, config) ? Results.Ok(admin.ObtenerBloqueos()) : Results.Unauthorized());
        app.MapPost("/api/admin/blocks", (NuevoBloqueHorario request, HttpContext context, IServicioAdministracion admin, IConfiguration config) =>
            !EsAdministrador(context, config) ? Results.Unauthorized() : ConvertirResultadoHttp(admin.CrearBloqueo(request), value => $"/api/admin/blocks/{value.Id}"));
        app.MapDelete("/api/admin/blocks/{blockId:guid}", (Guid blockId, HttpContext context, IServicioAdministracion admin, IConfiguration config) =>
        {
            if (!EsAdministrador(context, config)) return Results.Unauthorized();
            var result = admin.EliminarBloqueo(blockId);
            return result.Succeeded ? Results.NoContent() : Results.NotFound(new { detail = result.Error });
        });
    }

    private static IResult ConvertirResultadoHttp<T>(ResultadoOperacion<T> result, Func<T, string>? location = null)
    {
        if (!result.Succeeded) return Results.Json(new { detail = result.Error }, statusCode: result.StatusCode);
        if (result.StatusCode == StatusCodes.Status201Created)
            return Results.Created(location?.Invoke(result.Value!) ?? "", result.Value);
        return Results.Json(result.Value, statusCode: result.StatusCode);
    }

    private static bool EsTecnico(HttpContext context, IConfiguration configuration) =>
        Coincide(context.Request.Headers["X-Staff-Token"].FirstOrDefault(), configuration["STAFF_TOKEN"] ?? "tec");

    private static bool EsAdministrador(HttpContext context, IConfiguration configuration) =>
        Coincide(context.Request.Headers["X-Admin-Token"].FirstOrDefault(), configuration["ADMIN_TOKEN"] ?? "adm");

    private static bool Coincide(string? provided, string expected)
    {
        if (string.IsNullOrEmpty(provided)) return false;
        var left = Encoding.UTF8.GetBytes(provided); var right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}




