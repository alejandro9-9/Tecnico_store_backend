using Evol.Domain;

namespace Evol.Application.Abstracciones.Repositorios;

public interface IRepositorioCatalogoReparaciones
{
    IReadOnlyList<ElementoServicio> ObtenerServicios();
    IReadOnlyList<RespuestaModeloCatalogo> ObtenerModelos();
    IReadOnlyList<RespuestaPrecioReparacionModeloAdmin> ObtenerPreciosModelo();
    string? BuscarNombreServicio(string? code);
    PrecioRegistrado? BuscarPrecioExacto(string? brand, string? model, string? repairCode);
    ResultadoOperacion<RespuestaPrecioReparacionModeloAdmin> GuardarPrecioModelo(ActualizarPrecioReparacionModelo request, string verifiedOn);
}




