using Evol.Domain.Abstracciones;

namespace Evol.Domain.Entidades;

public sealed class BloqueHorario : Entidad
{
    private BloqueHorario() { }

    public BloqueHorario(DateTimeOffset startsAt, DateTimeOffset endsAt, string reason, DateTimeOffset createdAt)
    {
        StartsAt = startsAt;
        EndsAt = endsAt;
        Reason = reason.Trim();
        CreatedAt = createdAt;
    }

    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string Reason { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
}




