from __future__ import annotations

import os


def get_database_url() -> str:
    url = os.getenv("DATABASE_URL", "sqlite:///./evol.db")
    if url.startswith("postgres://"):
        url = "postgresql+psycopg://" + url.removeprefix("postgres://")
    elif url.startswith("postgresql://") and "+psycopg" not in url:
        url = url.replace("postgresql://", "postgresql+psycopg://", 1)
    return url


def get_allowed_origins() -> list[str]:
    value = os.getenv("FRONTEND_ORIGIN", "*").strip()
    origins = [origin.strip().rstrip("/") for origin in value.split(",") if origin.strip()]
    return [f"https://{origin}" if origin != "*" and "://" not in origin else origin for origin in origins]


def get_staff_token() -> str:
    # The deployed service should supply its own STAFF_TOKEN secret.
    return os.getenv("STAFF_TOKEN", "tec")


def get_admin_token() -> str:
    # The deployed service should supply its own ADMIN_TOKEN secret.
    return os.getenv("ADMIN_TOKEN", "adm")
