using Evol.Domain.Abstracciones;

namespace Evol.Domain.Entidades;

public sealed class PrecioReparacionModelo : Entidad
{
    private PrecioReparacionModelo() { }

    public PrecioReparacionModelo(Guid deviceModelId, string repairCode, decimal price, string currency, string verifiedOn)
    {
        DeviceModelId = deviceModelId;
        RepairCode = repairCode.Trim();
        SetPrice(price, currency, verifiedOn);
    }

    public Guid DeviceModelId { get; private set; }
    public string RepairCode { get; private set; } = "";
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "PEN";
    public string VerifiedOn { get; private set; } = "";
    public ModeloDispositivo ModeloDispositivo { get; private set; } = null!;
    public ServicioReparacion ServicioReparacion { get; private set; } = null!;

    public void SetPrice(decimal price, string currency, string verifiedOn)
    {
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        Price = price;
        Currency = currency.Trim().ToUpperInvariant();
        VerifiedOn = verifiedOn.Trim();
    }
}




