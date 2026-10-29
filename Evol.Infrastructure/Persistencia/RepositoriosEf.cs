using System.Globalization;
using Evol.Application;
using Evol.Application.Abstracciones.Repositorios;
using Evol.Domain;
using Evol.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Evol.Infrastructure.Persistencia;

public sealed class RepositorioCatalogoEf(ContextoEvol db) : RepositorioBaseEf<ServicioReparacion>(db), IRepositorioCatalogoReparaciones
{
    public IReadOnlyList<ElementoServicio> ObtenerServicios() => Db.ServiciosReparacion.AsNoTracking().OrderBy(x => x.Name)
        .Select(x => new ElementoServicio(x.Code, x.Name, x.Notes)).ToList();

    public IReadOnlyList<RespuestaModeloCatalogo> ObtenerModelos() => Db.ModelosDispositivo.AsNoTracking().Include(x => x.RepairPrices)
        .OrderBy(x => x.Brand).ThenBy(x => x.Model).ToList()
        .Select(x => new RespuestaModeloCatalogo(x.Brand, x.Model,
            x.RepairPrices.ToDictionary(p => p.RepairCode, p => p.Price, StringComparer.OrdinalIgnoreCase))).ToList();

    public IReadOnlyList<RespuestaPrecioReparacionModeloAdmin> ObtenerPreciosModelo() =>
        (from price in Db.PreciosReparacionModelo.AsNoTracking()
         join device in Db.ModelosDispositivo.AsNoTracking() on price.DeviceModelId equals device.Id
         join service in Db.ServiciosReparacion.AsNoTracking() on price.RepairCode equals service.Code
         orderby device.Brand, device.Model, service.Name
         select new RespuestaPrecioReparacionModeloAdmin(device.Brand, device.Model, price.RepairCode,
             service.Name, price.Price, price.Currency, price.VerifiedOn)).ToList();

    public string? BuscarNombreServicio(string? code) => string.IsNullOrWhiteSpace(code) ? null :
        Db.ServiciosReparacion.AsNoTracking().Where(x => x.Code == code).Select(x => x.Name).FirstOrDefault();

