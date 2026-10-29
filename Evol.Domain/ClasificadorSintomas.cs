using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Evol.Domain;

public sealed class ClasificadorSintomas(IEnumerable<ReglaSintoma> rules)
{
    private readonly List<ReglaSintoma> _rules = rules.OrderByDescending(rule => rule.Priority).ToList();

    public string? ClasificarReparacion(string text) => _rules.FirstOrDefault(rule => rule.Kind == "repair" &&
        rule.Phrases.Any(phrase => ContainsPhrase(text, phrase)))?.IssueCode;

    public ReglaSintoma? ObtenerDiagnosticoManual(string text) => _rules.FirstOrDefault(rule => rule.Kind == "manual_review" &&
        rule.Phrases.Any(phrase => ContainsPhrase(text, phrase)));

    public string? ObtenerPreguntaAclaratoria(string text) => _rules.FirstOrDefault(rule => rule.Kind == "clarification" &&
        rule.Phrases.Any(phrase => ContainsPhrase(text, phrase)))?.Question;

    public string? ObtenerRespuestaFrecuente(string text) => _rules.FirstOrDefault(rule => rule.Kind == "faq" &&
        rule.Phrases.Any(phrase => ContainsPhrase(text, phrase)))?.Answer;

    public string? ObtenerConsejoSeguridad(string text) => _rules.FirstOrDefault(rule => rule.Kind == "advice" &&
        rule.Phrases.Any(phrase => ContainsPhrase(text, phrase)))?.Answer;

    public IReadOnlyList<object> ObtenerReglasPublicas() => _rules.Where(rule => rule.Kind == "repair")
        .Select(rule => (object)new { code = rule.IssueCode, examples = rule.Examples.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), question = rule.Question })
        .ToList();

    public static string? ExtraerMarca(string text)
    {
        var normalized = Normalizar(text);
        var aliases = new (string[] Names, string Brand)[]
        {
            (new[] { "apple", "iphone" }, "Apple"), (new[] { "samsung", "galaxy" }, "Samsung"),
            (new[] { "xiaomi", "redmi", "poco", "mi phone" }, "Xiaomi"), (new[] { "motorola", "moto" }, "Motorola"),
            (new[] { "huawei" }, "Huawei"), (new[] { "honor" }, "Honor"), (new[] { "oppo" }, "Oppo"), (new[] { "vivo" }, "Vivo"),
            (new[] { "realme" }, "Realme"), (new[] { "google pixel", "pixel" }, "Google")
        };
        foreach (var (names, brand) in aliases)
            if (names.Any(name => $" {normalized} ".Contains($" {Normalizar(name)} ", StringComparison.Ordinal))) return brand;
        var model = ExtraerModelo(text);
        if (model?.StartsWith("Galaxy ", StringComparison.OrdinalIgnoreCase) == true) return "Samsung";
        if (model?.StartsWith("iPhone ", StringComparison.OrdinalIgnoreCase) == true) return "Apple";
        return null;
    }

    public static string? ExtraerModelo(string text)
    {
        var patterns = new[]
        {
            @"\b(i\s?phone\s?(?:1[0-9]|[0-9])(?:\s?(?:pro\s?max|pro|plus|mini))?)\b",
            @"\b((?:galaxy\s*)?(?:s|a|z|m)\s?\d{2,3}(?:\s?(?:ultra|plus|\+|fe|5g))?)\b",
            @"\b((?:galaxy\s*)?(?:flip|fold)\s?\d(?:\s?fe)?)\b",
            @"\b(redmi\s?(?:note\s?)?\d{1,2}(?:\s?pro(?:\s?\+)?|\s?\+)?(?:\s?5g)?)\b",
            @"\b(poco\s?[a-z]?\s?\d{1,2}(?:\s?pro|\s?x\d)?)\b",
            @"\b(moto\s?[a-z]?\d{1,2}(?:\s?power|\s?play|\s?plus)?)\b",
            @"\b(pixel\s?\d{1,2}(?:\s?(?:pro|a|xl))?)\b"
        };
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success) continue;
            var model = Regex.Replace(match.Groups[1].Value.Trim(), @"\s+", " ");
            if (Regex.IsMatch(model, @"^(?:s|a|z|m)\s?\d", RegexOptions.IgnoreCase)) model = $"Galaxy {model}";
            else if (Regex.IsMatch(model, @"^(?:flip|fold)\s?\d", RegexOptions.IgnoreCase)) model = $"Galaxy {model}";
            else if (Regex.IsMatch(model, @"^i\s?phone", RegexOptions.IgnoreCase)) model = Regex.Replace(model, @"^i\s?phone\s*", "iPhone ", RegexOptions.IgnoreCase);
            return model;
        }
        return null;
    }

    public static string Normalizar(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var plain = new string(decomposed.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(plain, @"[^a-z0-9]+", " ").Trim();
    }

    public static string NormalizarModelo(string value) => Regex.Replace(Normalizar(value), @"\s+", " ").Trim();

    private static bool ContainsPhrase(string text, string phrase)
    {
        var normalizedText = $" {Normalizar(text)} ";
        var segments = phrase.Split('*', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalizar).Where(segment => segment.Length > 0).ToArray();
        if (segments.Length == 0) return false;
        var searchFrom = 0;
        foreach (var segment in segments)
        {
            var index = normalizedText.IndexOf($" {segment} ", searchFrom, StringComparison.Ordinal);
            if (index < 0) return false;
            searchFrom = index + segment.Length + 1;
        }
        return true;
    }
}




