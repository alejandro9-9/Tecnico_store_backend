using TecFast.Domain;

namespace TecFast.Application;

public sealed class ServicioCatalogo(IRepositorioCatalogoReparaciones catalog, ClasificadorSintomas classifier) : IServicioCatalogo
{
    public IReadOnlyList<RespuestaServicio> ObtenerServicios() => catalog.ObtenerServicios()
        .Select(item => new RespuestaServicio(item.Code, item.Name, item.Notes)).ToList();
    public IReadOnlyList<RespuestaModeloCatalogo> ObtenerModelos() => catalog.ObtenerModelos();
    public IReadOnlyList<object> ObtenerSintomas() => classifier.ObtenerReglasPublicas();
}




