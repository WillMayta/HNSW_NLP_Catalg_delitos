# -*- coding: utf-8 -*-
"""
Microservicio de IA - Ministerio Público del Perú
Sistema Inteligente de Normalización, Agrupación Semántica y Consolidación
del Catálogo de Delitos.

Expone vía REST:
  POST /normalizar              -> normaliza un texto o lote de textos
  POST /embeddings/generar      -> genera embeddings para una lista de textos
  POST /indice/construir        -> (re)construye el índice HNSW desde cero
  POST /indice/insertar         -> inserta un nuevo registro en el índice
  PUT  /indice/actualizar       -> actualiza un registro existente
  POST /busqueda                -> búsqueda KNN de un delito por texto
  POST /agrupamiento/proponer   -> genera propuestas de agrupación
  GET  /salud                   -> healthcheck + info del motor activo

Ejecutar con:  uvicorn main:app --host 0.0.0.0 --port 8001 --reload
"""
import os
from typing import List, Optional

import numpy as np
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

from normalizacion import normalizar
from embeddings import crear_motor_embeddings
from motor_hnsw import MotorHNSW
from agrupamiento import MotorAgrupamiento, UMBRAL_SIMILITUD_DEFECTO

app = FastAPI(
    title="Motor de IA - Catálogo de Delitos MP Perú",
    description="Normalización léxica, embeddings, búsqueda vectorial HNSW y agrupamiento inteligente.",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # restringir en producción al dominio del backend .NET
    allow_methods=["*"],
    allow_headers=["*"],
)

RUTA_DATOS = os.environ.get("RUTA_DATOS_IA", "/home/claude/ministerio-publico-ia/data")
RUTA_INDICE = os.path.join(RUTA_DATOS, "indices", "hnsw_delitos.bin")

motor_embeddings, tipo_motor_embeddings = crear_motor_embeddings()
motor_hnsw: Optional[MotorHNSW] = None  # se inicializa al conocer la dimensión real
motor_agrupamiento: Optional[MotorAgrupamiento] = None


def _asegurar_motor_hnsw(dim: int):
    global motor_hnsw, motor_agrupamiento
    if motor_hnsw is None:
        motor_hnsw = MotorHNSW(dim=dim, ruta_indice=RUTA_INDICE)
        motor_agrupamiento = MotorAgrupamiento(motor_hnsw)
    return motor_hnsw


# ---------------- Esquemas ----------------

class TextoIn(BaseModel):
    texto: str


class LoteTextosIn(BaseModel):
    textos: List[str]


class RegistroDelito(BaseModel):
    id: str
    texto: str


class LoteRegistrosIn(BaseModel):
    registros: List[RegistroDelito]


class InsertarIn(BaseModel):
    id: str
    texto: str


class ActualizarIn(BaseModel):
    id: str
    texto: str


class BusquedaIn(BaseModel):
    texto: str
    k: int = 10


class AgrupamientoIn(BaseModel):
    registros: List[RegistroDelito]
    umbral_similitud: float = UMBRAL_SIMILITUD_DEFECTO
    k_vecinos: int = 8


# ---------------- Endpoints ----------------

@app.get("/salud")
def salud():
    return {
        "estado": "ok",
        "motor_embeddings": tipo_motor_embeddings,
        "dimension_embeddings": motor_embeddings.dim,
        "registros_en_indice": motor_hnsw.cantidad_registros() if motor_hnsw else 0,
        "advertencia": (
            None if tipo_motor_embeddings == "sentence-transformers" else
            "Usando motor TF-IDF de respaldo: no hay acceso a Hugging Face. "
            "Calidad semántica reducida respecto a Sentence Transformers. "
            "Verifique conectividad en el entorno de producción."
        ),
    }


@app.post("/normalizar")
def endpoint_normalizar(payload: LoteTextosIn):
    resultados = []
    for texto in payload.textos:
        r = normalizar(texto)
        resultados.append({
            "texto_original": r.texto_original,
            "texto_normalizado": r.texto_normalizado,
            "texto_comparable": r.texto_comparable,
            "reglas_aplicadas": r.reglas_aplicadas,
        })
    return {"resultados": resultados}


