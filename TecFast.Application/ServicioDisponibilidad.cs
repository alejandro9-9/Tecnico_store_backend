namespace TecFast.Application;

public sealed class ServicioDisponibilidad(
    IRepositorioCitas appointments,
    IRepositorioBloqueosHorario blocks) : IServicioDisponibilidad
{
    public string? Validar(DateTimeOffset startsAt, DateTimeOffset endsAt, Guid? excludeAppointmentId = null)
    {
        if (blocks.SeSuperpone(startsAt, endsAt))
            return "Ese horario está bloqueado por el taller. Elige otra fecha u hora.";
        if (appointments.ExisteConflictoActivo(startsAt, endsAt, excludeAppointmentId))
            return "Ese horario ya está ocupado por otra cita activa. Elige otra fecha u hora.";
        return null;
    }
}
