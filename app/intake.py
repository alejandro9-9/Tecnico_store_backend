from __future__ import annotations

import csv
import re
import unicodedata
from functools import lru_cache
from pathlib import Path


BACKEND_DIR = Path(__file__).resolve().parents[1]
CATALOG_PATH = BACKEND_DIR / "catalog.csv"
SYMPTOM_RULES_PATH = BACKEND_DIR / "symptom_rules.csv"


def normalize(text: str) -> str:
    value = unicodedata.normalize("NFKD", text.lower())
    value = "".join(char for char in value if not unicodedata.combining(char))
    return re.sub(r"[^a-z0-9]+", " ", value).strip()


@lru_cache(maxsize=1)
def read_symptom_rules() -> tuple[dict[str, str | int], ...]:
    with SYMPTOM_RULES_PATH.open(encoding="utf-8-sig", newline="") as file:
        rows = []
        for raw in csv.DictReader(file):
            raw["priority"] = int(raw["priority"])
            raw["phrases_list"] = tuple(
                phrase.strip() for phrase in raw["phrases"].split("|") if phrase.strip()
            )
            rows.append(raw)
    return tuple(sorted(rows, key=lambda row: int(row["priority"]), reverse=True))


def _contains_phrase(text: str, phrase: str) -> bool:
    normalized_text = f" {normalize(text)} "
    normalized_phrase = f" {normalize(phrase)} "
    return normalized_phrase in normalized_text


def classify_issue(text: str) -> str | None:
    """Map customer wording to a repair category using the editable CSV catalog."""
    for rule in read_symptom_rules():
        if rule["kind"] != "repair":
            continue
        if any(_contains_phrase(text, phrase) for phrase in rule["phrases_list"]):
            return str(rule["issue_code"])
    return None


def clarification_for(text: str) -> str | None:
    """Return a mapped question for vague descriptions such as a fall or impact."""
    for rule in read_symptom_rules():
        if rule["kind"] != "clarification":
            continue
        if any(_contains_phrase(text, phrase) for phrase in rule["phrases_list"]):
            return str(rule["question"])
    return None


def extract_brand(text: str) -> str | None:
    normalized = normalize(text)
    aliases = (
        (("apple", "iphone"), "Apple"),
        (("samsung", "galaxy"), "Samsung"),
        (("xiaomi", "redmi", "poco", "mi phone"), "Xiaomi"),
        (("motorola", "moto"), "Motorola"),
        (("huawei",), "Huawei"),
        (("honor",), "Honor"),
        (("oppo",), "Oppo"),
        (("vivo",), "Vivo"),
        (("realme",), "Realme"),
        (("google pixel", "pixel"), "Google"),
    )
    for names, brand in aliases:
        if any(f" {normalize(name)} " in f" {normalized} " for name in names):
            return brand
    model = extract_model(text)
    if model and model.lower().startswith("galaxy "):
        return "Samsung"
    if model and model.lower().startswith("iphone "):
        return "Apple"
    return None


def extract_model(text: str) -> str | None:
    normalized = " ".join(text.split())
    patterns = (
        r"\b(i\s?phone\s?(?:1[0-9]|[0-9])(?:\s?(?:pro\s?max|pro|plus|mini))?)\b",
        r"\b((?:galaxy\s*)?(?:s|a|z|m)\s?\d{2,3}(?:\s?(?:ultra|plus|\+|fe|5g))?)\b",
        r"\b((?:galaxy\s*)?(?:flip|fold)\s?\d(?:\s?fe)?)\b",
        r"\b(redmi\s?(?:note\s?)?\d{1,2}(?:\s?pro(?:\s?\+)?|\s?\+)?(?:\s?5g)?)\b",
        r"\b(poco\s?[a-z]?\s?\d{1,2}(?:\s?pro|\s?x\d)?)\b",
        r"\b(moto\s?[a-z]?\d{1,2}(?:\s?power|\s?play|\s?plus)?)\b",
        r"\b(pixel\s?\d{1,2}(?:\s?(?:pro|a|xl))?)\b",
    )
    for pattern in patterns:
        match = re.search(pattern, normalized, flags=re.IGNORECASE)
        if match:
            model = " ".join(match.group(1).split())
            if re.match(r"^(?:s|a|z|m)\s?\d", model, flags=re.IGNORECASE):
                model = f"Galaxy {model}"
            elif re.match(r"^(?:flip|fold)\s?\d", model, flags=re.IGNORECASE):
                model = f"Galaxy {model}"
            return model
    return None


def read_catalog_rows() -> list[dict[str, str]]:
    with CATALOG_PATH.open(encoding="utf-8-sig", newline="") as file:
        return list(csv.DictReader(file))
