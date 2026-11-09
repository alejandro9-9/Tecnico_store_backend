namespace TecFast.Application;

public interface IServicioDisponibilidad
{
    string? Validar(DateTimeOffset startsAt, DateTimeOffset endsAt, Guid? excludeAppointmentId = null);
}
