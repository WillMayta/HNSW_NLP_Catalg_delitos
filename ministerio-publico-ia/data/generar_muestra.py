# -*- coding: utf-8 -*-
"""
Generador de datos sintéticos de muestra que replican la variabilidad real
observada en el catálogo de delitos del Ministerio Público del Perú.
Se usa solo para poblar la demo mientras no se cuenta con el archivo real.
"""
import csv
import random
from datetime import datetime, timedelta

random.seed(42)

# Familias de delitos con sus variantes reales observadas (ortografía,
# abreviaturas, artículos de ley, palabras adicionales, etc.)
FAMILIAS = {
    "ABUSO DE AUTORIDAD": [
        "ABUSO DE AUTORIDAD",
        "ABUSO AUTORIDAD",
        "ABUSO DE AUTORIDAD ART.376",
        "ABUSO DE AUTORIDAD FUNCIONARIO",
        "ABUSO DE AUTORIDAD Y OTROS",
        "ABUSO DE AUTORIDAD (ART. 376 CP)",
        "ABUSO DE AUTORIDAD - FUNCIONARIO PUBLICO",
    ],
    "FALSIFICACION DOCUMENTARIA": [
        "FALSIFICACION DOCUMENTARIA",
        "USO DE DOCUMENTO FALSO",
        "FALSEDAD IDEOLOGICA",
        "DOCUMENTO PUBLICO FALSO",
        "C.F.P. (FALSIFICACION DOCUMENTARIA)",
        "C.F.P. (FALSEDAD GENERICA)",
        "FALSIFICACION DE DOCUMENTOS",
        "FALSEDAD GENERICA ART. 438",
    ],
    "FEMINICIDIO": [
        "FEMINICIDIO",
        "FEMINICIDIO - CIRCUNSTANCIA AGRAVANTE (AGENTE ACTUA EN ESTADO DE EBRIEDAD)",
        "FEMINICIDIO AGRAVADO",
        "FEMINICIDIO (TENTATIVA)",
        "FEMINICIDIO - CIRCUNSTANCIAS AGRAVANTES",
    ],
    "LESIONES GRAVES": [
        "LESIONES GRAVES",
        "LESIONES GRAVES (SEGUIDAS DE MUERTE)",
        "LESIONES GRAVES SEGUIDAS DE MUERTE CIRCUNSTANCIAS AGRAVANTES",
        "LESIONES GRAVES ART. 121",
        "LESIONES GRAVES - AGRAVADAS",
    ],
    "USURPACION": [
        "USURPACION",
        "USURPACION (AGENTE CON VIOLENCIA, AMENAZA, ENGANO O ABUSO DE CONFIANZA)",
        "USURPACION AGRAVADA",
        "USURPACION ART. 202",
        "USURPACION DE TERRENO",
    ],
    "ACOSO": [
        "ACOSO",
        "V.L.P. (ACOSO - AGENTE POR CUALQUIER MEDIO VIGILA, PERSIGUE, HOSTIGA)",
        "ACOSO SEXUAL",
        "HOSTIGAMIENTO - ACOSO",
        "ACOSO ART. 151-A",
    ],
    "HURTO SIMPLE": [
        "HURTO SIMPLE",
        "HURTO",
        "HURTO SIMPLE ART. 185",
        "HURTO DE BIENES MUEBLES",
        "HURTO SIMPLE Y OTROS",
    ],
    "ROBO AGRAVADO": [
        "ROBO AGRAVADO",
        "ROBO AGRAVADO ART. 189",
        "ROBO CON AGRAVANTES",
        "ROBO AGRAVADO (A MANO ARMADA)",
        "ROBO AGRAVADO - PLURALIDAD DE AGENTES",
    ],
    "VIOLENCIA FAMILIAR": [
        "VIOLENCIA FAMILIAR",
        "VIOLENCIA CONTRA LA MUJER E INTEGRANTES DEL GRUPO FAMILIAR",
        "VIOLENCIA FAMILIAR - PSICOLOGICA",
        "VIOLENCIA FAMILIAR ART. 122-B",
        "AGRESIONES EN EL AMBITO FAMILIAR",
    ],
    "OMISION DE ASISTENCIA FAMILIAR": [
        "OMISION DE ASISTENCIA FAMILIAR",
        "OMISION A LA ASISTENCIA FAMILIAR",
        "O.A.F.",
        "OMISION DE ASISTENCIA FAMILIAR ART. 149",
        "INCUMPLIMIENTO DE OBLIGACION ALIMENTARIA",
    ],
}

ESTADOS = [
    "ASIGNADO PNP (PRELIMINAR)",
    "CON INVESTIGACION PRELIMINAR",
    "FORMALIZA INVESTIGACION PREPARATORIA",
    "ARCHIVADO",
    "EN JUZGAMIENTO",
    "CON SENTENCIA",
]

FISCALIAS = [
    "1FPPC-PUNO", "2FPPC-PUNO", "3FPPC-PUNO", "1FPPC-JULIACA", "2FPPC-JULIACA",
    "1FPPC-AREQUIPA", "1FPPC-CUSCO", "1FPPC-LIMA NORTE", "1FPPC-LIMA SUR",
]

APELLIDOS = [
    "MAMANI CURASI", "MELENDRES QUISPE", "CONDORI ESCARCENA", "CHOQUE CHUQUIMIA",
    "MAMANI MASCO", "QUISPE HUANCA", "TICONA APAZA", "FLORES CONDORI",
    "HUANCA TITO", "APAZA MAMANI", "VILCA QUISPE", "CALLE PARI",
]
NOMBRES = [
    "RONALD BARTOLOME", "YESICA", "YESSY GIOVANNA", "SILVIA MARIBEL",
    "HIDALGO NEPTALI", "JUAN CARLOS", "MARIA ELENA", "JOSE LUIS",
    "ROSA MARIA", "LUIS ALBERTO", "CARMEN ROSA", "PEDRO PABLO",
]


def random_date(start_year=2010, end_year=2026):
    start = datetime(start_year, 1, 1)
    end = datetime(end_year, 6, 29)
    delta = end - start
    return start + timedelta(days=random.randint(0, delta.days))


def generar_id_unico(idx):
    return f"02{random.randint(70000,79999)}{random.randint(1000,9999)}{idx:013d}"


def generar_registros(n=300):
    registros = []
    familias_keys = list(FAMILIAS.keys())
    for i in range(1, n + 1):
        familia = random.choice(familias_keys)
        denominacion = random.choice(FAMILIAS[familia])
        apellido = random.choice(APELLIDOS)
        nombre = random.choice(NOMBRES)
        fecha = random_date()
        registros.append({
            "de_esp": "PENAL",
            "tx_descripcion": "DENUNCIA",
            "tx_tipo_caso": "D",
            "id_unico": generar_id_unico(i),
            "fe_ing_caso": fecha.strftime("%d/%m/%Y"),
            "de_mat_deli": denominacion,
            "ca_foli": random.randint(1, 300),
            "de_estado": random.choice(ESTADOS),
            "st_acumulado": "",
            "situacion": "T",
            "de_sigl_mpub": random.choice(FISCALIAS),
            "fiscal": f"{apellido} {nombre}",
            "familia_real": familia,  # columna de control, solo para validar el sistema
        })
    return registros


if __name__ == "__main__":
    data = generar_registros(300)
    out_path = "/home/claude/ministerio-publico-ia/data/delitos_muestra.csv"
    with open(out_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=list(data[0].keys()))
        writer.writeheader()
        writer.writerows(data)
    print(f"Generados {len(data)} registros en {out_path}")
