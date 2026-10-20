from __future__ import annotations

from contextlib import asynccontextmanager
from datetime import datetime, timedelta, timezone
from decimal import Decimal
import csv
import hmac
from pathlib import Path

from fastapi import Depends, FastAPI, Header, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from sqlalchemy import inspect, select, text
from sqlalchemy.orm import Session

from .config import get_admin_token, get_allowed_origins, get_staff_token
from .database import Base, engine, get_db
from .ai import interpret_intake
from .intake import (
    classify_issue,
    clarification_for,
    extract_brand,
    extract_model,
    read_catalog_rows,
    read_symptom_rules,
)
from .models import Appointment, AvailabilityBlock, DeviceModel, ModelRepairPrice, Quote, RepairService
from .pricing import find_device_model, find_model_price, find_service, format_pen
from .schemas import (
    AppointmentCreate,
    AppointmentResponse,
    AppointmentAvailabilityUpdate,
    AdminModelRepairPriceResponse,
    AvailabilityBlockCreate,
    AvailabilityBlockResponse,
    ChatRequest,
    ModelCatalogResponse,
    ModelRepairPriceUpdate,
    QuoteResponse,
    ServiceResponse,
)


BACKEND_DIR = Path(__file__).resolve().parents[1]
LIMA_TZ = timezone(timedelta(hours=-5))
APPOINTMENT_DURATION = timedelta(hours=1)
ACTIVE_STATUSES = {"scheduled", "confirmed", "arrived", "diagnosing", "awaiting_approval", "waiting_parts", "in_repair", "ready_for_pickup"}


def initialize_database() -> None:
    Base.metadata.create_all(bind=engine)
    # create_all creates new tables but does not add columns to an existing one.
    appointment_columns = {column["name"] for column in inspect(engine).get_columns("appointments")}
    if "quote_id" not in appointment_columns:
        with engine.begin() as connection:
            connection.execute(text("ALTER TABLE appointments ADD COLUMN quote_id INTEGER REFERENCES quotes(id)"))
            connection.execute(text("CREATE INDEX IF NOT EXISTS ix_appointments_quote_id ON appointments (quote_id)"))
    # Remove generic prices and source metadata left by older schemas. Model
    # repair prices now contain only the specific model, repair, and amount.
    legacy_columns = {
        "repair_services": {"min_price", "max_price", "source_url"},
        "device_models": {"source_url"},
        "model_repair_prices": {"source_name", "source_url", "notes"},
    }
    for table, obsolete in legacy_columns.items():
        columns = {column["name"] for column in inspect(engine).get_columns(table)}
        for column in obsolete & columns:
            with engine.begin() as connection:
                connection.execute(text(f"ALTER TABLE {table} DROP COLUMN {column}"))
    with Session(engine) as db:
        for row in read_catalog_rows():
            service = db.scalar(select(RepairService).where(RepairService.code == row["code"]))
            if service is None:
                service = RepairService(code=row["code"], name=row["name"])
                db.add(service)
            service.name = row["name"]
            service.requires_diagnosis = True
            service.notes = row.get("notes", "")

        # Keep known devices even when no exact public price is available yet.
        models_path = BACKEND_DIR / "known_models.csv"
        if models_path.exists():
            with models_path.open(encoding="utf-8-sig", newline="") as file:
                for row in csv.DictReader(file):
                    _get_or_create_device(db, row["brand"], row["model"])

        prices_path = BACKEND_DIR / "model_repair_prices.csv"
        with prices_path.open(encoding="utf-8-sig", newline="") as file:
            for row in csv.DictReader(file):
                device = _get_or_create_device(db, row["brand"], row["model"])
                price = db.scalar(select(ModelRepairPrice).where(
                    ModelRepairPrice.device_model_id == device.id,
                    ModelRepairPrice.repair_code == row["repair_code"],
                ))
                if price is None:
                    price = ModelRepairPrice(device_model_id=device.id, repair_code=row["repair_code"])
                    db.add(price)
                    price.price = Decimal(row["price"])
                    price.currency = "PEN"
                    price.verified_on = row["verified_on"]
        db.commit()


