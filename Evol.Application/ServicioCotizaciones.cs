using System.Globalization;
using Evol.Domain;

namespace Evol.Application;

public sealed class ServicioCotizaciones(IRepositorioCatalogoReparaciones catalog, IRepositorioCotizaciones quotes, ClasificadorSintomas classifier) : IServicioCotizaciones
{
    public ResultadoOperacion<RespuestaCotizacion> CrearCotizacion(SolicitudChat request)
    {
        var messages = request.Messages?.Where(message => string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(message.Content))
            .Select(message => message.Content.Trim()).Take(30).ToList() ?? [];
        if (messages.Count == 0) return ResultadoOperacion<RespuestaCotizacion>.Fail(422, "Envía al menos un mensaje del cliente.");
        if (messages.Any(message => message.Length > 2000)) return ResultadoOperacion<RespuestaCotizacion>.Fail(422, "Cada mensaje admite hasta 2000 caracteres.");

        var fullText = string.Join('\n', messages);
        var latestMessage = messages[^1];
        var issueCode = messages.AsEnumerable().Reverse().Select(classifier.ClasificarReparacion).FirstOrDefault(code => code is not null);
        var manualDiagnosis = issueCode is null
            ? messages.AsEnumerable().Reverse().Select(classifier.ObtenerDiagnosticoManual).FirstOrDefault(rule => rule is not null)
            : null;
        var deviceMessage = messages.AsEnumerable().Reverse().FirstOrDefault(message =>
            ClasificadorSintomas.ExtraerMarca(message) is not null || ClasificadorSintomas.ExtraerModelo(message) is not null);
        string? brand; string? model;
        if (deviceMessage is not null)
        {
            var parsedBrand = ClasificadorSintomas.ExtraerMarca(deviceMessage);
            var parsedModel = ClasificadorSintomas.ExtraerModelo(deviceMessage);
            brand = parsedBrand ?? request.Brand;
            model = parsedModel ?? (brand == request.Brand ? request.Model : null);
        }
        else
        {
            brand = ClasificadorSintomas.ExtraerMarca(fullText) ?? request.Brand;
            model = ClasificadorSintomas.ExtraerModelo(fullText) ?? request.Model;
        }

        var effectiveIssueCode = issueCode ?? manualDiagnosis?.IssueCode;
        var serviceName = catalog.BuscarNombreServicio(effectiveIssueCode);
        var registeredPrice = catalog.BuscarPrecioExacto(brand, model, effectiveIssueCode);
        var amount = registeredPrice?.Price;
        var isSimulated = registeredPrice?.IsSimulated ?? false;
        var faqAnswer = issueCode is null ? classifier.ObtenerRespuestaFrecuente(latestMessage) : null;
        var safetyAdvice = classifier.ObtenerConsejoSeguridad(fullText);
        var status = "needs_details";
        var issueName = serviceName;
        var minPrice = amount; var maxPrice = amount;
        string reply;
        if (faqAnswer is not null)
        {
            status = "information";
            var registeredBrands = catalog.ObtenerModelos().Select(item => item.Brand).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase);
            var registeredRepairs = catalog.ObtenerServicios().Select(item => item.Name).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase);
            reply = faqAnswer.Replace("{marcas}", string.Join(", ", registeredBrands), StringComparison.Ordinal)
                .Replace("{reparaciones}", string.Join(", ", registeredRepairs), StringComparison.Ordinal);
        }
        else if (manualDiagnosis is not null && (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(model)))
        {
            var missing = string.IsNullOrWhiteSpace(brand) && string.IsNullOrWhiteSpace(model) ? "la marca y el modelo exactos" : string.IsNullOrWhiteSpace(brand) ? "la marca" : "el modelo exacto";
            reply = $"Que no encienda no permite saber si la falla está en la placa. Para solicitar una revisión técnica, ¿me indicas {missing} de tu celular?";
        }
        else if (manualDiagnosis is not null)
        {
            status = "manual_review";
            issueName = manualDiagnosis.Question;
            var sampleText = amount is null ? "No hay un importe de muestra cargado para esta combinación." :
                $"El importe ficticio de muestra para este modelo es S/ {amount.Value.ToString("#,##0.00", CultureInfo.GetCultureInfo("es-PE"))}; no es una tarifa real.";
            reply = $"El equipo {brand} {model} requiere diagnóstico técnico porque no enciende. Este síntoma por sí solo no confirma una falla de placa. {sampleText} El técnico debe confirmar el costo real después de revisarlo. ¿Quieres solicitar una cita?";
        }
        else if (serviceName is null)
            reply = classifier.ObtenerPreguntaAclaratoria(latestMessage) ?? classifier.ObtenerPreguntaAclaratoria(fullText) ??
                (messages.Count > 1
                    ? "Aún no logro identificar la falla. ¿Puedes contarme qué parte del celular falla y qué sucede? Puedes elegir una opción o escribirlo con tus palabras."
                    : "¿Qué le ocurre a tu celular? Puedes elegir una opción o describirlo con tus palabras.");
        else if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(model))
        {
            var missing = string.IsNullOrWhiteSpace(brand) && string.IsNullOrWhiteSpace(model) ? "la marca y el modelo" : string.IsNullOrWhiteSpace(brand) ? "la marca" : "el modelo exacto";
            reply = $"Entiendo que necesitas revisar {serviceName.ToLowerInvariant()}. Para consultar un precio específico, ¿me indicas {missing} de tu teléfono?";
        }
        else if (issueCode is "liquid" or "board")
        {
            status = "manual_review";
            var sampleText = amount is null ? "No hay un importe de muestra cargado para esta combinación." :
                $"El importe ficticio de muestra es S/ {amount.Value.ToString("#,##0.00", CultureInfo.GetCultureInfo("es-PE"))}; no es una tarifa real.";
            reply = issueCode == "liquid"
                ? $"Si el equipo se mojó, apágalo y no lo conectes al cargador ni uses calor para secarlo. El caso requiere diagnóstico técnico. {sampleText} El taller confirmará el costo real después de revisar el equipo. ¿Quieres solicitar una cita?"
                : $"Este caso requiere diagnóstico técnico. {sampleText} El taller confirmará el costo real después de revisar el equipo. ¿Quieres solicitar una cita?";
        }
        else if (amount is null)
        {
            status = "manual_review";
            reply = $"Tengo registrado el equipo {brand} {model} y el tipo de reparación ({serviceName!.ToLowerInvariant()}), pero no hay un importe de muestra para esa combinación. ¿Quieres solicitar una cita para que el técnico lo revise?";
        }
        else
        {
            status = "estimate";
            issueName = serviceName.Replace(" (referencial)", "", StringComparison.OrdinalIgnoreCase);
            var formattedPrice = $"S/ {amount.Value.ToString("#,##0.00", CultureInfo.GetCultureInfo("es-PE"))}";
            reply = isSimulated
                ? $"El precio ficticio de muestra para {issueName.ToLowerInvariant()} del {brand} {model} es {formattedPrice}. Es solo para probar el sistema y no es una tarifa real. El costo final debe confirmarlo el técnico. ¿Quieres solicitar una cita?"
                : $"El precio registrado para {issueName.ToLowerInvariant()} del {brand} {model} es {formattedPrice}. La cotización final depende de la revisión y confirmación del técnico. ¿Quieres solicitar una cita con el taller?";
        }

        if (!string.IsNullOrWhiteSpace(safetyAdvice)) reply = $"{safetyAdvice} {reply}";

        var quoteId = quotes.GuardarCotizacion(DateTimeOffset.UtcNow, fullText, brand, model, effectiveIssueCode,
            status, minPrice, maxPrice, reply);
        return ResultadoOperacion<RespuestaCotizacion>.Ok(new RespuestaCotizacion(quoteId, status, reply, brand, model,
            effectiveIssueCode, issueName, minPrice, maxPrice,
            Disclaimer: isSimulated ? "Precio ficticio de muestra; no es una tarifa real." : "La cotización final requiere confirmación técnica.",
            IsSimulated: isSimulated));
    }
}




