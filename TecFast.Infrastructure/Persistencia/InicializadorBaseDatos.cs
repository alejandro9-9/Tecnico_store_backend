using System.Globalization;
using TecFast.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace TecFast.Infrastructure.Persistencia;

public sealed class InicializadorBaseDatos(ContextoTecFast db)
{
    private const string PrecioMercadoVersion = "MERCADO-2026-11-09";

    public void Inicializar()
    {
        db.Database.EnsureCreated();
        CargarCatalogoInicial();
    }

    private void CargarCatalogoInicial()
    {
        var root = AppContext.BaseDirectory;
        foreach (var row in LectorCsv.Read(Path.Combine(root, "catalog.csv")))
        {
            var code = row.GetValueOrDefault("code", "");
            if (!db.ServiciosReparacion.Any(x => x.Code == code))
            {
                var requiresDiagnosis = !bool.TryParse(row.GetValueOrDefault("requires_diagnosis"), out var parsed) || parsed;
                db.ServiciosReparacion.Add(new ServicioReparacion(code, row.GetValueOrDefault("name", ""), row.GetValueOrDefault("notes", ""), requiresDiagnosis));
            }
        }
        foreach (var row in LectorCsv.Read(Path.Combine(root, "known_models.csv")))
            AsegurarModelo(row.GetValueOrDefault("brand", ""), row.GetValueOrDefault("model", ""));
        foreach (var row in LectorCsv.Read(Path.Combine(root, "model_repair_prices.csv")))
        {
            var brand = row.GetValueOrDefault("brand", ""); var model = row.GetValueOrDefault("model", "");
            var device = AsegurarModelo(brand, model);
            var repairCode = row.GetValueOrDefault("repair_code", "");
            if (!decimal.TryParse(row.GetValueOrDefault("price"), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                continue;

            var verifiedOn = row.GetValueOrDefault("verified_on", PrecioMercadoVersion);
            var existingPrice = db.PreciosReparacionModelo.FirstOrDefault(x => x.DeviceModelId == device.Id && x.RepairCode == repairCode);
            if (existingPrice is null)
                db.PreciosReparacionModelo.Add(new PrecioReparacionModelo(device.Id, repairCode, amount, "PEN", verifiedOn));
            else if (EsPrecioInicialAnterior(existingPrice.VerifiedOn))
                existingPrice.SetPrice(amount, "PEN", verifiedOn);
        }
        db.SaveChanges();
    }

    private ModeloDispositivo AsegurarModelo(string brand, string model)
    {
        var device = db.ModelosDispositivo.FirstOrDefault(x => x.Brand == brand && x.Model == model);
        if (device is not null) return device;
        device = new ModeloDispositivo(brand, model); db.ModelosDispositivo.Add(device); db.SaveChanges(); return device;
    }

    private static bool EsPrecioInicialAnterior(string verifiedOn)
    {
        if (string.Equals(verifiedOn, "SIMULADO", StringComparison.OrdinalIgnoreCase)) return true;
        return DateTime.TryParseExact(verifiedOn, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date) && date < new DateTime(2026, 11, 9);
    }
}




