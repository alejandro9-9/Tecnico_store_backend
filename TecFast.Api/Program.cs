using TecFast.Application;
using TecFast.Api;
using TecFast.Infrastructure;
using TecFast.Infrastructure.Persistencia;
using TecFast.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = (builder.Configuration["FRONTEND_ORIGIN"] ?? "*")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(origin => origin.TrimEnd('/')).ToArray();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Contains("*")) policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    else policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));

var databasePath = RutaBaseDatosTecFast.Resolver(builder.Configuration["DATABASE_PATH"], builder.Configuration["DATA_DIR"], AppContext.BaseDirectory);
builder.Services.AgregarInfraestructuraTecFast(databasePath);
builder.Services.AddSingleton(_ => ProveedorReglasIngreso.CargarDesdeCsv(Path.Combine(AppContext.BaseDirectory, "symptom_rules.csv")));
builder.Services.AddSingleton(_ => ProveedorDispositivos.CargarDesdeCsv(Path.Combine(AppContext.BaseDirectory, "device_rules.csv")));
builder.Services.AddSingleton<ExtractorDispositivo>();
builder.Services.AddScoped<ResolutorDispositivo>();
builder.Services.AddScoped<IEstrategiaRespuestaCotizacion, EstrategiaPreguntaFrecuente>();
builder.Services.AddScoped<IEstrategiaRespuestaCotizacion, EstrategiaDiagnosticoManualSinDispositivo>();
builder.Services.AddScoped<IEstrategiaRespuestaCotizacion, EstrategiaDiagnosticoManual>();
builder.Services.AddScoped<IEstrategiaRespuestaCotizacion, EstrategiaFallaNoIdentificada>();
builder.Services.AddScoped<IEstrategiaRespuestaCotizacion, EstrategiaDispositivoIncompleto>();
builder.Services.AddScoped<IEstrategiaRespuestaCotizacion, EstrategiaRevisionManualPorRegla>();
builder.Services.AddScoped<IEstrategiaRespuestaCotizacion, EstrategiaSinPrecio>();
builder.Services.AddScoped<IEstrategiaRespuestaCotizacion, EstrategiaEstimacion>();
builder.Services.AddScoped<IServicioCatalogo, ServicioCatalogo>();
builder.Services.AddScoped<IServicioCotizaciones, ServicioCotizaciones>();
builder.Services.AddScoped<IServicioCitas, ServicioCitas>();
builder.Services.AddScoped<IServicioAdministracion, ServicioAdministracion>();
builder.Services.AddScoped<IServicioDisponibilidad, ServicioDisponibilidad>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
});

var app = builder.Build();
app.UseCors();
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<InicializadorBaseDatos>().Inicializar();
app.MapearRutasTecFast();
app.Run();




