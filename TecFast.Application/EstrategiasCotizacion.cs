using System.Globalization;
using TecFast.Domain;

namespace TecFast.Application;

public sealed record ContextoCotizacion(
    IReadOnlyList<string> Messages,
    string FullText,
    string LatestMessage,
    string? Brand,
    string? Model,
    string? IssueCode,
    ElementoServicio? Service,
    ReglaSintoma? ManualDiagnosis,
    ReglaSintoma? RepairRule,
    PrecioRegistrado? RegisteredPrice,
    string? FaqAnswer,
    string? ClarifyingQuestion,
    string? SafetyAdvice,
    IReadOnlyList<string> RegisteredBrands,
    IReadOnlyList<string> RegisteredRepairs)
{
    public bool MissingDevice => string.IsNullOrWhiteSpace(Brand) || string.IsNullOrWhiteSpace(Model);
    public decimal? Amount => RegisteredPrice?.Price;
    public bool IsSimulated => RegisteredPrice?.IsSimulated ?? false;
}

public sealed record RespuestaEstrategiaCotizacion(EstadoCotizacion Status, string Reply, string? IssueName = null);

public interface IEstrategiaRespuestaCotizacion
{
    bool Aplica(ContextoCotizacion context);
    RespuestaEstrategiaCotizacion Construir(ContextoCotizacion context);
}

public sealed class EstrategiaPreguntaFrecuente : IEstrategiaRespuestaCotizacion
{
    public bool Aplica(ContextoCotizacion context) => context.FaqAnswer is not null;

    public RespuestaEstrategiaCotizacion Construir(ContextoCotizacion context)
    {
        var reply = context.FaqAnswer!
            .Replace("{marcas}", string.Join(", ", context.RegisteredBrands), StringComparison.Ordinal)
            .Replace("{reparaciones}", string.Join(", ", context.RegisteredRepairs), StringComparison.Ordinal);
        return new(EstadoCotizacion.Information, reply);
    }
}

public sealed class EstrategiaDiagnosticoManualSinDispositivo : IEstrategiaRespuestaCotizacion
{
    public bool Aplica(ContextoCotizacion context) => context.ManualDiagnosis is not null && context.MissingDevice;

    public RespuestaEstrategiaCotizacion Construir(ContextoCotizacion context)
    {
        var missing = string.IsNullOrWhiteSpace(context.Brand) && string.IsNullOrWhiteSpace(context.Model)
            ? "la marca y el modelo exactos" : string.IsNullOrWhiteSpace(context.Brand) ? "la marca" : "el modelo exacto";
        return new(EstadoCotizacion.NeedsDetails,
            $"Que no encienda no permite saber si la falla está en la placa. Para solicitar una revisión técnica, ¿me indicas {missing} de tu celular?");
    }
}

public sealed class EstrategiaDiagnosticoManual : IEstrategiaRespuestaCotizacion
{
    public bool Aplica(ContextoCotizacion context) => context.ManualDiagnosis is not null;

    public RespuestaEstrategiaCotizacion Construir(ContextoCotizacion context)
    {
        var sampleText = context.Amount is null ? "No hay un precio referencial cargado para esta combinación." :
            $"El precio referencial de mercado para este modelo es S/ {context.Amount.Value.ToString("#,##0.00", CultureInfo.GetCultureInfo("es-PE"))}; no es una tarifa final.";
        var reply = $"El equipo {context.Brand} {context.Model} requiere diagnóstico técnico porque no enciende. Este síntoma por sí solo no confirma una falla de placa. {sampleText} El técnico debe confirmar el costo final después de revisarlo. ¿Quieres solicitar una cita?";
        return new(EstadoCotizacion.ManualReview, reply, context.ManualDiagnosis!.Question);
    }
}

public sealed class EstrategiaFallaNoIdentificada : IEstrategiaRespuestaCotizacion
{
    public bool Aplica(ContextoCotizacion context) => context.Service is null;

