using System.Globalization;
using TecFast.Domain.Entidades;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TecFast.Infrastructure.Persistencia;

public sealed class InicializadorBaseDatos(ContextoTecFast db, string databaseDirectory)
{
    private const string PrecioMercadoVersion = "MERCADO-2026-11-09";
    private readonly string _databaseDirectory = databaseDirectory;

    public void Inicializar()
    {
        db.Database.EnsureCreated();
        CargarCatalogoInicial();
        ImportarPreciosModelosAnteriores();
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

    // The old integer-keyed database stays untouched. Bring its model prices into the new Guid schema once.
    private void ImportarPreciosModelosAnteriores()
    {
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS migration_markers (name TEXT PRIMARY KEY)");
        if (db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM migration_markers WHERE name='legacy-model-prices'").Single() > 0) return;
        var legacyPath = Path.Combine(_databaseDirectory, "tecfast-net.db");
        if (!File.Exists(legacyPath)) return;
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = legacyPath, Mode = SqliteOpenMode.ReadOnly }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT d.brand,d.model,p.repair_code,p.price,p.currency,p.verified_on FROM model_repair_prices p JOIN device_models d ON d.id=p.device_model_id";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var brand = reader.GetString(0); var model = reader.GetString(1); var code = reader.GetString(2);
            var device = AsegurarModelo(brand, model);
            var amount = Convert.ToDecimal(reader.GetValue(3), CultureInfo.InvariantCulture);
            var price = db.PreciosReparacionModelo.FirstOrDefault(x => x.DeviceModelId == device.Id && x.RepairCode == code);
            var currency = reader.GetString(4); var verified = reader.GetString(5);
            if (price is null) db.PreciosReparacionModelo.Add(new PrecioReparacionModelo(device.Id, code, amount, currency, verified));
        }
        reader.Close(); db.SaveChanges();
        db.Database.ExecuteSqlInterpolated($"INSERT OR IGNORE INTO migration_markers(name) VALUES ({"legacy-model-prices"})");
    }

    private static bool EsPrecioInicialAnterior(string verifiedOn)
    {
        if (string.Equals(verifiedOn, "SIMULADO", StringComparison.OrdinalIgnoreCase)) return true;
        return DateTime.TryParseExact(verifiedOn, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date) && date < new DateTime(2026, 11, 9);
    }
}




