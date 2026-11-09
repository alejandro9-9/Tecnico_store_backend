using TecFast.Domain;

namespace TecFast.Infrastructure;

public static class ProveedorDispositivos
{
    public static IReadOnlyList<ReglaDispositivo> CargarDesdeCsv(string path) => LectorCsv.Read(path)
        .Select(row => new ReglaDispositivo(
            row.GetValueOrDefault("kind", ""),
            row.GetValueOrDefault("brand", ""),
            row.GetValueOrDefault("alias", ""),
            row.GetValueOrDefault("pattern", ""),
            row.GetValueOrDefault("prefix", ""),
            row.GetValueOrDefault("remove_prefix_pattern", "")))
        .Where(rule => !string.IsNullOrWhiteSpace(rule.Kind))
        .ToList();
}
