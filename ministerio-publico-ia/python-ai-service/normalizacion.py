# -*- coding: utf-8 -*-
"""
Motor de Normalización Léxica.

Limpia y estandariza la denominación de un delito sin perder el texto
original (siempre se conserva tx_original junto con tx_normalizado).

Reglas aplicadas (configurables vía listas/diccionarios, pensadas para
poblarse luego desde tablas de parámetros en PostgreSQL):
  1. Conversión a mayúsculas.
  2. Eliminación de tildes para comparación robusta (se conserva versión
     con tildes en el campo "visual" y sin tildes en el campo "comparable").
  3. Eliminación de caracteres especiales no relevantes.
  4. Eliminación de contenido entre paréntesis (detalles circunstanciales
     que generan ruido en el embedding, ej. "(AGENTE ACTUA EN ESTADO DE...)").
  5. Eliminación de referencias a artículos de ley (ART. 376, ART 121, etc.).
  6. Eliminación de dobles espacios.
  7. Eliminación de palabras irrelevantes / stopwords jurídicas configurables
     (Y OTROS, ART, INC., ETC.).
  8. Unificación de abreviaturas comunes (C.F.P. -> CONTRA LA FE PUBLICA, etc.)
"""
import re
import unicodedata
from dataclasses import dataclass


# --- Parámetros configurables (en producción: tabla parametro_normalizacion) ---

ABREVIATURAS = {
    r"\bC\.F\.P\.?\b": "CONTRA LA FE PUBLICA",
    r"\bV\.L\.P\.?\b": "VIOLENCIA CONTRA LA LIBERTAD PERSONAL",
    r"\bO\.A\.F\.?\b": "OMISION DE ASISTENCIA FAMILIAR",
    r"\bART\.?\s*\d+[\-A-Z]*\b": "",
    r"\bCP\b": "",
    r"\bC\.P\.?\b": "",
}

PALABRAS_IRRELEVANTES = {
    "Y", "OTROS", "OTRO", "DE", "DEL", "LA", "EL", "LOS", "LAS",
    "EN", "A", "CON", "POR", "SU", "SUS",
}
# Nota: el motor solo elimina estas palabras cuando aparecen como ruido al
# final/aislado, no se aplica indiscriminadamente para no destruir el
# significado jurídico (ABUSO DE AUTORIDAD necesita el "DE").

PATRON_PARENTESIS = re.compile(r"\([^)]*\)")
PATRON_ARTICULO = re.compile(r"\bART[ICULO]*\.?\s*\d+[\-A-Z]*\.?", re.IGNORECASE)
PATRON_CARACTERES_ESPECIALES = re.compile(r"[^A-Za-zÁÉÍÓÚÑáéíóúñ0-9\s]")
PATRON_ESPACIOS = re.compile(r"\s+")
SUFIJOS_RUIDO = re.compile(r"\b(Y OTROS?|ETC\.?|ENTRE OTROS)\b", re.IGNORECASE)


@dataclass
class ResultadoNormalizacion:
    texto_original: str
    texto_normalizado: str
    texto_comparable: str  # sin tildes, para indexado/comparación
    reglas_aplicadas: list


def quitar_tildes(texto: str) -> str:
    nfkd = unicodedata.normalize("NFKD", texto)
    return "".join(c for c in nfkd if not unicodedata.combining(c))


def _extraer_contenido_parentesis(t: str) -> str:
    """Devuelve el contenido entre paréntesis concatenado (sin los paréntesis)."""
    contenidos = PATRON_PARENTESIS.findall(t)
    return " ".join(c.strip("() ") for c in contenidos)


def normalizar(texto: str) -> ResultadoNormalizacion:
    if texto is None:
        texto = ""
    original = texto
    reglas = []
    t = texto.upper()
    reglas.append("mayusculas")

    if PATRON_PARENTESIS.search(t):
        fuera_parentesis = PATRON_PARENTESIS.sub(" ", t).strip()
        dentro_parentesis = _extraer_contenido_parentesis(t)
        # Heurística: si el texto fuera de paréntesis es muy corto
        # (ej. "C.F.P.", "V.L.P.") es una sigla/clasificación genérica y el
        # contenido jurídicamente relevante está DENTRO del paréntesis, por
        # lo que se prioriza ese contenido en vez de descartarlo.
        if len(fuera_parentesis.replace(".", "").strip()) <= 6 and dentro_parentesis:
            t = dentro_parentesis
            reglas.append("priorizacion_contenido_parentesis")
        else:
            # Caso general: el paréntesis aporta detalle circunstancial que
            # genera ruido para el embedding (agravantes extensas, etc.)
            t = fuera_parentesis
            reglas.append("eliminacion_parentesis")

    if PATRON_ARTICULO.search(t):
        t = PATRON_ARTICULO.sub(" ", t)
        reglas.append("eliminacion_articulos_ley")

    for patron, reemplazo in ABREVIATURAS.items():
        if re.search(patron, t):
            t = re.sub(patron, reemplazo, t)
            reglas.append(f"unificacion_abreviatura:{patron}")

    if SUFIJOS_RUIDO.search(t):
        t = SUFIJOS_RUIDO.sub(" ", t)
        reglas.append("eliminacion_ruido_textual")

    t_sin_especiales = PATRON_CARACTERES_ESPECIALES.sub(" ", t)
    if t_sin_especiales != t:
        reglas.append("eliminacion_caracteres_especiales")
    t = t_sin_especiales

    t = PATRON_ESPACIOS.sub(" ", t).strip()
    reglas.append("eliminacion_dobles_espacios")

    comparable = quitar_tildes(t)

    return ResultadoNormalizacion(
        texto_original=original,
        texto_normalizado=t,
        texto_comparable=comparable,
        reglas_aplicadas=reglas,
    )


if __name__ == "__main__":
    pruebas = [
        "ABUSO DE AUTORIDAD ART.376",
        "FEMINICIDIO - CIRCUNSTANCIA AGRAVANTE (AGENTE ACTUA EN ESTADO DE EBRIEDAD, CON PRESENCIA DE ALCOHOL)",
        "C.F.P. (FALSIFICACION DOCUMENTARIA)",
        "USURPACION (AGENTE CON VIOLENCIA, AMENAZA, ENGAÑA O ABUSO DE CONFIANZA, DESPOJA TOTAL O PARCIALME...)",
    ]
    for p in pruebas:
        r = normalizar(p)
        print(f"ORIGINAL : {r.texto_original}")
        print(f"NORMAL.  : {r.texto_normalizado}")
        print(f"REGLAS   : {r.reglas_aplicadas}")
        print("-" * 80)
