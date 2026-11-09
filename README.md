# Backend TecFast ASP.NET Core

API sobre .NET 10 organizada siguiendo la separación por capas del repositorio [Diarso](https://github.com/alejandro9-9/Diarso):

- `TecFast.Domain`: entidades de negocio (`Cita`, `CotizacionReparacion`, `ModeloDispositivo`, `ServicioReparacion`, `PrecioReparacionModelo`, `BloqueHorario`), reglas y la clase base `Entidad` con `Guid`.
- `TecFast.Application`: casos de uso, DTOs y contratos de repositorio. No depende de EF Core ni SQLite.
- `TecFast.Infrastructure`: `ContextoTecFast`, configuración EF Core, repositorios específicos y repositorio genérico; implementa los contratos de Aplicación.
- `TecFast.Api`: rutas HTTP, autenticación por tokens y composición de dependencias (`MapeadorRutas.cs`).

Los nombres de carpetas y archivos de negocio también están en español: `Entidades/`, `Abstracciones/`, `Repositorios/`, `Persistencia/`, `ServicioCitas.cs`, `ServicioCotizaciones.cs` y `ServicioAdministracion.cs`.

Las dependencias apuntan hacia el núcleo: API e Infraestructura dependen de Aplicación, y Aplicación depende de Dominio. Dominio no conoce EF Core. Los repositorios traducen entidades a DTOs para mantener la forma de respuesta que consume React. Las rutas HTTP y los nombres JSON se mantienen estables aunque las clases internas estén en español.

El chat es determinístico y sin créditos de IA: `symptom_rules.csv` contiene frases, variantes, preguntas aclaratorias, respuestas frecuentes y consejos de seguridad; `device_rules.csv` contiene alias de marcas y patrones de modelos. Para marcas y reparaciones disponibles, consulta el catálogo activo en la base de datos. Añade datos a los CSV para ampliar lo que reconoce; no agregues información comercial que la tienda no haya confirmado.

Los precios de `model_repair_prices.csv` son referencias de mercado para un taller independiente, no tarifas oficiales ni cotizaciones finales. Se muestran para orientar al cliente y pueden cambiar según la calidad y disponibilidad del repuesto y el diagnóstico técnico. La versión vigente se identifica con `MERCADO-2026-11-09`.

Ejecuta desde `TecFast.Api`:

```powershell
$env:STAFF_TOKEN="tec"
$env:ADMIN_TOKEN="adm"
$env:FRONTEND_ORIGIN="http://localhost:5173"
dotnet run --urls http://localhost:8000
```

La API usa EF Core con SQLite y crea `../../data/tecfast.db` localmente. También acepta `DATABASE_PATH` o `DATA_DIR`. El catálogo, modelos y precios iniciales se cargan desde los CSV; si encuentra `tecfast-net.db` en el mismo directorio, importa una vez los precios específicos guardados allí y deja intacta la base anterior. Las entidades y sus relaciones usan claves `Guid`.

Render publica esta API como servicio Docker con el plan gratuito. Su sistema de archivos no es persistente en ese plan; los datos SQLite pueden reiniciarse al volver a desplegar. Para producción se debe conectar almacenamiento persistente o migrar el proveedor de EF Core a PostgreSQL.