    public PrecioRegistrado? BuscarPrecioExacto(string? brand, string? model, string? repairCode)
    {
        if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(repairCode)) return null;
        var normalizedBrand = ClasificadorSintomas.Normalizar(brand);
        var normalizedModel = ClasificadorSintomas.NormalizarModelo(model);
        return (from price in Db.PreciosReparacionModelo.AsNoTracking()
                join device in Db.ModelosDispositivo.AsNoTracking() on price.DeviceModelId equals device.Id
                where price.RepairCode == repairCode
                select new { device.Brand, device.Model, price.Price, price.VerifiedOn })
            .AsEnumerable()
            .Where(row => ClasificadorSintomas.Normalizar(row.Brand) == normalizedBrand &&
                          ClasificadorSintomas.NormalizarModelo(row.Model) == normalizedModel)
            .Select(row => (PrecioRegistrado?)new PrecioRegistrado(row.Price,
                string.Equals(row.VerifiedOn, "SIMULADO", StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
    }

    public ResultadoOperacion<RespuestaPrecioReparacionModeloAdmin> GuardarPrecioModelo(ActualizarPrecioReparacionModelo request, string verifiedOn)
    {
        var service = Db.ServiciosReparacion.AsNoTracking().FirstOrDefault(x => x.Code == request.RepairCode);
        if (service is null) return ResultadoOperacion<RespuestaPrecioReparacionModeloAdmin>.Fail(404, "No se encontró el tipo de reparación.");
        var brand = request.Brand.Trim(); var model = request.Model.Trim();
        var normalizedBrand = ClasificadorSintomas.Normalizar(brand); var normalizedModel = ClasificadorSintomas.NormalizarModelo(model);
        var device = Db.ModelosDispositivo.FirstOrDefault(x => x.Brand.ToUpper() == brand.ToUpper() && x.Model.ToUpper() == model.ToUpper());
        device ??= Db.ModelosDispositivo.AsEnumerable().FirstOrDefault(x =>
            ClasificadorSintomas.Normalizar(x.Brand) == normalizedBrand && ClasificadorSintomas.NormalizarModelo(x.Model) == normalizedModel);
        if (device is null) { device = new ModeloDispositivo(brand, model); Db.ModelosDispositivo.Add(device); }
        var price = Db.PreciosReparacionModelo.FirstOrDefault(x => x.DeviceModelId == device.Id && x.RepairCode == request.RepairCode);
        if (price is null) Db.PreciosReparacionModelo.Add(new PrecioReparacionModelo(device.Id, request.RepairCode, request.Price, "PEN", verifiedOn));
        else price.SetPrice(request.Price, "PEN", verifiedOn);
        Db.SaveChanges();
        return ResultadoOperacion<RespuestaPrecioReparacionModeloAdmin>.Ok(new(brand, model, request.RepairCode, service.Name, request.Price, "PEN", verifiedOn));
    }
}

public sealed class RepositorioCotizacionesEf(ContextoEvol db) : RepositorioBaseEf<CotizacionReparacion>(db), IRepositorioCotizaciones
{
    public Guid GuardarCotizacion(DateTimeOffset createdAt, string customerMessage, string? brand, string? model,
        string? issueCode, string status, decimal? minPrice, decimal? maxPrice, string reply)
    {
        var quote = new CotizacionReparacion(createdAt, customerMessage, brand, model, issueCode, status, minPrice, maxPrice, reply);
        Db.Cotizaciones.Add(quote); Db.SaveChanges(); return quote.Id;
    }

    public CotizacionParaCita? ObtenerCotizacionParaCita(Guid id) => Db.Cotizaciones.AsNoTracking().Where(x => x.Id == id)
        .Select(x => new CotizacionParaCita(x.Brand, x.Model, x.IssueCode, x.Status)).FirstOrDefault();
}

public sealed class RepositorioCitasEf(ContextoEvol db) : RepositorioBaseEf<Cita>(db), IRepositorioCitas
{
    public IReadOnlyList<RespuestaCita> ObtenerCitas(bool includePrice) =>
        Db.Citas.AsNoTracking().Include(x => x.Quote).ToList()
            .OrderBy(x => x.ScheduledAt).Select(x => ConvertirRespuesta(x, includePrice)).ToList();

    public RespuestaCita? ObtenerCita(Guid id, bool includePrice) => Db.Citas.AsNoTracking()
        .Include(x => x.Quote).FirstOrDefault(x => x.Id == id) is { } appointment ? ConvertirRespuesta(appointment, includePrice) : null;

    public RespuestaCita Crear(NuevaCita request, string brand, string model, string issue, DateTimeOffset scheduledAt)
    {
        var entity = new Cita(request.QuoteId, request.CustomerName, request.CustomerPhone, brand, model,
            issue, scheduledAt, "requested", request.Notes ?? "", DateTimeOffset.UtcNow);
        Db.Citas.Add(entity); Db.SaveChanges();
        return ConvertirRespuesta(entity, false);
    }

    public bool ActualizarEstadoPendiente(Guid id, string status)
    {
        var appointment = Db.Citas.FirstOrDefault(x => x.Id == id);
        if (appointment is null || !appointment.DecidirDisponibilidad(status)) return false;
        Db.SaveChanges(); return true;
    }

    public bool ExisteConflictoActivo(DateTimeOffset startsAt, DateTimeOffset endsAt, Guid? excludeId = null) =>
        Db.Citas.AsNoTracking().Where(x => excludeId == null || x.Id != excludeId)
            .AsEnumerable().Any(x => PoliticaCitas.EstaActiva(x.Status) &&
                PoliticaCitas.SeSuperponen(startsAt, endsAt, PoliticaCitas.ConvertirHoraLima(x.ScheduledAt), PoliticaCitas.ConvertirHoraLima(x.ScheduledAt).AddHours(1)));

    private static RespuestaCita ConvertirRespuesta(Cita x, bool includePrice) => new(x.Id, x.QuoteId,
        x.CustomerName, x.CustomerPhone, x.Brand, x.Model, x.Issue, x.ScheduledAt, x.Status, x.Notes, x.CreatedAt,
        includePrice ? x.Quote?.MinPrice ?? x.Quote?.MaxPrice : null,
        includePrice && x.Quote?.Reply.Contains("ficticio", StringComparison.OrdinalIgnoreCase) == true);
}

public sealed class RepositorioBloqueosHorarioEf(ContextoEvol db) : RepositorioBaseEf<BloqueHorario>(db), IRepositorioBloqueosHorario
{
    public IReadOnlyList<RespuestaBloqueHorario> ObtenerBloqueos() => Db.BloqueosHorario.AsNoTracking().ToList()
        .OrderBy(x => x.StartsAt)
        .Select(x => new RespuestaBloqueHorario(x.Id, x.StartsAt, x.EndsAt, x.Reason, x.CreatedAt)).ToList();

    public bool SeSuperpone(DateTimeOffset startsAt, DateTimeOffset endsAt) => Db.BloqueosHorario.AsNoTracking().AsEnumerable()
        .Any(x => PoliticaCitas.SeSuperponen(startsAt, endsAt, x.StartsAt, x.EndsAt));

    public RespuestaBloqueHorario Crear(DateTimeOffset startsAt, DateTimeOffset endsAt, string reason)
    {
        var entity = new BloqueHorario(startsAt, endsAt, reason, DateTimeOffset.UtcNow);
        Db.BloqueosHorario.Add(entity); Db.SaveChanges();
        return new(entity.Id, entity.StartsAt, entity.EndsAt, entity.Reason, entity.CreatedAt);
    }

    public bool Eliminar(Guid id)
    {
        var entity = Db.BloqueosHorario.FirstOrDefault(x => x.Id == id);
        if (entity is null) return false;
        Db.BloqueosHorario.Remove(entity); Db.SaveChanges(); return true;
    }
}




