namespace Evol.Domain.Abstracciones;

public abstract class Entidad
{
    protected Entidad() => Id = Guid.NewGuid();

    protected Entidad(Guid id) => Id = id == Guid.Empty ? Guid.NewGuid() : id;

    public Guid Id { get; private set; }
}




