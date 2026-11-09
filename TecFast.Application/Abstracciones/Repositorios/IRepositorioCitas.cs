using TecFast.Domain;

namespace TecFast.Application.Abstracciones.Repositorios;

public interface IRepositorioCitas
{
    IReadOnlyList<RespuestaCita> ObtenerCitas(bool includePrice);
    RespuestaCita? ObtenerCita(Guid id, bool includePrice);
    RespuestaCita Crear(NuevaCita request, string brand, string model, string issue, DateTimeOffset scheduledAt);
    bool ActualizarEstadoPendiente(Guid id, EstadoCita status);
    bool ExisteConflictoActivo(DateTimeOffset startsAt, DateTimeOffset endsAt, Guid? excludeId = null);
}




