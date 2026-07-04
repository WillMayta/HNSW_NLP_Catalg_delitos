# Sistema Inteligente de Normalización, Agrupación Semántica y Consolidación
## Catálogo de Delitos — Ministerio Público del Perú

MVP funcional del sistema descrito en el documento de arquitectura: normalización
léxica, embeddings (Sentence Transformers), búsqueda vectorial HNSW, agrupamiento
inteligente con validación jurídica obligatoria, y catálogo penal consolidado.

## Qué se entrega y qué se verificó en este entorno

Por restricciones de red del entorno donde se generó este proyecto (sin acceso
a `huggingface.co` ni a los repositorios de Microsoft/.NET), las tres piezas
tuvieron distinto grado de verificación:

| Componente | Estado |
|---|---|
| Microservicio Python (normalización, HNSW, agrupamiento) | **Probado end-to-end** con los 300 registros de muestra (`data/delitos_muestra.csv`), corriendo el servidor FastAPI real. |
| Frontend React | **Compilado exitosamente** (`npm run build` sin errores) y servido en modo desarrollo. Todos los íconos y dependencias verificados. |
| Backend .NET 8 | Código completo y revisado manualmente (Clean Architecture, Repository Pattern, JWT, EF Core), **no compilado** en este entorno por no contar con el SDK de .NET. Debe compilarse y probarse en su máquina con `dotnet build`. |

Recomiendo que, al recibir el proyecto, lo primero que haga sea `dotnet build`
sobre la solución para detectar cualquier error de compilación que no pude
verificar aquí, antes de continuar con el resto de la puesta en marcha.

## Estructura del proyecto

```
ministerio-publico-ia/
├── python-ai-service/      # Motor de IA: normalización, embeddings, HNSW, agrupamiento
├── backend/                # API .NET 8 (Clean Architecture)
│   └── src/
│       ├── MinisterioPublico.Domain/
│       ├── MinisterioPublico.Application/
│       ├── MinisterioPublico.Infrastructure/
│       └── MinisterioPublico.API/
├── frontend/                # React + MUI + Tailwind
├── data/                    # Datos de muestra (300 registros sintéticos)
└── docs/                    # (pendiente: diagramas UML, C4, diccionario de datos)
```

## 1. Puesta en marcha del Motor de IA (Python)

```bash
cd python-ai-service
python3 -m venv venv
source venv/bin/activate          # Windows: venv\Scripts\activate
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8001 --reload
```

**Importante — primera ejecución:** el sistema requiere conectividad a
`huggingface.co` para descargar el modelo `all-MiniLM-L6-v2` (~90 MB). Si su
servidor no tiene salida a internet, descargue el modelo desde una máquina
con acceso y cópielo a `~/.cache/huggingface/` en el servidor de destino, o
empaquételo dentro de la imagen Docker.

Si no hay conectividad, el sistema cae automáticamente a un motor de respaldo
TF-IDF (ver advertencia impresa en consola) que permite seguir operando con
calidad semántica reducida — **no usar en producción**, solo como contingencia.

Verifique que el servicio esté activo:
```bash
curl http://localhost:8001/salud
```

## 2. Puesta en marcha del Backend (.NET 8)

Requiere [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) y,
para producción, PostgreSQL 15+ con la extensión `pgvector`.

```bash
cd backend
dotnet restore
dotnet build
```

### Modo demo (SQLite, sin instalar PostgreSQL)

El proyecto viene configurado por defecto con SQLite para que pueda probarlo
sin instalar un servidor de base de datos:

```bash
cd src/MinisterioPublico.API
dotnet run
```

La base de datos SQLite se crea automáticamente (`EnsureCreated`) con datos
semilla: roles, familias delictivas del Código Penal, y un usuario
administrador (`admin` / `Admin#2026` — **cámbielo de inmediato**).

Swagger disponible en: `https://localhost:7xxx/swagger` (el puerto exacto lo
indica la consola al iniciar).

### Migración a PostgreSQL + pgvector (producción)

1. Instale PostgreSQL 15+ y la extensión `pgvector`:
   ```sql
   CREATE EXTENSION IF NOT EXISTS vector;
   ```
