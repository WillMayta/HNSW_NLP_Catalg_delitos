# -*- coding: utf-8 -*-
"""
Motor de Embeddings.

Usa Sentence Transformers (modelo all-MiniLM-L6-v2, 384 dimensiones) según
especifica el documento de arquitectura.

IMPORTANTE PARA DESPLIEGUE EN PRODUCCION:
La primera ejecución requiere conexión a internet para descargar el modelo
desde Hugging Face (~90 MB). Luego queda cacheado en
~/.cache/huggingface/ y no vuelve a requerir red.

Si el entorno de ejecución no tiene salida a internet (ej. sandbox aislado,
servidor on-premise sin acceso externo), este módulo cae automáticamente a
un EmbeddingEngineTFIDF como mecanismo de respaldo, para que el pipeline
completo (normalización -> embeddings -> HNSW -> búsqueda -> agrupamiento)
se pueda demostrar igual. En ese caso se imprime una advertencia clara.

Para producción real se recomienda:
  1. Descargar el modelo una vez con conexión a internet.
  2. O empaquetarlo en la imagen Docker (COPY del cache de HF).
"""
import os
import numpy as np

MODEL_NAME = "all-MiniLM-L6-v2"
EMBEDDING_DIM_TRANSFORMER = 384


class EmbeddingEngineTransformer:
    """Motor real, usando Sentence Transformers (uso en producción)."""

    def __init__(self, model_name: str = MODEL_NAME):
        from sentence_transformers import SentenceTransformer
        self.model = SentenceTransformer(model_name)
        self.dim = self.model.get_sentence_embedding_dimension()

    def encode(self, textos):
        return self.model.encode(
            textos, normalize_embeddings=True, show_progress_bar=False
        ).astype(np.float32)


class EmbeddingEngineTFIDF:
    """
    Motor de respaldo (NO usar en producción) basado en TF-IDF + reducción
    de dimensionalidad, para entornos sin acceso a Hugging Face. Permite
    demostrar el pipeline completo de búsqueda vectorial HNSW de forma
    funcional aunque con menor calidad semántica que un modelo transformer.
    """

    def __init__(self, dim: int = 128):
        from sklearn.feature_extraction.text import TfidfVectorizer
        from sklearn.decomposition import TruncatedSVD
        self.dim = dim
        self.vectorizer = TfidfVectorizer(analyzer="char_wb", ngram_range=(2, 4))
        self.svd = TruncatedSVD(n_components=dim, random_state=42)
        self._fitted = False

    def fit(self, corpus):
        tfidf = self.vectorizer.fit_transform(corpus)
        n_comp = min(self.dim, tfidf.shape[0] - 1, tfidf.shape[1] - 1)
        if n_comp < self.dim:
            from sklearn.decomposition import TruncatedSVD
            self.svd = TruncatedSVD(n_components=max(n_comp, 2), random_state=42)
            self.dim = self.svd.n_components
        self.svd.fit(tfidf)
        self._fitted = True

    def encode(self, textos):
        if not self._fitted:
            self.fit(textos)
        tfidf = self.vectorizer.transform(textos)
        vecs = self.svd.transform(tfidf).astype(np.float32)
        norms = np.linalg.norm(vecs, axis=1, keepdims=True)
        norms[norms == 0] = 1
        return vecs / norms


def crear_motor_embeddings():
    """
    Factory: intenta cargar el motor real (Sentence Transformers). Si falla
    por falta de conectividad u otro error, cae al motor TF-IDF de respaldo
    e imprime una advertencia explícita.
    """
    try:
        motor = EmbeddingEngineTransformer()
        print(f"[OK] Motor de embeddings cargado: Sentence Transformers ({MODEL_NAME}), dim={motor.dim}")
        return motor, "sentence-transformers"
    except Exception as e:
        print("=" * 90)
        print("[ADVERTENCIA] No se pudo cargar Sentence Transformers / Hugging Face.")
        print(f"  Motivo: {e}")
        print("  Usando motor de respaldo TF-IDF SOLO PARA DEMOSTRACION DEL PIPELINE.")
        print("  En producción, asegúrese de tener conectividad a huggingface.co")
        print("  en el primer arranque, o pre-cargue el modelo en la imagen Docker.")
        print("=" * 90)
        motor = EmbeddingEngineTFIDF(dim=128)
        return motor, "tfidf-fallback"


if __name__ == "__main__":
    motor, tipo = crear_motor_embeddings()
    print("Motor activo:", tipo)
    textos = ["ABUSO DE AUTORIDAD", "ABUSO AUTORIDAD FUNCIONARIO", "FEMINICIDIO AGRAVADO"]
    vecs = motor.encode(textos)
    print("Shape embeddings:", vecs.shape)
