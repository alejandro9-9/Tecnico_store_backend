using TecFast.Domain.Abstracciones;

namespace TecFast.Domain.Entidades;

public sealed class CotizacionReparacion : Entidad
{
    private CotizacionReparacion() { }

    public CotizacionReparacion(DateTimeOffset createdAt, string customerMessage, string? brand, string? model,
        string? issueCode, EstadoCotizacion status, decimal? minPrice, decimal? maxPrice, string reply)
    {
        CreatedAt = createdAt;
        CustomerMessage = customerMessage;
        Brand = brand;
        Model = model;
        IssueCode = issueCode;
        Status = status;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        Reply = reply;
    }

    public DateTimeOffset CreatedAt { get; private set; }
    public string CustomerMessage { get; private set; } = "";
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public string? IssueCode { get; private set; }
    public EstadoCotizacion Status { get; private set; } = EstadoCotizacion.NeedsDetails;
    public decimal? MinPrice { get; private set; }
    public decimal? MaxPrice { get; private set; }
    public string Reply { get; private set; } = "";
    public ICollection<Cita> Appointments { get; private set; } = new List<Cita>();
}




