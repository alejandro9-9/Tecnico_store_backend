using Evol.Application.Abstracciones.Repositorios;
using Evol.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Evol.Infrastructure;

public static class RegistroDependencias
{
    public static IServiceCollection AgregarInfraestructuraEvol(this IServiceCollection services, string databasePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath))!;
        Directory.CreateDirectory(directory);
        services.AddDbContext<ContextoEvol>(options => options.UseSqlite($"Data Source={databasePath};Foreign Keys=True"));
        services.AddScoped<IRepositorio<Evol.Domain.Entidades.ServicioReparacion>, RepositorioBaseEf<Evol.Domain.Entidades.ServicioReparacion>>();
        services.AddScoped<IRepositorio<Evol.Domain.Entidades.ModeloDispositivo>, RepositorioBaseEf<Evol.Domain.Entidades.ModeloDispositivo>>();
        services.AddScoped<IRepositorio<Evol.Domain.Entidades.PrecioReparacionModelo>, RepositorioBaseEf<Evol.Domain.Entidades.PrecioReparacionModelo>>();
        services.AddScoped<IRepositorio<Evol.Domain.Entidades.CotizacionReparacion>, RepositorioBaseEf<Evol.Domain.Entidades.CotizacionReparacion>>();
        services.AddScoped<IRepositorio<Evol.Domain.Entidades.Cita>, RepositorioBaseEf<Evol.Domain.Entidades.Cita>>();
        services.AddScoped<IRepositorio<Evol.Domain.Entidades.BloqueHorario>, RepositorioBaseEf<Evol.Domain.Entidades.BloqueHorario>>();
        services.AddScoped<IRepositorioCatalogoReparaciones, RepositorioCatalogoEf>();
        services.AddScoped<IRepositorioCotizaciones, RepositorioCotizacionesEf>();
        services.AddScoped<IRepositorioCitas, RepositorioCitasEf>();
        services.AddScoped<IRepositorioBloqueosHorario, RepositorioBloqueosHorarioEf>();
        services.AddScoped(provider => new InicializadorBaseDatos(provider.GetRequiredService<ContextoEvol>(), directory));
        return services;
    }
}

public static class RutaBaseDatosEvol
{
    public static string Resolver(string? configuredPath, string? dataDirectory, string applicationBaseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var configuredFullPath = Path.GetFullPath(configuredPath);
            var directory = Path.GetDirectoryName(configuredFullPath)!;
            var file = Path.GetFileName(configuredFullPath);
            return Path.Combine(directory, file.Equals("evol-net.db", StringComparison.OrdinalIgnoreCase) ? "evol-guid.db" : file);
        }
        if (!string.IsNullOrWhiteSpace(dataDirectory)) return Path.Combine(Path.GetFullPath(dataDirectory), "evol-guid.db");
        var projectRoot = Path.GetFullPath(Path.Combine(applicationBaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(projectRoot, "data", "evol-guid.db");
    }
}




