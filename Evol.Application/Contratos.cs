using Evol.Domain;

namespace Evol.Application;

public sealed record MensajeChat(string Role, string Content);
public sealed record SolicitudChat(IReadOnlyList<MensajeChat> Messages, string? Brand = null, string? Model = null);
public sealed record RespuestaCotizacion(Guid QuoteId, string Status, string Reply, string? Brand, string? Model,
    string? IssueCode, string? IssueName, decimal? MinPrice, decimal? MaxPrice, string Currency = "PEN",
    string Disclaimer = "La cotización final requiere confirmación técnica.", bool IsSimulated = false);
public sealed record RespuestaServicio(string Code, string Name, string Notes);
public sealed record RespuestaModeloCatalogo(string Brand, string Model, Dictionary<string, decimal> Prices);
public sealed record ActualizarPrecioReparacionModelo(string Brand, string Model, string RepairCode, decimal Price);
public sealed record RespuestaPrecioReparacionModeloAdmin(string Brand, string Model, string RepairCode, string RepairName,
    decimal Price, string Currency, string VerifiedOn);
public sealed record NuevaCita(Guid? QuoteId, string CustomerName, string CustomerPhone,
    string Brand, string Model, string Issue, DateTimeOffset ScheduledAt, string Notes = "");
public sealed record ActualizarDisponibilidadCita(string Status);
public sealed record RespuestaCita(Guid Id, Guid? QuoteId, string CustomerName, string CustomerPhone,
    string Brand, string Model, string Issue, DateTimeOffset ScheduledAt, string Status, string Notes,
    DateTimeOffset CreatedAt, decimal? QuotePrice = null, bool QuoteIsSimulated = false);
public sealed record NuevoBloqueHorario(DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    string Reason = "Horario no disponible");
public sealed record RespuestaBloqueHorario(Guid Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    string Reason, DateTimeOffset CreatedAt);
public sealed record ResultadoOperacion<T>(int StatusCode, T? Value = default, string? Error = null)
{
    public bool Succeeded => StatusCode is >= 200 and < 300;
    public static ResultadoOperacion<T> Ok(T value) => new(200, value);
    public static ResultadoOperacion<T> Created(T value) => new(201, value);
    public static ResultadoOperacion<T> Fail(int statusCode, string error) => new(statusCode, default, error);
}
public sealed record CotizacionParaCita(string? Brand, string? Model, string? IssueCode, string Status);

public interface IServicioCatalogo
{
    IReadOnlyList<RespuestaServicio> ObtenerServicios();
    IReadOnlyList<RespuestaModeloCatalogo> ObtenerModelos();
    IReadOnlyList<object> ObtenerSintomas();
}
public interface IServicioCotizaciones { ResultadoOperacion<RespuestaCotizacion> CrearCotizacion(SolicitudChat request); }
public interface IServicioCitas
{
    ResultadoOperacion<RespuestaCita> Crear(NuevaCita request);
    IReadOnlyList<RespuestaCita> ObtenerParaTecnico();
    ResultadoOperacion<RespuestaCita> DecidirDisponibilidad(Guid id, string status);
}
public interface IServicioAdministracion
{
    IReadOnlyList<RespuestaCita> ObtenerCitas();
    IReadOnlyList<RespuestaPrecioReparacionModeloAdmin> ObtenerPreciosModelo();
    ResultadoOperacion<RespuestaPrecioReparacionModeloAdmin> GuardarPrecioModelo(ActualizarPrecioReparacionModelo request);
    IReadOnlyList<RespuestaBloqueHorario> ObtenerBloqueos();
    ResultadoOperacion<RespuestaBloqueHorario> CrearBloqueo(NuevoBloqueHorario request);
    ResultadoOperacion<bool> EliminarBloqueo(Guid id);
}




