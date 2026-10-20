# EVOL — backend

API de cotizaciones, catálogo de precios por modelo y reparación, y gestión de solicitudes de cita.

## Tecnologías

- Python
- FastAPI y Uvicorn para la API REST
- Pydantic para validar solicitudes y respuestas
- SQLAlchemy para persistencia con SQLite o PostgreSQL
- Reglas locales de interpretación; Gemini se puede activar de forma opcional

## Ejecución local

Desde esta carpeta:

```powershell
py -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

La documentación interactiva está en `http://localhost:8000/docs`. Si `DATABASE_URL` se omite, el backend usa SQLite local. Los archivos CSV inicializan el catálogo, modelos conocidos, reglas y precios específicos.

## Variables de entorno

Copia `.env.example` a `.env` y configura `DATABASE_URL`, `STAFF_TOKEN`, `ADMIN_TOKEN` y `FRONTEND_ORIGIN`. `AI_PROVIDER=rules` no requiere una clave de IA. Para activar Gemini, configura `AI_PROVIDER=gemini` y `GEMINI_API_KEY` como secreto; la aplicación vuelve a las reglas locales cuando Gemini no está disponible.

## Docker

```powershell
docker build -t evol-backend .
docker run --rm -p 8000:8000 -e DATABASE_URL="sqlite:///./evol.db" evol-backend
```

Para producción configura `DATABASE_URL` con la cadena PostgreSQL de Neon y establece tokens propios para el técnico y administración. No publiques claves ni el archivo de base de datos.

## Despliegue en Render

El archivo `render.yaml` define el servicio FastAPI. Conecta este repositorio como Blueprint, agrega la URL de Neon y configura los secretos solicitados. Después de desplegar, copia la URL HTTPS del backend a `BACKEND_URL` en el repositorio frontend y añade la URL del frontend a `FRONTEND_ORIGIN`.