2. En `MinisterioPublico.Infrastructure.csproj`, reemplace el paquete
   `Microsoft.EntityFrameworkCore.Sqlite` por:
   ```xml
   <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />
   <PackageReference Include="Pgvector.EntityFrameworkCore" Version="0.2.0" />
   ```
3. En `Program.cs`, cambie `UseSqlite(...)` por `UseNpgsql(...)` apuntando a
   la cadena de conexión `PostgreSql` de `appsettings.json`.
4. En `MinisterioPublicoDbContext.OnModelCreating`, mapee `Embedding.Vector`
   como `HasColumnType("vector(384)")` en vez de la conversión JSON actual,
   y cree el índice HNSW nativo:
   ```sql
   CREATE INDEX idx_embeddings_hnsw ON embeddings
       USING hnsw (vector vector_cosine_ops);
   ```
5. Ejecute migraciones formales en vez de `EnsureCreated`:
   ```bash
   dotnet ef migrations add InicialPostgres
   dotnet ef database update
   ```

Esto reemplaza el índice HNSW gestionado por `hnswlib` en el microservicio
Python por el índice HNSW nativo de `pgvector`, manteniendo la misma lógica
de negocio (es la migración prevista desde el diseño original).

## 3. Puesta en marcha del Frontend (React)

Requiere Node.js 18+.

```bash
cd frontend
npm install
cp .env.example .env     # ajuste VITE_API_BASE_URL a la URL real de su API .NET
npm run dev
```

La aplicación se sirve en `http://localhost:5173`.

## 4. Datos de muestra

`data/delitos_muestra.csv` contiene 300 registros sintéticos generados con
la misma variabilidad observada en sus ejemplos reales (abreviaturas,
artículos de ley, paréntesis con detalle circunstancial, etc.), cubriendo
10 familias de delitos. Se generaron con `data/generar_muestra.py` porque
aún no contábamos con el archivo real de ~50,000 registros.

**Para usar sus datos reales:** suba el Excel/CSV real desde el módulo
"Carga de expedientes" del frontend, respetando las columnas del documento
original (`de_esp`, `tx_descripcion`, `tx_tipo_caso`, `id_unico`,
`fe_ing_caso`, `de_mat_deli`, `ca_foli`, `de_estado`, `st_acumulado`,
`situacion`, `de_sigl_mpub`, `fiscal`).

## 5. Validación del pipeline (lo que sí se ejecutó en este entorno)

Con el motor de respaldo TF-IDF (sin acceso a Hugging Face), el pipeline
completo normalización → embeddings → HNSW → agrupamiento agrupó
correctamente las 10 familias de delitos de la muestra con 94-100% de
cohesión interna. Con el modelo real `all-MiniLM-L6-v2` (que usted ejecutará
con conectividad normal a internet), la calidad semántica será superior,
especialmente para variantes léxicamente distintas con el mismo significado
jurídico (ej. "FALSIFICACIÓN DOCUMENTARIA" vs "USO DE DOCUMENTO FALSO").

## Pendientes para llevar esto a producción real

Este MVP cubre el núcleo funcional del pipeline (los 9 pasos descritos en el
documento de arquitectura) y los módulos de Seguridad, Carga, Normalización,
Búsqueda, Agrupamiento, Validación y Catálogo. Quedan pendientes, en orden
sugerido de prioridad:

1. Migración real a PostgreSQL + pgvector (pasos arriba).
2. Pruebas unitarias y de integración (xUnit + Moq para .NET, pytest para Python).
3. Módulo de Reportes (PDF/Excel/CSV) y Administración avanzada de usuarios/roles/permisos.
4. Diagramas formales: C4, UML, ER, diccionario de datos (carpeta `docs/`).
5. Endpoint para gestionar `ParametroNormalizacion` desde la UI (actualmente
   las reglas del motor de normalización están en código, según lo
   especificado, pero el documento pide que sean editables vía tablas).
6. Endpoint `/api/FamiliasDelictivas` (el frontend actualmente usa una lista
   fija de ejemplo en el diálogo de validación).
7. Reconstrucción periódica/programada del índice HNSW tras lotes grandes
   de validaciones (aprendizaje continuo).
8. Endurecimiento de seguridad: rotación de la clave JWT, rate limiting,
   HTTPS obligatorio, CORS restringido al dominio real de producción.
