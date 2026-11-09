using System.Security.Cryptography;
using System.Text;

namespace TecFast.Api;

internal sealed class FiltroToken(string headerName, string configurationKey, string fallback) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var provided = request.Headers[headerName].FirstOrDefault();
        var expected = configuration[configurationKey] ?? fallback;
        if (!Coincide(provided, expected)) return ValueTask.FromResult<object?>(Results.Unauthorized());
        return next(context);
    }

    private static bool Coincide(string? provided, string expected)
    {
        if (string.IsNullOrEmpty(provided)) return false;
        var left = Encoding.UTF8.GetBytes(provided); var right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
