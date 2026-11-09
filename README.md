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

La API usa EF Core con PostgreSQL mediante Npgsql. Configura `DATABASE_URL` con la cadena de conexión de Neon (se acepta el formato `postgresql://...` que entrega Neon o el formato de parámetros de Npgsql). En el primer arranque crea las tablas y carga el catálogo, modelos y precios iniciales desde los CSV; las cargas son idempotentes. Las entidades y sus relaciones usan claves `Guid`.

Render publica esta API como servicio Docker. La base de datos vive en Neon, separada del contenedor, por lo que los datos no dependen del sistema de archivos efímero de Render. En Render configura `DATABASE_URL`, `STAFF_TOKEN`, `ADMIN_TOKEN` y `FRONTEND_ORIGIN`.

## Despliegue en Render + Neon

1. Crea un proyecto PostgreSQL en Neon y copia la cadena pooled desde **Connect**.
2. En el servicio `tecfast-api` de Render agrega esa cadena como `DATABASE_URL`.
3. Configura `FRONTEND_ORIGIN` con la URL final de Vercel.
4. Configura `STAFF_TOKEN` y `ADMIN_TOKEN` con valores seguros.
5. Despliega el backend; `EnsureCreated()` crea las tablas y el inicializador carga los CSV.
