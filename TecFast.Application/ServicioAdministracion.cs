using TecFast.Domain;

namespace TecFast.Application;

public sealed class ServicioAdministracion(
    IRepositorioCitas appointments,
    IRepositorioCatalogoReparaciones catalog,
    IRepositorioBloqueosHorario blocks,
    IServicioDisponibilidad availability) : IServicioAdministracion
{
    public IReadOnlyList<RespuestaCita> ObtenerCitas() => appointments.ObtenerCitas(includePrice: true);
    public IReadOnlyList<RespuestaPrecioReparacionModeloAdmin> ObtenerPreciosModelo() => catalog.ObtenerPreciosModelo();
    public IReadOnlyList<RespuestaBloqueHorario> ObtenerBloqueos() => blocks.ObtenerBloqueos();

    public ResultadoOperacion<RespuestaPrecioReparacionModeloAdmin> GuardarPrecioModelo(ActualizarPrecioReparacionModelo request)
    {
        if (string.IsNullOrWhiteSpace(request.Brand) || request.Brand.Trim().Length < 2 || request.Brand.Length > 80 ||
            string.IsNullOrWhiteSpace(request.Model) || request.Model.Length > 120 || request.Price < 0 || request.Price > 99999999.99m)
            return ResultadoOperacion<RespuestaPrecioReparacionModeloAdmin>.Fail(422, "Completa una marca, modelo y precio válidos.");
        var verifiedOn = PoliticaCitas.ConvertirHoraLima(DateTimeOffset.UtcNow).ToString("yyyy-MM-dd");
        return catalog.GuardarPrecioModelo(request, verifiedOn);
    }

    public ResultadoOperacion<RespuestaBloqueHorario> CrearBloqueo(NuevoBloqueHorario request)
    {
        var start = PoliticaCitas.ConvertirHoraLima(request.StartsAt); var end = PoliticaCitas.ConvertirHoraLima(request.EndsAt);
        if (start <= PoliticaCitas.ConvertirHoraLima(DateTimeOffset.UtcNow) || end <= start || (end - start).TotalDays > 31)
            return ResultadoOperacion<RespuestaBloqueHorario>.Fail(422, "Indica un horario futuro y una hora final posterior a la inicial.");
        if (availability.Validar(start, end) is { } error)
            return ResultadoOperacion<RespuestaBloqueHorario>.Fail(409, error);
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Horario no disponible" : request.Reason.Trim();
        return ResultadoOperacion<RespuestaBloqueHorario>.Created(blocks.Crear(start, end, reason));
    }

    public ResultadoOperacion<bool> EliminarBloqueo(Guid id) => blocks.Eliminar(id)
        ? ResultadoOperacion<bool>.Ok(true)
        : ResultadoOperacion<bool>.Fail(404, "No se encontró el bloqueo.");
}
