using TecFast.Domain;

namespace TecFast.Application;

public sealed record DispositivoResuelto(string? Brand, string? Model);

public sealed class ResolutorDispositivo(ExtractorDispositivo extractor)
{
    public DispositivoResuelto Resolver(IReadOnlyList<string> messages, string? fallbackBrand, string? fallbackModel)
    {
        var fullText = string.Join('\n', messages);
        var deviceMessage = messages.AsEnumerable().Reverse().FirstOrDefault(message =>
            extractor.ExtraerMarca(message) is not null || extractor.ExtraerModelo(message) is not null);

        if (deviceMessage is null)
            return new(extractor.ExtraerMarca(fullText) ?? fallbackBrand, extractor.ExtraerModelo(fullText) ?? fallbackModel);

        var brand = extractor.ExtraerMarca(deviceMessage) ?? fallbackBrand;
        var model = extractor.ExtraerModelo(deviceMessage) ??
            (string.Equals(brand, fallbackBrand, StringComparison.OrdinalIgnoreCase) ? fallbackModel : null);
        return new(brand, model);
    }
}
