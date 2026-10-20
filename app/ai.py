from __future__ import annotations

import os
from typing import Literal

from pydantic import BaseModel, Field

from .intake import classify_issue, extract_brand, extract_model


IssueCode = Literal[
    "screen", "battery", "charging_port", "liquid", "software", "board",
    "camera", "audio", "buttons", "back_glass", "connectivity", "other",
]


class IntakeInterpretation(BaseModel):
    issue_code: IssueCode | None = Field(description="Categoría probable; null si no se entiende con suficiente claridad.")
    brand: str | None = Field(description="Marca solo si está indicada o se deduce con claridad del texto.")
    model: str | None = Field(description="Modelo exacto solo si aparece claramente en el texto.")
    clarifying_question: str = Field(description="Una sola pregunta breve para pedir el dato más importante que falta.")


def interpret_intake(text: str) -> tuple[IntakeInterpretation, str]:
    """Interpret the message with an optional LLM; always retain a no-key fallback."""
    provider = os.getenv("AI_PROVIDER", "rules").strip().lower()
    if provider == "gemini" and os.getenv("GEMINI_API_KEY"):
        try:
            return _interpret_with_gemini(text), "gemini"
        except Exception:
            # The service remains usable if the provider quota is reached or unavailable.
            pass

    result = IntakeInterpretation(
        issue_code=classify_issue(text) or "other",
        brand=extract_brand(text),
        model=extract_model(text),
        clarifying_question="¿Qué marca y modelo exacto es tu teléfono y qué síntoma presenta?",
    )
    return result, "rules"


def _interpret_with_gemini(text: str) -> IntakeInterpretation:
    from google import genai
    from google.genai import types

    client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])
    model_name = os.getenv("AI_MODEL", "gemini-2.5-flash-lite")
    prompt = f"""
Interpreta los mensajes de un cliente de un taller de celulares. Devuelve únicamente los campos del esquema.
No inventes marca ni modelo. issue_code debe ser una de: screen, battery, charging_port, liquid, software, board, camera, audio, buttons, back_glass, connectivity, other.
Usa null si no puedes identificar una categoría con base en el texto. El daño por agua/líquido y placa requieren revisión técnica.
No diagnostiques como certeza y no calcules ni sugieras ningún precio.
Haz una sola pregunta corta para obtener el dato más importante que falte.

Mensaje del cliente:
{text[:6000]}
""".strip()
    response = client.models.generate_content(
        model=model_name,
        contents=prompt,
        config=types.GenerateContentConfig(
            response_mime_type="application/json",
            response_schema=IntakeInterpretation,
            temperature=0,
        ),
    )
    if not response.text:
        raise ValueError("El modelo no devolvió una interpretación.")
    parsed = IntakeInterpretation.model_validate_json(response.text)
    if parsed.issue_code == "other":
        parsed.issue_code = None
    return parsed
