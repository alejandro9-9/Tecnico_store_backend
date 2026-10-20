from __future__ import annotations

from threading import Lock
from typing import Callable

from sqlalchemy import create_engine
from sqlalchemy.orm import DeclarativeBase, sessionmaker

from .config import get_database_url


DATABASE_URL = get_database_url()
connect_args = (
    {"check_same_thread": False}
    if DATABASE_URL.startswith("sqlite")
    else {"connect_timeout": 15}
)
engine = create_engine(
    DATABASE_URL,
    pool_pre_ping=True,
    pool_recycle=300,
    connect_args=connect_args,
)
SessionLocal = sessionmaker(bind=engine, autoflush=False, expire_on_commit=False)

_database_initializer: Callable[[], None] | None = None
_database_initialized = False
_database_init_lock = Lock()


class Base(DeclarativeBase):
    pass


def register_database_initializer(initializer: Callable[[], None]) -> None:
    global _database_initializer, _database_initialized
    _database_initializer = initializer
    _database_initialized = False


def ensure_database_initialized() -> None:
    """Initialize the schema only when an endpoint actually needs the DB."""
    global _database_initialized
    if _database_initialized:
        return
    if _database_initializer is None:
        raise RuntimeError("No se registró el inicializador de la base de datos.")
    with _database_init_lock:
        if _database_initialized:
            return
        _database_initializer()
        _database_initialized = True


def get_db():
    ensure_database_initialized()
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
