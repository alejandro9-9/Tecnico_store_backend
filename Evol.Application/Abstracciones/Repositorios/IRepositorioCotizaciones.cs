namespace Evol.Application.Abstracciones.Repositorios;

public interface IRepositorioCotizaciones
{
    Guid GuardarCotizacion(DateTimeOffset createdAt, string customerMessage, string? brand, string? model,
        string? issueCode, string status, decimal? minPrice, decimal? maxPrice, string reply);
    CotizacionParaCita? ObtenerCotizacionParaCita(Guid id);
}