@app.post("/embeddings/generar")
def endpoint_generar_embeddings(payload: LoteTextosIn):
    if not payload.textos:
        raise HTTPException(400, "La lista de textos no puede estar vacía.")
    vectores = motor_embeddings.encode(payload.textos)
    _asegurar_motor_hnsw(vectores.shape[1])
    return {
        "dimension": int(vectores.shape[1]),
        "embeddings": vectores.tolist(),
        "motor_usado": tipo_motor_embeddings,
    }


@app.post("/indice/construir")
def endpoint_construir_indice(payload: LoteRegistrosIn):
    if not payload.registros:
        raise HTTPException(400, "Se requiere al menos un registro para construir el índice.")
    textos = [r.texto for r in payload.registros]
    ids = [r.id for r in payload.registros]
    vectores = motor_embeddings.encode(textos)
    motor = _asegurar_motor_hnsw(vectores.shape[1])
    motor.reconstruir_indice(ids, vectores, capacidad_maxima=len(ids) + 5000)
    return {"mensaje": "Índice HNSW construido correctamente.", "registros_indexados": len(ids)}


@app.post("/indice/insertar")
def endpoint_insertar(payload: InsertarIn):
    if motor_hnsw is None:
        raise HTTPException(409, "El índice aún no ha sido construido. Use /indice/construir primero.")
    vector = motor_embeddings.encode([payload.texto])[0]
    motor_hnsw.insertar(payload.id, vector)
    return {"mensaje": "Registro insertado.", "id": payload.id}


@app.put("/indice/actualizar")
def endpoint_actualizar(payload: ActualizarIn):
    if motor_hnsw is None:
        raise HTTPException(409, "El índice aún no ha sido construido.")
    vector = motor_embeddings.encode([payload.texto])[0]
    motor_hnsw.actualizar(payload.id, vector)
    return {"mensaje": "Registro actualizado.", "id": payload.id}


@app.delete("/indice/eliminar/{id_delito}")
def endpoint_eliminar(id_delito: str):
    if motor_hnsw is None:
        raise HTTPException(409, "El índice aún no ha sido construido.")
    motor_hnsw.eliminar(id_delito)
    return {"mensaje": "Registro eliminado del índice.", "id": id_delito}


@app.post("/busqueda")
def endpoint_busqueda(payload: BusquedaIn):
    if motor_hnsw is None or motor_hnsw.cantidad_registros() == 0:
        raise HTTPException(409, "El índice está vacío. Construya el índice primero.")
    r = normalizar(payload.texto)
    vector = motor_embeddings.encode([r.texto_normalizado])[0]
    resultados = motor_hnsw.buscar_knn(vector, k=payload.k)
    return {
        "texto_consultado": payload.texto,
        "texto_normalizado": r.texto_normalizado,
        "resultados": [
            {"id_delito": id_d, "porcentaje_similitud": sim}
            for id_d, sim in resultados
        ],
    }


@app.post("/agrupamiento/proponer")
def endpoint_proponer_agrupamiento(payload: AgrupamientoIn):
    if not payload.registros:
        raise HTTPException(400, "Se requieren registros para proponer agrupaciones.")
    textos = [r.texto for r in payload.registros]
    textos_normalizados = [normalizar(t).texto_normalizado for t in textos]
    vectores = motor_embeddings.encode(textos_normalizados)
    motor = _asegurar_motor_hnsw(vectores.shape[1])

    ids = [r.id for r in payload.registros]
    motor.reconstruir_indice(ids, vectores, capacidad_maxima=len(ids) + 5000)

    registros_dict = [
        {"id": r.id, "texto_original": r.texto, "texto_normalizado": tn}
        for r, tn in zip(payload.registros, textos_normalizados)
    ]
    agrupador = MotorAgrupamiento(motor, umbral_similitud=payload.umbral_similitud)
    propuestas = agrupador.proponer_agrupaciones(registros_dict, vectores, k_vecinos=payload.k_vecinos)
    return {"cantidad_propuestas": len(propuestas), "propuestas": propuestas}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8001)
