using Evol.Domain.Abstracciones;

namespace Evol.Domain.Entidades;

public sealed class Cita : Entidad
{
    private Cita() { }

    public Cita(Guid? quoteId, string customerName, string customerPhone, string brand, string model,
        string issue, DateTimeOffset scheduledAt, string status, string notes, DateTimeOffset createdAt)
    {
        QuoteId = quoteId;
        CustomerName = customerName.Trim();
        CustomerPhone = customerPhone.Trim();
        Brand = brand.Trim();
        Model = model.Trim();
        Issue = issue.Trim();
        ScheduledAt = scheduledAt;
        Status = status;
        Notes = notes.Trim();
        CreatedAt = createdAt;
    }

    public Guid? QuoteId { get; private set; }
    public string CustomerName { get; private set; } = "";
    public string CustomerPhone { get; private set; } = "";
    public string Brand { get; private set; } = "";
    public string Model { get; private set; } = "";
    public string Issue { get; private set; } = "";
    public DateTimeOffset ScheduledAt { get; private set; }
    public string Status { get; private set; } = "requested";
    public string Notes { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public CotizacionReparacion? Quote { get; private set; }

    public bool DecidirDisponibilidad(string status)
    {
        if (Status != "requested" || status is not ("confirmed" or "cancelled")) return false;
        Status = status;
        return true;
    }
}




