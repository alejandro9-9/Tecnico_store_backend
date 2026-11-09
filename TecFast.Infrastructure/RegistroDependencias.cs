using TecFast.Application.Abstracciones.Repositorios;
using TecFast.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace TecFast.Infrastructure;

public static class RegistroDependencias
{
    public static IServiceCollection AgregarInfraestructuraTecFast(this IServiceCollection services, string databaseConnection)
    {
        services.AddDbContext<ContextoTecFast>(options => options.UseNpgsql(databaseConnection, npgsql =>
            npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));
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
        services.AddScoped(provider => new InicializadorBaseDatos(provider.GetRequiredService<ContextoTecFast>()));
        return services;
    }
}

public static class ConexionPostgresTecFast
{
    public static string Resolver(IConfiguration configuration)
    {
        var value = configuration.GetConnectionString("Default") ?? configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Configura DATABASE_URL o ConnectionStrings__Default con la cadena de conexión de Neon.");

        return Normalizar(value);
    }

    private static string Normalizar(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return value;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
            throw new InvalidOperationException("DATABASE_URL no contiene una URL válida de PostgreSQL.");

        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2 || string.IsNullOrWhiteSpace(userInfo[0]))
            throw new InvalidOperationException("DATABASE_URL debe incluir usuario y contraseña de PostgreSQL.");

        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1]),
            SslMode = SslMode.Require,
            Pooling = true,
            Timeout = 15,
            CommandTimeout = 30,
            MaxPoolSize = 20
        };

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = parameter.Split('=', 2);
            if (pair.Length != 2) continue;
            var key = Uri.UnescapeDataString(pair[0]).ToLowerInvariant();
            var parameterValue = Uri.UnescapeDataString(pair[1]);
            if (key == "sslmode" && Enum.TryParse<SslMode>(parameterValue, true, out var sslMode))
                connection.SslMode = sslMode;
            else if (key == "channel_binding" && Enum.TryParse<ChannelBinding>(parameterValue, true, out var channelBinding))
                connection.ChannelBinding = channelBinding;
            else if (key == "connect_timeout" && int.TryParse(parameterValue, out var timeout))
                connection.Timeout = timeout;
        }

        return connection.ConnectionString;
    }
}




