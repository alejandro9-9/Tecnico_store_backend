namespace TecFast.Api;

internal static class MapeadorRutas
{
    public static void MapearRutasTecFast(this WebApplication app)
    {
        app.MapearRutasPublicas();
        app.MapearRutasTecnico();
        app.MapearRutasAdministracion();
    }
}
