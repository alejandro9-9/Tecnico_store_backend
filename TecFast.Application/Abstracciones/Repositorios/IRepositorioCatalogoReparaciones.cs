using TecFast.Domain;

namespace TecFast.Application.Abstracciones.Repositorios;

public interface IRepositorioCatalogoReparaciones
{
    IReadOnlyList<ElementoServicio> ObtenerServicios();
    IReadOnlyList<RespuestaModeloCatalogo> ObtenerModelos();
    IReadOnlyList<RespuestaPrecioReparacionModeloAdmin> ObtenerPreciosModelo();
    ElementoServicio? BuscarServicio(string? code);
    string? BuscarNombreServicio(string? code);
    PrecioRegistrado? BuscarPrecioExacto(string? brand, string? model, string? repairCode);
    ResultadoOperacion<RespuestaPrecioReparacionModeloAdmin> GuardarPrecioModelo(ActualizarPrecioReparacionModelo request, string verifiedOn);
}




