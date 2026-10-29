namespace Evol.Domain;

public sealed record ElementoServicio(string Code, string Name, string Notes);
public sealed record PrecioRegistrado(decimal Price, bool IsSimulated);
public sealed record ReglaSintoma(string Kind, string? IssueCode, int Priority, string[] Phrases,
    string Question, string Examples, string Answer = "");

public static class PoliticaCitas
{
    private static readonly HashSet<string> ActiveStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "scheduled", "confirmed", "arrived", "diagnosing", "awaiting_approval", "waiting_parts", "in_repair", "ready_for_pickup"
    };

    public static bool EstaActiva(string status) => ActiveStatuses.Contains(status);
    public static bool SeSuperponen(DateTimeOffset aStart, DateTimeOffset aEnd, DateTimeOffset bStart, DateTimeOffset bEnd)
        => aStart < bEnd && bStart < aEnd;
    public static DateTimeOffset ConvertirHoraLima(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(-5));
}




