# -*- coding: utf-8 -*-
"""
Motor Vectorial HNSW.

Responsabilidades (según especificación del documento de arquitectura):
  - construir el índice
  - insertar registros
  - actualizar registros
  - reconstruir el índice
  - realizar búsquedas KNN
  - devolver porcentaje de similitud

Implementado con la librería `hnswlib` (HNSW = Hierarchical Navigable
Small World), que es el mismo algoritmo que usa la extensión `pgvector`
de PostgreSQL bajo `CREATE INDEX ... USING hnsw`. Esto permite que esta
capa sea conceptualmente intercambiable: en producción con PostgreSQL +
pgvector, esta clase se reemplaza por consultas SQL con el operador
`<=>` (distancia coseno) sin cambiar la lógica de negocio que la consume.

Persistencia: el índice se guarda en disco (.bin) junto con un mapeo
id_interno -> id_delito_original para poder reconstruirlo o actualizarlo.
"""
import json
import os
import threading
import numpy as np
import hnswlib


class MotorHNSW:
    def __init__(self, dim: int, ruta_indice: str, espacio: str = "cosine",
                 ef_construccion: int = 200, m: int = 16):
        self.dim = dim
        self.ruta_indice = ruta_indice
        self.ruta_meta = ruta_indice + ".meta.json"
        self.espacio = espacio
        self.ef_construccion = ef_construccion
        self.m = m
        self._lock = threading.RLock()
        self.index = None
        # mapeos entre el id interno secuencial de hnswlib (entero) y el
        # id_delito_normalizado (string/uuid) del dominio de negocio
        self.id_interno_a_id_delito = {}
        self.id_delito_a_id_interno = {}
        self._siguiente_id_interno = 0
        self._cargar_si_existe()

    # ---------- construcción / persistencia ----------

    def construir_desde_cero(self, capacidad_maxima: int = 200_000):
        self.index = hnswlib.Index(space=self.espacio, dim=self.dim)
        self.index.init_index(
            max_elements=capacidad_maxima,
            ef_construction=self.ef_construccion,
            M=self.m,
        )
        self.index.set_ef(64)
        self.id_interno_a_id_delito = {}
        self.id_delito_a_id_interno = {}
        self._siguiente_id_interno = 0

    def _cargar_si_existe(self):
        if os.path.exists(self.ruta_indice) and os.path.exists(self.ruta_meta):
            with open(self.ruta_meta, "r", encoding="utf-8") as f:
                meta = json.load(f)
            self.id_interno_a_id_delito = {int(k): v for k, v in meta["mapa"].items()}
            self.id_delito_a_id_interno = {v: int(k) for k, v in meta["mapa"].items()}
            self._siguiente_id_interno = meta["siguiente_id"]
            self.index = hnswlib.Index(space=self.espacio, dim=self.dim)
            self.index.load_index(self.ruta_indice, max_elements=meta.get("capacidad", 200_000))
            self.index.set_ef(64)
        else:
            self.construir_desde_cero()

    def guardar(self):
        with self._lock:
            os.makedirs(os.path.dirname(self.ruta_indice) or ".", exist_ok=True)
            self.index.save_index(self.ruta_indice)
            meta = {
                "mapa": {str(k): v for k, v in self.id_interno_a_id_delito.items()},
                "siguiente_id": self._siguiente_id_interno,
                "capacidad": self.index.get_max_elements(),
            }
            with open(self.ruta_meta, "w", encoding="utf-8") as f:
                json.dump(meta, f, ensure_ascii=False)

    def reconstruir_indice(self, ids_delito: list, vectores: np.ndarray,
                            capacidad_maxima: int = 200_000):
        """Reconstruye el índice completo desde cero a partir de todos los
        embeddings vigentes. Se usa tras procesos de aprendizaje continuo
        o cargas masivas grandes."""
        with self._lock:
            self.construir_desde_cero(capacidad_maxima=max(capacidad_maxima, len(ids_delito) + 1000))
            self._insertar_lote_interno(ids_delito, vectores)
            self.guardar()

    # ---------- inserción / actualización ----------

    def _insertar_lote_interno(self, ids_delito: list, vectores: np.ndarray):
        ids_internos = []
        for id_delito in ids_delito:
            nuevo_id = self._siguiente_id_interno
            self.id_interno_a_id_delito[nuevo_id] = id_delito
            self.id_delito_a_id_interno[id_delito] = nuevo_id
            ids_internos.append(nuevo_id)
            self._siguiente_id_interno += 1
        self.index.add_items(vectores, np.array(ids_internos))

    def insertar(self, id_delito: str, vector: np.ndarray):
        with self._lock:
            if id_delito in self.id_delito_a_id_interno:
                # ya existe -> tratar como actualización
                self._actualizar_interno(id_delito, vector)
            else:
                self._insertar_lote_interno([id_delito], vector.reshape(1, -1))
            self.guardar()

    def insertar_lote(self, ids_delito: list, vectores: np.ndarray):
        with self._lock:
            self._insertar_lote_interno(ids_delito, vectores)
            self.guardar()

    def _actualizar_interno(self, id_delito: str, vector: np.ndarray):
        # hnswlib no soporta update in-place de forma nativa en todas las
        # versiones; estrategia segura: marcar el punto antiguo como
        # eliminado (mark_deleted) e insertar uno nuevo con nuevo id interno.
        id_interno_viejo = self.id_delito_a_id_interno[id_delito]
        try:
            self.index.mark_deleted(id_interno_viejo)
        except Exception:
            pass
        del self.id_interno_a_id_delito[id_interno_viejo]
        self._insertar_lote_interno([id_delito], vector.reshape(1, -1))

    def actualizar(self, id_delito: str, vector: np.ndarray):
        with self._lock:
            self._actualizar_interno(id_delito, vector)
            self.guardar()

    def eliminar(self, id_delito: str):
        with self._lock:
            if id_delito in self.id_delito_a_id_interno:
                id_interno = self.id_delito_a_id_interno.pop(id_delito)
                self.id_interno_a_id_delito.pop(id_interno, None)
                try:
                    self.index.mark_deleted(id_interno)
                except Exception:
                    pass
                self.guardar()

    # ---------- búsqueda ----------

    def buscar_knn(self, vector_consulta: np.ndarray, k: int = 10):
        """
        Devuelve lista de tuplas (id_delito, porcentaje_similitud) ordenadas
        de mayor a menor similitud. Distancia coseno de hnswlib -> se
        convierte a porcentaje de similitud: similitud = 1 - distancia.
        """
        if self.index.get_current_count() == 0:
            return []
        k_efectivo = min(k, self.index.get_current_count())
        labels, distancias = self.index.knn_query(
            vector_consulta.reshape(1, -1), k=k_efectivo
        )
        resultados = []
        for id_interno, dist in zip(labels[0], distancias[0]):
            id_delito = self.id_interno_a_id_delito.get(int(id_interno))
            if id_delito is None:
                continue
            similitud = max(0.0, 1.0 - float(dist))  # cosine distance -> similitud
            resultados.append((id_delito, round(similitud * 100, 2)))
        return resultados

    def cantidad_registros(self) -> int:
        return self.index.get_current_count() if self.index else 0


if __name__ == "__main__":
    # Prueba rápida del motor HNSW de forma aislada
    np.random.seed(0)
    dim = 16
    motor = MotorHNSW(dim=dim, ruta_indice="/tmp/test_hnsw/indice.bin")
    ids = [f"delito_{i}" for i in range(5)]
    vecs = np.random.rand(5, dim).astype(np.float32)
    motor.insertar_lote(ids, vecs)
    print("Registros en índice:", motor.cantidad_registros())
    resultados = motor.buscar_knn(vecs[0], k=3)
    print("Resultados KNN para delito_0:", resultados)
