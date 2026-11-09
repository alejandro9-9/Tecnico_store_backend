namespace TecFast.Domain;

public sealed class ClasificadorSintomas(IEnumerable<ReglaSintoma> rules)
{
    private readonly List<ReglaSintoma> _rules = rules.OrderByDescending(rule => rule.Priority).ToList();

    public string? ClasificarReparacion(string text) => _rules.FirstOrDefault(rule => rule.Kind == "repair" &&
        rule.Phrases.Any(phrase => NormalizadorTexto.ContieneFrase(text, phrase)))?.IssueCode;

    public ReglaSintoma? ObtenerDiagnosticoManual(string text) => _rules.FirstOrDefault(rule => rule.Kind == "manual_review" &&
        rule.Phrases.Any(phrase => NormalizadorTexto.ContieneFrase(text, phrase)));

    public string? ObtenerPreguntaAclaratoria(string text) => _rules.FirstOrDefault(rule => rule.Kind == "clarification" &&
        rule.Phrases.Any(phrase => NormalizadorTexto.ContieneFrase(text, phrase)))?.Question;

    public string? ObtenerRespuestaFrecuente(string text) => _rules.FirstOrDefault(rule => rule.Kind == "faq" &&
        rule.Phrases.Any(phrase => NormalizadorTexto.ContieneFrase(text, phrase)))?.Answer;

    public string? ObtenerConsejoSeguridad(string text) => _rules.FirstOrDefault(rule => rule.Kind == "advice" &&
        rule.Phrases.Any(phrase => NormalizadorTexto.ContieneFrase(text, phrase)))?.Answer;

    public bool RequiereRevisionManual(string? issueCode) => !string.IsNullOrWhiteSpace(issueCode) &&
        _rules.Any(rule => rule.Kind == "repair" && string.Equals(rule.IssueCode, issueCode, StringComparison.OrdinalIgnoreCase) && rule.RequiresManualReview);

    public ReglaSintoma? ObtenerReglaReparacion(string? issueCode) => _rules.FirstOrDefault(rule =>
        rule.Kind == "repair" && string.Equals(rule.IssueCode, issueCode, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<object> ObtenerReglasPublicas() => _rules.Where(rule => rule.Kind == "repair")
        .Select(rule => (object)new { code = rule.IssueCode, examples = rule.Examples.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), question = rule.Question })
        .ToList();

}




