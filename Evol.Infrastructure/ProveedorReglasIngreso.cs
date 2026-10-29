using Evol.Domain;

namespace Evol.Infrastructure;

public static class ProveedorReglasIngreso
{
    public static ClasificadorSintomas CargarDesdeCsv(string path)
    {
        var rules = LectorCsv.Read(path).Select(row => new ReglaSintoma(
            row.GetValueOrDefault("kind", ""), EmptyToNull(row.GetValueOrDefault("issue_code")),
            int.TryParse(row.GetValueOrDefault("priority"), out var priority) ? priority : 0,
            row.GetValueOrDefault("phrases", "").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            row.GetValueOrDefault("question", ""), row.GetValueOrDefault("examples", ""),
            row.GetValueOrDefault("answer", ""))).ToList();
        return new ClasificadorSintomas(rules);
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}