def _get_or_create_device(db: Session, brand: str, model: str) -> DeviceModel:
    device = db.scalar(select(DeviceModel).where(DeviceModel.brand == brand, DeviceModel.model == model))
    if device is None:
        device = DeviceModel(brand=brand, model=model)
        db.add(device)
        db.flush()
    return device


@asynccontextmanager
async def lifespan(_: FastAPI):
    initialize_database()
    yield


app = FastAPI(
    title="EVOL — Cotizador de reparación",
    description="API para interpretar la falla descrita por el cliente y generar una estimación referencial.",
    version="1.0.0",
    lifespan=lifespan,
)
app.add_middleware(
    CORSMiddleware,
    allow_origins=get_allowed_origins(),
    allow_credentials=False,
    allow_methods=["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
    allow_headers=["Content-Type", "Authorization", "X-Staff-Token", "X-Admin-Token"],
)


@app.get("/health")
def health():
    return {"status": "ok", "service": "evol-repair-api"}


def require_staff(x_staff_token: str | None = Header(default=None, alias="X-Staff-Token")) -> None:
    expected = get_staff_token()
    if not x_staff_token or not hmac.compare_digest(x_staff_token, expected):
        raise HTTPException(status_code=401, detail="Acceso del técnico no autorizado.")


def require_admin(x_admin_token: str | None = Header(default=None, alias="X-Admin-Token")) -> None:
    expected = get_admin_token()
    if not x_admin_token or not hmac.compare_digest(x_admin_token, expected):
        raise HTTPException(status_code=401, detail="Acceso de administración no autorizado.")


def _as_lima(value: datetime) -> datetime:
    return value.replace(tzinfo=LIMA_TZ) if value.tzinfo is None else value.astimezone(LIMA_TZ)


def _overlaps(start: datetime, end: datetime, other_start: datetime, other_end: datetime) -> bool:
    return _as_lima(start) < _as_lima(other_end) and _as_lima(other_start) < _as_lima(end)


def _appointment_dict(appointment: Appointment, db: Session) -> dict:
    data = AppointmentResponse.model_validate(appointment).model_dump()
    quote = db.get(Quote, appointment.quote_id) if appointment.quote_id else None
    data["quote_price"] = (quote.min_price if quote and quote.min_price is not None else quote.max_price) if quote else None
    return data


def _ensure_not_blocked(db: Session, start: datetime, end: datetime) -> None:
    blocks = db.scalars(select(AvailabilityBlock)).all()
    if any(_overlaps(start, end, block.starts_at, block.ends_at) for block in blocks):
        raise HTTPException(status_code=409, detail="Ese horario está bloqueado por el taller. Elige otra fecha u hora.")


def _ensure_no_active_conflict(db: Session, appointment: Appointment, start: datetime) -> None:
    end = start + APPOINTMENT_DURATION
    _ensure_not_blocked(db, start, end)
    appointments = db.scalars(select(Appointment).where(Appointment.id != appointment.id)).all()
    for other in appointments:
        if other.status in ACTIVE_STATUSES and _overlaps(start, end, other.scheduled_at, _as_lima(other.scheduled_at) + APPOINTMENT_DURATION):
            raise HTTPException(status_code=409, detail="Ese horario ya está ocupado por otra cita activa. Elige otro horario.")


def _appointment_from_request(request: AppointmentCreate, db: Session, status: str) -> Appointment:
    data = request.model_dump()
    if data["quote_id"] is not None:
        quote = db.get(Quote, data["quote_id"])
        if quote is None:
            raise HTTPException(status_code=404, detail="No se encontró la cotización asociada.")
        if quote.status not in {"estimate", "manual_review"}:
            raise HTTPException(status_code=409, detail="Completa primero la información del teléfono y la reparación antes de solicitar una cita.")
        if not quote.brand or not quote.model or not quote.issue_code:
            raise HTTPException(status_code=409, detail="La cotización no tiene un teléfono y una reparación identificados. Vuelve al chat para completar esos datos.")
        service = find_service(db, quote.issue_code)
        if service is None:
            raise HTTPException(status_code=409, detail="No se pudo identificar el tipo de reparación de la cotización.")
        # The quote is the source of truth: a client request cannot accidentally
        # become associated with another model or repair.
        data["brand"] = quote.brand
        data["model"] = quote.model
        data["issue"] = service.name
    scheduled_at = data["scheduled_at"]
    if scheduled_at.tzinfo is None:
        scheduled_at = scheduled_at.replace(tzinfo=LIMA_TZ)
    else:
        scheduled_at = scheduled_at.astimezone(LIMA_TZ)
    if scheduled_at <= datetime.now(LIMA_TZ):
        raise HTTPException(status_code=422, detail="La fecha y hora de la cita deben ser futuras.")
    _ensure_not_blocked(db, scheduled_at, scheduled_at + APPOINTMENT_DURATION)
    data["scheduled_at"] = scheduled_at
    return Appointment(**data, status=status)


@app.post("/api/appointments", response_model=AppointmentResponse, status_code=201)
def request_appointment(request: AppointmentCreate, db: Session = Depends(get_db)):
    """Public customer flow; new requests await confirmation by the workshop."""
    appointment = _appointment_from_request(request, db, status="requested")
    db.add(appointment)
    db.commit()
    db.refresh(appointment)
    return _appointment_dict(appointment, db)


@app.get("/api/staff/appointments", response_model=list[AppointmentResponse], dependencies=[Depends(require_staff)])
def list_appointments(db: Session = Depends(get_db)):
    appointments = db.scalars(select(Appointment).order_by(Appointment.scheduled_at, Appointment.id)).all()
    # The technician only needs appointment details to decide availability.
    return [AppointmentResponse.model_validate(item).model_dump() for item in appointments]


@app.get("/api/admin/appointments", response_model=list[AppointmentResponse], dependencies=[Depends(require_admin)])
def admin_list_appointments(db: Session = Depends(get_db)):
    appointments = db.scalars(select(Appointment).order_by(Appointment.scheduled_at, Appointment.id)).all()
    return [_appointment_dict(item, db) for item in appointments]


@app.get("/api/admin/blocks", response_model=list[AvailabilityBlockResponse], dependencies=[Depends(require_admin)])
def list_availability_blocks(db: Session = Depends(get_db)):
    return db.scalars(select(AvailabilityBlock).order_by(AvailabilityBlock.starts_at)).all()


@app.post("/api/admin/blocks", response_model=AvailabilityBlockResponse, status_code=201, dependencies=[Depends(require_admin)])
def create_availability_block(request: AvailabilityBlockCreate, db: Session = Depends(get_db)):
    start, end = _as_lima(request.starts_at), _as_lima(request.ends_at)
    if start <= datetime.now(LIMA_TZ) or end <= start:
        raise HTTPException(status_code=422, detail="Indica un horario futuro y una hora final posterior a la inicial.")
    if any(_overlaps(start, end, block.starts_at, block.ends_at) for block in db.scalars(select(AvailabilityBlock)).all()):
        raise HTTPException(status_code=409, detail="El horario se cruza con otro bloqueo existente.")
    appointments = db.scalars(select(Appointment)).all()
    for item in appointments:
        if item.status in ACTIVE_STATUSES and _overlaps(start, end, item.scheduled_at, _as_lima(item.scheduled_at) + APPOINTMENT_DURATION):
            raise HTTPException(status_code=409, detail="No se puede bloquear: ya existe una cita activa en ese horario.")
    block = AvailabilityBlock(starts_at=start, ends_at=end, reason=request.reason.strip())
    db.add(block)
    db.commit()
    db.refresh(block)
    return block


@app.delete("/api/admin/blocks/{block_id}", status_code=204, dependencies=[Depends(require_admin)])
def delete_availability_block(block_id: int, db: Session = Depends(get_db)):
    block = db.get(AvailabilityBlock, block_id)
    if block is None:
        raise HTTPException(status_code=404, detail="No se encontró el bloqueo.")
    db.delete(block)
    db.commit()


@app.patch(
    "/api/staff/appointments/{appointment_id}",
    response_model=AppointmentResponse,
    dependencies=[Depends(require_staff)],
)
def update_appointment(
    appointment_id: int,
    request: AppointmentAvailabilityUpdate,
    db: Session = Depends(get_db),
):
    appointment = db.get(Appointment, appointment_id)
    if appointment is None:
        raise HTTPException(status_code=404, detail="No se encontró la cita.")
    if appointment.status != "requested":
        raise HTTPException(status_code=409, detail="Solo se puede decidir la disponibilidad de solicitudes pendientes.")
    if request.status == "confirmed":
        _ensure_no_active_conflict(db, appointment, _as_lima(appointment.scheduled_at))
    appointment.status = request.status
    db.commit()
    db.refresh(appointment)
    return AppointmentResponse.model_validate(appointment).model_dump()


@app.get("/api/services", response_model=list[ServiceResponse])
def list_services(db: Session = Depends(get_db)):
    return db.scalars(select(RepairService).order_by(RepairService.name)).all()


@app.get(
    "/api/admin/model-prices",
    response_model=list[AdminModelRepairPriceResponse],
    dependencies=[Depends(require_admin)],
)
def admin_list_model_prices(db: Session = Depends(get_db)):
    rows = db.execute(
        select(ModelRepairPrice, DeviceModel, RepairService)
        .join(DeviceModel, DeviceModel.id == ModelRepairPrice.device_model_id)
        .join(RepairService, RepairService.code == ModelRepairPrice.repair_code)
        .order_by(DeviceModel.brand, DeviceModel.model, RepairService.name)
    ).all()
    return [
        {
            "brand": device.brand,
            "model": device.model,
            "repair_code": price.repair_code,
            "repair_name": service.name,
            "price": price.price,
            "currency": price.currency,
            "verified_on": price.verified_on,
        }
        for price, device, service in rows
    ]


@app.put(
    "/api/admin/model-prices",
    response_model=AdminModelRepairPriceResponse,
    dependencies=[Depends(require_admin)],
)
def upsert_model_repair_price(request: ModelRepairPriceUpdate, db: Session = Depends(get_db)):
    service = db.scalar(select(RepairService).where(RepairService.code == request.repair_code))
    if service is None:
        raise HTTPException(status_code=404, detail="No se encontró el tipo de reparación.")
    device = find_device_model(db, request.brand, request.model)
    if device is None:
        device = _get_or_create_device(db, request.brand.strip(), request.model.strip())
    price = db.scalar(
        select(ModelRepairPrice).where(
            ModelRepairPrice.device_model_id == device.id,
            ModelRepairPrice.repair_code == request.repair_code,
        )
    )
    if price is None:
        price = ModelRepairPrice(device_model_id=device.id, repair_code=request.repair_code)
        db.add(price)
    price.price = request.price
    price.currency = "PEN"
    price.verified_on = datetime.now(LIMA_TZ).date().isoformat()
    db.commit()
    db.refresh(price)
    return {
        "brand": device.brand,
        "model": device.model,
        "repair_code": price.repair_code,
        "repair_name": service.name,
        "price": price.price,
        "currency": price.currency,
        "verified_on": price.verified_on,
    }


@app.get("/api/symptoms")
def list_symptom_mappings(db: Session = Depends(get_db)):
    services = {service.code: service.name for service in db.scalars(select(RepairService)).all()}
    return [
        {
            "code": str(rule["issue_code"]),
            "repair": services.get(str(rule["issue_code"]), str(rule["issue_code"])),
            "examples": [example.strip() for example in str(rule["examples"]).split("|") if example.strip()],
            "question": str(rule["question"]),
        }
        for rule in read_symptom_rules()
        if rule["kind"] == "repair"
    ]


@app.get("/api/models", response_model=list[ModelCatalogResponse])
def list_models(db: Session = Depends(get_db)):
    devices = db.scalars(select(DeviceModel).order_by(DeviceModel.brand, DeviceModel.model)).all()
    return [
        ModelCatalogResponse(
            brand=device.brand,
            model=device.model,
            prices={price.repair_code: price.price for price in db.scalars(select(ModelRepairPrice).where(
                ModelRepairPrice.device_model_id == device.id
            )).all()},
        )
        for device in devices
    ]


@app.post("/api/chat", response_model=QuoteResponse)
def chat(request: ChatRequest, db: Session = Depends(get_db)):
    user_messages = [item.content.strip() for item in request.messages if item.role == "user"]
    if not user_messages:
        raise HTTPException(status_code=422, detail="Envía al menos un mensaje del cliente.")

    full_text = "\n".join(user_messages)
    interpretation, _provider = interpret_intake(full_text)
    # A new explicit symptom in a follow-up replaces the previous repair intent,
    # while a reply containing only the model keeps the earlier symptom.
    latest_issue = next(
        (issue for message in reversed(user_messages) if (issue := classify_issue(message))),
        None,
    )
    latest_device_message = next(
        (
            message for message in reversed(user_messages)
            if extract_brand(message) or extract_model(message)
        ),
        None,
    )
    latest_brand = extract_brand(latest_device_message or "")
    latest_model = extract_model(latest_device_message or "")
    if latest_device_message:
        brand = latest_brand or request.brand
        model = latest_model or (request.model if brand == request.brand else None)
    else:
        brand = interpretation.brand or request.brand
        model = interpretation.model or request.model
    issue_code = latest_issue or interpretation.issue_code
    if brand and model:
        known_device = find_device_model(db, brand, model)
        if known_device:
            brand, model = known_device.brand, known_device.model
    service = find_service(db, issue_code) if issue_code else None

    status = "needs_details"
    model_price = find_model_price(db, brand, model, issue_code) if brand and model and issue_code else None
    exact_price = model_price[1] if model_price else None
    min_price = exact_price.price if exact_price else None
    max_price = exact_price.price if exact_price else None
    issue_name = service.name if service else None

    if not service:
        reply = clarification_for(full_text) or (
            "¿Qué síntoma presenta exactamente? Por ejemplo: pantalla rota, no carga, "
            "batería dura poco, falla la cámara, no enciende o se mojó."
        )
    elif not brand or not model:
        missing = "la marca y el modelo" if not brand and not model else ("la marca" if not brand else "el modelo exacto")
        reply = (
            f"Entiendo que necesitas revisar {service.name.lower()}. "
            f"Para consultar un precio específico, ¿me indicas {missing} de tu teléfono?"
        )
    elif issue_code in {"liquid", "board"} or min_price is None or max_price is None:
        status = "manual_review"
        if issue_code in {"liquid", "board"}:
            reply = (
                f"Por lo que describes, podría tratarse de: {service.name.lower()}. "
                "Este caso requiere diagnóstico técnico y no tiene una tarifa automática por modelo registrada. "
                "¿Quieres solicitar una cita para que el técnico lo revise?"
            )
        else:
            reply = (
                f"Tengo registrado el equipo {brand} {model} y el tipo de reparación ({service.name.lower()}), "
                "pero todavía no hay un costo específico verificado para esa combinación en la base de datos. "
                "No usaré un precio genérico. ¿Quieres solicitar una cita para que el técnico lo revise?"
            )
    else:
        status = "estimate"
        issue_name = service.name.removesuffix(" (referencial)")
        reply = (
            f"El precio registrado para {issue_name.lower()} del {brand} {model} es "
            f"{format_pen(exact_price.price)}. La cotización final depende de la revisión "
            "y confirmación del técnico. "
            "¿Quieres solicitar una cita con el taller?"
        )

    quote = Quote(
        customer_message="\n".join(user_messages),
        brand=brand,
        model=model,
        issue_code=issue_code,
        status=status,
        min_price=min_price,
        max_price=max_price,
        reply=reply,
    )
    db.add(quote)
    db.commit()
    db.refresh(quote)

    return QuoteResponse(
        quote_id=quote.id,
        status=status,
        reply=reply,
        brand=brand,
        model=model,
        issue_code=issue_code,
        issue_name=issue_name,
        min_price=min_price,
        max_price=max_price,
    )
