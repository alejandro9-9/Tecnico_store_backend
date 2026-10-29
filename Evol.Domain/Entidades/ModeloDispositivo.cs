using Evol.Domain.Abstracciones;

namespace Evol.Domain.Entidades;

public sealed class ModeloDispositivo : Entidad
{
    private ModeloDispositivo() { }

    public ModeloDispositivo(string brand, string model)
    {
        Brand = brand.Trim();
        Model = model.Trim();
    }

    public string Brand { get; private set; } = "";
    public string Model { get; private set; } = "";
    public ICollection<PrecioReparacionModelo> RepairPrices { get; private set; } = new List<PrecioReparacionModelo>();
}




