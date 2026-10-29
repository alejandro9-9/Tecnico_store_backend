using Evol.Application;
using Evol.Api;
using Evol.Infrastructure;
using Evol.Infrastructure.Persistencia;
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

var databasePath = RutaBaseDatosEvol.Resolver(builder.Configuration["DATABASE_PATH"], builder.Configuration["DATA_DIR"], AppContext.BaseDirectory);
builder.Services.AgregarInfraestructuraEvol(databasePath);
builder.Services.AddSingleton(_ => ProveedorReglasIngreso.CargarDesdeCsv(Path.Combine(AppContext.BaseDirectory, "symptom_rules.csv")));
builder.Services.AddScoped<IServicioCatalogo, ServicioCatalogo>();
builder.Services.AddScoped<IServicioCotizaciones, ServicioCotizaciones>();
builder.Services.AddScoped<IServicioCitas, ServicioCitas>();
builder.Services.AddScoped<IServicioAdministracion, ServicioAdministracion>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();
app.UseCors();
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<InicializadorBaseDatos>().Inicializar();
app.MapearRutasEvol();
app.Run();




