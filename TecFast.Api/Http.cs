using TecFast.Application;

namespace TecFast.Api;

internal static class Http
{
    public static IResult Convertir<T>(ResultadoOperacion<T> result, Func<T, string>? location = null)
    {
        if (!result.Succeeded) return Results.Json(new { detail = result.Error }, statusCode: result.StatusCode);
        if (result.StatusCode == StatusCodes.Status201Created)
            return Results.Created(location?.Invoke(result.Value!) ?? "", result.Value);
        return Results.Json(result.Value, statusCode: result.StatusCode);
    }
}
