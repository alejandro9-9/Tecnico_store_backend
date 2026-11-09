namespace TecFast.Domain;

public sealed record ElementoServicio(string Code, string Name, string Notes);
public sealed record PrecioRegistrado(decimal Price, bool IsSimulated);
public sealed record ReglaSintoma(string Kind, string? IssueCode, int Priority, string[] Phrases,
    string Question, string Examples, string Answer = "", bool RequiresManualReview = false, string ManualReviewReply = "");

public static class PoliticaCitas
{
    private static readonly HashSet<EstadoCita> ActiveStatuses = new()
    {
        EstadoCita.Scheduled, EstadoCita.Confirmed, EstadoCita.Arrived, EstadoCita.Diagnosing,
        EstadoCita.AwaitingApproval, EstadoCita.WaitingParts, EstadoCita.InRepair, EstadoCita.ReadyForPickup
    };

    public static bool EstaActiva(EstadoCita status) => ActiveStatuses.Contains(status);
    public static bool SeSuperponen(DateTimeOffset aStart, DateTimeOffset aEnd, DateTimeOffset bStart, DateTimeOffset bEnd)
        => aStart < bEnd && bStart < aEnd;
    public static DateTimeOffset ConvertirHoraLima(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(-5));
}




