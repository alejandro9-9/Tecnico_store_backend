using System.Globalization;
using Evol.Domain.Entidades;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Evol.Infrastructure.Persistencia;

public sealed class InicializadorBaseDatos(ContextoEvol db, string databaseDirectory)
{
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
                db.ServiciosReparacion.Add(new ServicioReparacion(code, row.GetValueOrDefault("name", ""), row.GetValueOrDefault("notes", "")));
        }
        foreach (var row in LectorCsv.Read(Path.Combine(root, "known_models.csv")))
            AsegurarModelo(row.GetValueOrDefault("brand", ""), row.GetValueOrDefault("model", ""));
        foreach (var row in LectorCsv.Read(Path.Combine(root, "model_repair_prices.csv")))
        {
            var brand = row.GetValueOrDefault("brand", ""); var model = row.GetValueOrDefault("model", "");
            var device = AsegurarModelo(brand, model);
            var repairCode = row.GetValueOrDefault("repair_code", "");
            if (!db.PreciosReparacionModelo.Any(x => x.DeviceModelId == device.Id && x.RepairCode == repairCode) &&
                decimal.TryParse(row.GetValueOrDefault("price"), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                db.PreciosReparacionModelo.Add(new PrecioReparacionModelo(device.Id, repairCode, amount, "PEN",
                    row.GetValueOrDefault("verified_on", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
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
        var legacyPath = Path.Combine(_databaseDirectory, "evol-net.db");
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
            else price.SetPrice(amount, currency, verified);
        }
        reader.Close(); db.SaveChanges();
        db.Database.ExecuteSqlInterpolated($"INSERT OR IGNORE INTO migration_markers(name) VALUES ({"legacy-model-prices"})");
    }
}




