namespace Evol.Application.Abstracciones.Repositorios;

public interface IRepositorioBloqueosHorario
{
    IReadOnlyList<RespuestaBloqueHorario> ObtenerBloqueos();
    bool SeSuperpone(DateTimeOffset startsAt, DateTimeOffset endsAt);
    RespuestaBloqueHorario Crear(DateTimeOffset startsAt, DateTimeOffset endsAt, string reason);
    bool Eliminar(Guid id);
}




