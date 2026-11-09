using System.Text.RegularExpressions;

namespace TecFast.Domain;

public enum EstadoCotizacion
{
    NeedsDetails,
    Information,
    ManualReview,
    Estimate
}

public enum EstadoCita
{
    Requested,
    Confirmed,
    Cancelled,
    Scheduled,
    Arrived,
    Diagnosing,
    AwaitingApproval,
    WaitingParts,
    InRepair,
    ReadyForPickup,
    Completed
}

public static class Estados
{
    public static string ToSnakeCase<TEnum>(TEnum value) where TEnum : struct, Enum =>
        Regex.Replace(value.ToString(), "(?<!^)([A-Z])", "_$1").ToLowerInvariant();

    public static TEnum Parse<TEnum>(string value) where TEnum : struct, Enum
    {
        var parsed = Enum.GetValues<TEnum>().FirstOrDefault(item =>
            string.Equals(ToSnakeCase(item), value, StringComparison.OrdinalIgnoreCase));
        return parsed;
    }

    public static bool TryParseCita(string? value, out EstadoCita status)
    {
        var match = Enum.GetValues<EstadoCita>().FirstOrDefault(item =>
            string.Equals(ToSnakeCase(item), value?.Trim(), StringComparison.OrdinalIgnoreCase));
        status = match;
        return value is not null && Enum.GetValues<EstadoCita>().Any(item =>
            string.Equals(ToSnakeCase(item), value.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
