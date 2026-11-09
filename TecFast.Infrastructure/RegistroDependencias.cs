using TecFast.Application.Abstracciones.Repositorios;
using TecFast.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TecFast.Infrastructure;

public static class RegistroDependencias
{
    public static IServiceCollection AgregarInfraestructuraTecFast(this IServiceCollection services, string databasePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath))!;
        Directory.CreateDirectory(directory);
        services.AddDbContext<ContextoTecFast>(options => options.UseSqlite($"Data Source={databasePath};Foreign Keys=True"));
        services.AddScoped<IRepositorio<TecFast.Domain.Entidades.ServicioReparacion>, RepositorioBaseEf<TecFast.Domain.Entidades.ServicioReparacion>>();
        services.AddScoped<IRepositorio<TecFast.Domain.Entidades.ModeloDispositivo>, RepositorioBaseEf<TecFast.Domain.Entidades.ModeloDispositivo>>();
        services.AddScoped<IRepositorio<TecFast.Domain.Entidades.PrecioReparacionModelo>, RepositorioBaseEf<TecFast.Domain.Entidades.PrecioReparacionModelo>>();
        services.AddScoped<IRepositorio<TecFast.Domain.Entidades.CotizacionReparacion>, RepositorioBaseEf<TecFast.Domain.Entidades.CotizacionReparacion>>();
        services.AddScoped<IRepositorio<TecFast.Domain.Entidades.Cita>, RepositorioBaseEf<TecFast.Domain.Entidades.Cita>>();
        services.AddScoped<IRepositorio<TecFast.Domain.Entidades.BloqueHorario>, RepositorioBaseEf<TecFast.Domain.Entidades.BloqueHorario>>();
        services.AddScoped<IRepositorioCatalogoReparaciones, RepositorioCatalogoEf>();
        services.AddScoped<IRepositorioCotizaciones, RepositorioCotizacionesEf>();
        services.AddScoped<IRepositorioCitas, RepositorioCitasEf>();
        services.AddScoped<IRepositorioBloqueosHorario, RepositorioBloqueosHorarioEf>();
        services.AddScoped(provider => new InicializadorBaseDatos(provider.GetRequiredService<ContextoTecFast>(), directory));
        return services;
    }
}

public static class RutaBaseDatosTecFast
{
    public static string Resolver(string? configuredPath, string? dataDirectory, string applicationBaseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var configuredFullPath = Path.GetFullPath(configuredPath);
            var directory = Path.GetDirectoryName(configuredFullPath)!;
            var file = Path.GetFileName(configuredFullPath);
            return Path.Combine(directory, file.Equals("tecfast-net.db", StringComparison.OrdinalIgnoreCase) ? "tecfast.db" : file);
        }
        if (!string.IsNullOrWhiteSpace(dataDirectory)) return Path.Combine(Path.GetFullPath(dataDirectory), "tecfast.db");
        var projectRoot = Path.GetFullPath(Path.Combine(applicationBaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(projectRoot, "data", "tecfast.db");
    }
}




