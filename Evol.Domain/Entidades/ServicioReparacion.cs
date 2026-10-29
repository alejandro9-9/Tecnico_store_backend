using Evol.Domain.Abstracciones;

namespace Evol.Domain.Entidades;

public sealed class ServicioReparacion : Entidad
{
    private ServicioReparacion() { }

    public ServicioReparacion(string code, string name, string notes, bool requiresDiagnosis = true)
    {
        Code = code.Trim();
        Name = name.Trim();
        Notes = notes.Trim();
        RequiresDiagnosis = requiresDiagnosis;
    }

    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Notes { get; private set; } = "";
    public bool RequiresDiagnosis { get; private set; }
}




