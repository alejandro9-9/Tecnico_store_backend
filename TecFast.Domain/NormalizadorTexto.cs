using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TecFast.Domain;

public static class NormalizadorTexto
{
    public static string Normalizar(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var plain = new string(decomposed.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(plain, @"[^a-z0-9]+", " ").Trim();
    }

    public static string NormalizarModelo(string value) => Regex.Replace(Normalizar(value), @"\s+", " ").Trim();

    public static bool ContieneFrase(string text, string phrase)
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