    public RespuestaEstrategiaCotizacion Construir(ContextoCotizacion context)
    {
        var reply = context.ClarifyingQuestion ??
            (context.Messages.Count > 1
                ? "Aún no logro identificar la falla. ¿Puedes contarme qué parte del celular falla y qué sucede? Puedes elegir una opción o escribirlo con tus palabras."
                : "¿Qué le ocurre a tu celular? Puedes elegir una opción o describirlo con tus palabras.");
        return new(EstadoCotizacion.NeedsDetails, reply);
    }
}

public sealed class EstrategiaDispositivoIncompleto : IEstrategiaRespuestaCotizacion
{
    public bool Aplica(ContextoCotizacion context) => context.Service is not null && context.MissingDevice;

    public RespuestaEstrategiaCotizacion Construir(ContextoCotizacion context)
    {
        var missing = string.IsNullOrWhiteSpace(context.Brand) && string.IsNullOrWhiteSpace(context.Model)
            ? "la marca y el modelo" : string.IsNullOrWhiteSpace(context.Brand) ? "la marca" : "el modelo exacto";
        return new(EstadoCotizacion.NeedsDetails,
            $"Entiendo que necesitas revisar {context.Service!.Name.ToLowerInvariant()}. Para consultar un precio específico, ¿me indicas {missing} de tu teléfono?");
    }
}

public sealed class EstrategiaRevisionManualPorRegla : IEstrategiaRespuestaCotizacion
{
    public bool Aplica(ContextoCotizacion context) => context.RepairRule?.RequiresManualReview == true;

    public RespuestaEstrategiaCotizacion Construir(ContextoCotizacion context)
    {
        var sampleText = context.Amount is null ? "No hay un precio referencial cargado para esta combinación." :
            $"El precio referencial de mercado es S/ {context.Amount.Value.ToString("#,##0.00", CultureInfo.GetCultureInfo("es-PE"))}; no es una tarifa final.";
        var reply = string.IsNullOrWhiteSpace(context.RepairRule!.ManualReviewReply)
            ? $"Este caso requiere diagnóstico técnico. {sampleText} El taller confirmará el costo real después de revisar el equipo. ¿Quieres solicitar una cita?"
            : string.Format(CultureInfo.InvariantCulture, context.RepairRule.ManualReviewReply, context.Brand, context.Model, sampleText);
        return new(EstadoCotizacion.ManualReview, reply);
    }
}

public sealed class EstrategiaSinPrecio : IEstrategiaRespuestaCotizacion
{
    public bool Aplica(ContextoCotizacion context) => context.Service is not null && context.Amount is null;

    public RespuestaEstrategiaCotizacion Construir(ContextoCotizacion context) => new(
        EstadoCotizacion.ManualReview,
        $"Tengo registrado el equipo {context.Brand} {context.Model} y el tipo de reparación ({context.Service!.Name.ToLowerInvariant()}), pero no hay un precio referencial para esa combinación. ¿Quieres solicitar una cita para que el técnico lo revise?");
}

public sealed class EstrategiaEstimacion : IEstrategiaRespuestaCotizacion
{
    public bool Aplica(ContextoCotizacion context) => context.Amount is not null;

    public RespuestaEstrategiaCotizacion Construir(ContextoCotizacion context)
    {
        var issueName = context.Service!.Name.Replace(" (referencial)", "", StringComparison.OrdinalIgnoreCase);
        var formattedPrice = $"S/ {context.Amount!.Value.ToString("#,##0.00", CultureInfo.GetCultureInfo("es-PE"))}";
        var reply = context.IsSimulated
            ? $"El precio ficticio de muestra para {issueName.ToLowerInvariant()} del {context.Brand} {context.Model} es {formattedPrice}. Es solo para probar el sistema y no es una tarifa real. El costo final debe confirmarlo el técnico. ¿Quieres solicitar una cita?"
            : $"El precio referencial de mercado para {issueName.ToLowerInvariant()} del {context.Brand} {context.Model} es {formattedPrice}. Puede variar según la calidad del repuesto, disponibilidad y resultado del diagnóstico. ¿Quieres solicitar una cita con el taller?";
        return new(EstadoCotizacion.Estimate, reply, issueName);
    }
}
