using Evol.Domain;

namespace Evol.Application;

public sealed class ServicioCitas(
    IRepositorioCitas appointments,
    IRepositorioCotizaciones quotes,
    IRepositorioCatalogoReparaciones catalog,
    IRepositorioBloqueosHorario blocks) : IServicioCitas
{
    private static readonly TimeSpan LimaOffset = TimeSpan.FromHours(-5);

    public ResultadoOperacion<RespuestaCita> Crear(NuevaCita request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Trim().Length < 2 || request.CustomerName.Length > 120)
            return ResultadoOperacion<RespuestaCita>.Fail(422, "Indica el nombre del cliente.");
        if (string.IsNullOrWhiteSpace(request.CustomerPhone) || request.CustomerPhone.Length < 7 || request.CustomerPhone.Length > 30)
            return ResultadoOperacion<RespuestaCita>.Fail(422, "Indica un número de celular válido.");
        if (string.IsNullOrWhiteSpace(request.Brand) || string.IsNullOrWhiteSpace(request.Model) || string.IsNullOrWhiteSpace(request.Issue))
            return ResultadoOperacion<RespuestaCita>.Fail(422, "Completa el teléfono y la reparación antes de solicitar la cita.");

        var scheduled = PoliticaCitas.ConvertirHoraLima(request.ScheduledAt);
        if (scheduled <= PoliticaCitas.ConvertirHoraLima(DateTimeOffset.UtcNow))
            return ResultadoOperacion<RespuestaCita>.Fail(422, "La fecha y hora de la cita deben ser futuras.");
        var brand = request.Brand.Trim(); var model = request.Model.Trim(); var issue = request.Issue.Trim();
        if (request.QuoteId is Guid quoteId)
        {
            var quote = quotes.ObtenerCotizacionParaCita(quoteId);
            if (quote is null) return ResultadoOperacion<RespuestaCita>.Fail(404, "No se encontró la cotización asociada.");
            if (quote.Status is not ("estimate" or "manual_review"))
                return ResultadoOperacion<RespuestaCita>.Fail(409, "Completa primero la información del teléfono y la reparación antes de solicitar una cita.");
            var quoteService = catalog.BuscarNombreServicio(quote.IssueCode);
            if (quote.Brand is null || quote.Model is null || quote.IssueCode is null || quoteService is null)
                return ResultadoOperacion<RespuestaCita>.Fail(409, "La cotización no tiene un teléfono y una reparación identificados. Vuelve al chat para completar esos datos.");
            brand = quote.Brand; model = quote.Model; issue = quoteService;
        }

        if (blocks.SeSuperpone(scheduled, scheduled.AddHours(1)))
            return ResultadoOperacion<RespuestaCita>.Fail(409, "Ese horario está bloqueado por el taller. Elige otra fecha u hora.");
        var created = appointments.Crear(request, brand, model, issue, scheduled);
        return ResultadoOperacion<RespuestaCita>.Created(created);
    }

    public IReadOnlyList<RespuestaCita> ObtenerParaTecnico() => appointments.ObtenerCitas(includePrice: false);

    public ResultadoOperacion<RespuestaCita> DecidirDisponibilidad(Guid id, string status)
    {
        if (status is not ("confirmed" or "cancelled"))
            return ResultadoOperacion<RespuestaCita>.Fail(422, "El técnico solo puede confirmar o rechazar la disponibilidad.");
        var appointment = appointments.ObtenerCita(id, includePrice: false);
        if (appointment is null) return ResultadoOperacion<RespuestaCita>.Fail(404, "No se encontró la cita.");
        if (appointment.Status != "requested")
            return ResultadoOperacion<RespuestaCita>.Fail(409, "Solo se puede decidir la disponibilidad de solicitudes pendientes.");
        if (status == "confirmed")
        {
            var start = PoliticaCitas.ConvertirHoraLima(appointment.ScheduledAt); var end = start.AddHours(1);
            if (blocks.SeSuperpone(start, end)) return ResultadoOperacion<RespuestaCita>.Fail(409, "Ese horario está bloqueado por el taller. Elige otra fecha u hora.");
            if (appointments.ExisteConflictoActivo(start, end, id))
                return ResultadoOperacion<RespuestaCita>.Fail(409, "Ese horario ya está ocupado por otra cita activa. Elige otra fecha u hora.");
        }
        if (!appointments.ActualizarEstadoPendiente(id, status))
            return ResultadoOperacion<RespuestaCita>.Fail(409, "Solo se puede decidir la disponibilidad de solicitudes pendientes.");
        return ResultadoOperacion<RespuestaCita>.Ok(appointments.ObtenerCita(id, includePrice: false)!);
    }
}




