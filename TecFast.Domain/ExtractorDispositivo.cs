using System.Text.RegularExpressions;

namespace TecFast.Domain;

public sealed record ReglaDispositivo(string Kind, string Brand, string Alias, string Pattern, string Prefix, string RemovePrefixPattern);

public sealed class ExtractorDispositivo(IReadOnlyList<ReglaDispositivo> rules)
{
    public string? ExtraerMarca(string text)
    {
        var normalized = NormalizadorTexto.Normalizar(text);
        foreach (var rule in rules.Where(rule => rule.Kind.Equals("brand", StringComparison.OrdinalIgnoreCase)))
            if ($" {normalized} ".Contains($" {NormalizadorTexto.Normalizar(rule.Alias)} ", StringComparison.Ordinal)) return rule.Brand;

        var model = ExtraerModelo(text);
        return rules.Where(rule => rule.Kind.Equals("model", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(rule => !string.IsNullOrWhiteSpace(rule.Prefix) && model?.StartsWith(rule.Prefix, StringComparison.OrdinalIgnoreCase) == true)?.Brand;
    }

    public string? ExtraerModelo(string text)
    {
        foreach (var rule in rules.Where(rule => rule.Kind.Equals("model", StringComparison.OrdinalIgnoreCase)))
        {
            var match = Regex.Match(text, rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success) continue;
            var model = Regex.Replace(match.Groups[1].Value.Trim(), @"\s+", " ");
            if (!string.IsNullOrWhiteSpace(rule.RemovePrefixPattern))
                model = Regex.Replace(model, rule.RemovePrefixPattern, $"{rule.Prefix.Trim()} ", RegexOptions.IgnoreCase);
            return string.IsNullOrWhiteSpace(rule.Prefix) ? model :
                model.StartsWith($"{rule.Prefix.Trim()} ", StringComparison.OrdinalIgnoreCase) ? model : $"{rule.Prefix.Trim()} {model}";
        }
        return null;
    }
}
