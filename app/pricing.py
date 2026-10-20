from __future__ import annotations

from decimal import Decimal

from sqlalchemy import select
from sqlalchemy.orm import Session

from .intake import normalize
from .models import DeviceModel, ModelRepairPrice, RepairService


def find_service(db: Session, code: str) -> RepairService | None:
    return db.scalar(select(RepairService).where(RepairService.code == code))


def format_pen(value: Decimal) -> str:
    return f"S/ {value:,.2f}"


def find_device_model(db: Session, brand: str, model: str) -> DeviceModel | None:
    wanted_brand, wanted_model = normalize(brand).strip(), " ".join(normalize(model).split())
    for device in db.scalars(select(DeviceModel)):
        if normalize(device.brand).strip() == wanted_brand and " ".join(normalize(device.model).split()) == wanted_model:
            return device
    return None


def find_model_price(db: Session, brand: str, model: str, repair_code: str) -> tuple[DeviceModel, ModelRepairPrice] | None:
    """Only return a cost explicitly registered for this exact brand/model/repair."""
    device = find_device_model(db, brand, model)
    if device is None:
        return None
    price = db.scalar(select(ModelRepairPrice).where(
        ModelRepairPrice.device_model_id == device.id,
        ModelRepairPrice.repair_code == repair_code,
    ))
    return (device, price) if price else None
