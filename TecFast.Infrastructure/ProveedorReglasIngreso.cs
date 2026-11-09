using TecFast.Domain;

namespace TecFast.Infrastructure;

public static class ProveedorReglasIngreso
{
    public static ClasificadorSintomas CargarDesdeCsv(string path)
    {
        var rules = LectorCsv.Read(path).Select(row =>
        {
            var kind = row.GetValueOrDefault("kind", "");
            var question = row.GetValueOrDefault("question", "");
            var answer = row.GetValueOrDefault("answer", "");
            if (string.Equals(kind, "faq", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(answer))
            {
                answer = question;
                question = "";
            }

            return new ReglaSintoma(
                kind, EmptyToNull(row.GetValueOrDefault("issue_code")),
                int.TryParse(row.GetValueOrDefault("priority"), out var priority) ? priority : 0,
                row.GetValueOrDefault("phrases", "").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                question, row.GetValueOrDefault("examples", ""), answer,
                bool.TryParse(row.GetValueOrDefault("requires_manual_review"), out var requiresManualReview) && requiresManualReview,
                row.GetValueOrDefault("manual_review_reply", ""));
        }).ToList();
        return new ClasificadorSintomas(rules);
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}




