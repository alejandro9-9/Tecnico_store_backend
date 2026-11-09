using TecFast.Domain;

namespace TecFast.Application;

public sealed class ServicioCotizaciones(
    IRepositorioCatalogoReparaciones catalog,
    IRepositorioCotizaciones quotes,
    ClasificadorSintomas classifier,
    ResolutorDispositivo deviceResolver,
    IEnumerable<IEstrategiaRespuestaCotizacion> strategies) : IServicioCotizaciones
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
        var device = deviceResolver.Resolver(messages, request.Brand, request.Model);
        var effectiveIssueCode = issueCode ?? manualDiagnosis?.IssueCode;
        var service = catalog.BuscarServicio(effectiveIssueCode);
        var registeredPrice = catalog.BuscarPrecioExacto(device.Brand, device.Model, effectiveIssueCode);
        var faqAnswer = issueCode is null ? classifier.ObtenerRespuestaFrecuente(latestMessage) : null;
        var registeredBrands = faqAnswer is null ? [] : catalog.ObtenerModelos().Select(item => item.Brand)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList();
        var registeredRepairs = faqAnswer is null ? [] : catalog.ObtenerServicios().Select(item => item.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList();
        var context = new ContextoCotizacion(
            messages,
            fullText,
            latestMessage,
            device.Brand,
            device.Model,
            effectiveIssueCode,
            service,
            manualDiagnosis,
            classifier.ObtenerReglaReparacion(effectiveIssueCode),
            registeredPrice,
            faqAnswer,
            classifier.ObtenerPreguntaAclaratoria(latestMessage) ?? classifier.ObtenerPreguntaAclaratoria(fullText),
            classifier.ObtenerConsejoSeguridad(fullText),
            registeredBrands,
            registeredRepairs);

        var strategy = strategies.FirstOrDefault(item => item.Aplica(context));
        if (strategy is null) return ResultadoOperacion<RespuestaCotizacion>.Fail(500, "No se encontró una estrategia de cotización.");
        var response = strategy.Construir(context);
        var reply = string.IsNullOrWhiteSpace(context.SafetyAdvice) ? response.Reply : $"{context.SafetyAdvice} {response.Reply}";
        var quoteId = quotes.GuardarCotizacion(DateTimeOffset.UtcNow, fullText, context.Brand, context.Model, context.IssueCode,
            response.Status, context.Amount, context.Amount, reply);
        return ResultadoOperacion<RespuestaCotizacion>.Ok(new RespuestaCotizacion(quoteId, response.Status, reply, context.Brand, context.Model,
            context.IssueCode, response.IssueName ?? service?.Name, context.Amount, context.Amount,
            Disclaimer: context.IsSimulated ? "Precio ficticio de muestra; no es una tarifa real." : "Precio referencial de mercado; requiere confirmación técnica.",
            IsSimulated: context.IsSimulated));
    }
}
