from __future__ import annotations

from datetime import datetime
from decimal import Decimal
from typing import Literal

from pydantic import BaseModel, Field


class ChatMessage(BaseModel):
    role: Literal["user", "assistant"]
    content: str = Field(min_length=1, max_length=2000)


class ChatRequest(BaseModel):
    messages: list[ChatMessage] = Field(min_length=1, max_length=30)
    brand: str | None = Field(default=None, max_length=80)
    model: str | None = Field(default=None, max_length=120)


class QuoteResponse(BaseModel):
    quote_id: int
    status: Literal["needs_details", "estimate", "manual_review"]
    reply: str
    brand: str | None = None
    model: str | None = None
    issue_code: str | None = None
    issue_name: str | None = None
    min_price: Decimal | None = None
    max_price: Decimal | None = None
    currency: str = "PEN"
    disclaimer: str = (
        "Se usa una tarifa publicada para el modelo y la reparación exactos, cuando existe; "
        "la cotización final requiere confirmación técnica."
    )


class ServiceResponse(BaseModel):
    code: str
    name: str
    notes: str

    model_config = {"from_attributes": True}


class ModelCatalogResponse(BaseModel):
    brand: str
    model: str
    prices: dict[str, Decimal]


class ModelRepairPriceUpdate(BaseModel):
    brand: str = Field(min_length=2, max_length=80)
    model: str = Field(min_length=1, max_length=120)
    repair_code: str = Field(min_length=2, max_length=40)
    price: Decimal = Field(ge=0, max_digits=10, decimal_places=2)

    model_config = {"extra": "forbid"}


class AdminModelRepairPriceResponse(BaseModel):
    brand: str
    model: str
    repair_code: str
    repair_name: str
    price: Decimal
    currency: str
    verified_on: str


class AppointmentCreate(BaseModel):
    quote_id: int | None = None
    customer_name: str = Field(min_length=2, max_length=120)
    customer_phone: str = Field(min_length=7, max_length=30)
    brand: str = Field(min_length=2, max_length=80)
    model: str = Field(min_length=1, max_length=120)
    issue: str = Field(min_length=3, max_length=240)
    scheduled_at: datetime
    notes: str = Field(default="", max_length=1000)


class AppointmentAvailabilityUpdate(BaseModel):
    status: Literal["confirmed", "cancelled"]

    model_config = {"extra": "forbid"}


class AppointmentResponse(BaseModel):
    id: int
    quote_id: int | None = None
    customer_name: str
    customer_phone: str
    brand: str
    model: str
    issue: str
    scheduled_at: datetime
    status: str
    notes: str
    created_at: datetime
    quote_price: Decimal | None = None

    model_config = {"from_attributes": True}


class AvailabilityBlockCreate(BaseModel):
    starts_at: datetime
    ends_at: datetime
    reason: str = Field(default="Horario no disponible", min_length=3, max_length=240)


class AvailabilityBlockResponse(BaseModel):
    id: int
    starts_at: datetime
    ends_at: datetime
    reason: str
    created_at: datetime

    model_config = {"from_attributes": True}
