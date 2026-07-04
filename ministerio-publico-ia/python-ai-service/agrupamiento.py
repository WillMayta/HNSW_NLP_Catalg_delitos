# -*- coding: utf-8 -*-
"""
Motor de Agrupamiento Inteligente.

Principio de diseño (según especificación): el sistema NO asigna
automáticamente un delito genérico definitivo. Solo PROPONE agrupaciones
de variantes similares para que un especialista jurídico las valide.

Estrategia:
  1. Para cada delito normalizado sin agrupar, se buscan sus k vecinos más
     cercanos en el índice HNSW.
  2. Se forman clusters (componentes conexos) uniendo registros cuya
     similitud supera un umbral configurable.
  3. Cada cluster se devuelve con su lista de variantes, un delito
     "representativo" sugerido (el más frecuente o el más corto/genérico)
     y un score de cohesión del grupo.
  4. El resultado es una PROPUESTA; el estado queda en "pendiente_validacion"
     hasta que un usuario con rol jurídico la apruebe o rechace.
"""
from collections import defaultdict
import numpy as np


UMBRAL_SIMILITUD_DEFECTO = 80.0  # porcentaje


class MotorAgrupamiento:
    def __init__(self, motor_hnsw, umbral_similitud: float = UMBRAL_SIMILITUD_DEFECTO):
        self.motor_hnsw = motor_hnsw
        self.umbral_similitud = umbral_similitud

    def proponer_agrupaciones(self, registros: list, vectores: np.ndarray,
                               k_vecinos: int = 8):
        """
        registros: lista de dicts con al menos {id, texto_normalizado}
        vectores: np.ndarray alineado posicionalmente con `registros`
        """
        # Unión de conjuntos (union-find) para formar clusters por
        # transitividad de similitud
        padre = {r["id"]: r["id"] for r in registros}

        def encontrar(x):
            while padre[x] != x:
                x = padre[x]
            return x

        def unir(x, y):
            rx, ry = encontrar(x), encontrar(y)
            if rx != ry:
                padre[rx] = ry

        id_a_registro = {r["id"]: r for r in registros}

        for registro, vector in zip(registros, vectores):
            vecinos = self.motor_hnsw.buscar_knn(vector, k=k_vecinos + 1)
            for id_vecino, similitud in vecinos:
                if id_vecino == registro["id"]:
                    continue
                if id_vecino not in id_a_registro:
                    continue
                if similitud >= self.umbral_similitud:
                    unir(registro["id"], id_vecino)

        # Agrupar por raíz del union-find
        grupos = defaultdict(list)
        for r in registros:
            raiz = encontrar(r["id"])
            grupos[raiz].append(r)

        propuestas = []
        for raiz, miembros in grupos.items():
            if len(miembros) < 2:
                continue  # un solo elemento no es una "agrupación"

            # delito representativo sugerido: el texto normalizado más
            # frecuente en el grupo; en caso de empate, el más corto
            conteo = defaultdict(int)
            for m in miembros:
                conteo[m["texto_normalizado"]] += 1
            max_conteo = max(conteo.values())
            candidatos = [t for t, c in conteo.items() if c == max_conteo]
            representativo = min(candidatos, key=len)

            cohesion = self._calcular_cohesion(miembros, vectores, registros)

            propuestas.append({
                "id_propuesta": f"prop_{raiz}",
                "delito_representativo_sugerido": representativo,
                "cantidad_variantes": len(miembros),
                "variantes": [
                    {"id": m["id"], "texto_original": m.get("texto_original", ""),
                     "texto_normalizado": m["texto_normalizado"]}
                    for m in miembros
                ],
                "cohesion_promedio": cohesion,
                "estado": "pendiente_validacion_juridica",
            })

        propuestas.sort(key=lambda p: p["cantidad_variantes"], reverse=True)
        return propuestas

    def _calcular_cohesion(self, miembros, vectores, registros_todos):
        idx_por_id = {r["id"]: i for i, r in enumerate(registros_todos)}
        vecs_grupo = np.array([vectores[idx_por_id[m["id"]]] for m in miembros])
        if len(vecs_grupo) < 2:
            return 100.0
        centroide = vecs_grupo.mean(axis=0)
        centroide_norm = centroide / (np.linalg.norm(centroide) + 1e-9)
        similitudes = [
            float(np.dot(v / (np.linalg.norm(v) + 1e-9), centroide_norm))
            for v in vecs_grupo
        ]
        return round(float(np.mean(similitudes)) * 100, 2)
