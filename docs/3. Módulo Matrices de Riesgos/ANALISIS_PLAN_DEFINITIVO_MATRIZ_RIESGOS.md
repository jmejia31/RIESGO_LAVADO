# Análisis y Plan Definitivo de Implementación
## Módulo Matrices de Riesgos (SGRLA - IHSS)

Este documento consolidado representa el análisis maestro, las decisiones de arquitectura definitiva y el plan de implementación para el módulo **Matrices de Riesgos** del Sistema de Gestión de Riesgo de Lavado de Activos y Financiamiento del Terrorismo (SGRLA) del Instituto Hondureño de Seguridad Social (IHSS).

Consolida en una única fuente de verdad la validación técnica reproducible que cierra la **Fase 12** y el diseño estratégico de evolución dinámica del módulo de **0 a 100%**.

---

## 1. Alineación de Objetivos

Existe una alíneación total y complementaria entre los dos componentes clave de esta intervención:

1. **La Validación Técnica Reproducible (Fase 12):** Asegura la calidad actual de la plataforma (limpieza de Git, compilación correcta, 96 pruebas Backend, 165 pruebas Frontend, 7 E2E y paso de Quality Gates sin degradaciones). Proporciona la base sólida de no-regresión necesaria antes de iniciar cambios estructurales.
2. **El Plan Definitivo de Implementación (Arquitectura Dinámica):** Define la visión a largo plazo para transformar el módulo en una solución paramétrica y dinámica basada en esquemas JSON. Esta arquitectura se apoya directamente en las políticas de calidad validadas en la Fase 12 para su despliegue seguro en Oracle (`MR_`) y Angular 22.

---

## 2. Validación Técnica de la Fase 12

La validación técnica se ha completado en el entorno local con un éxito del 100%. Los resultados reales registrados en esta sesión de trabajo son:

| Validación | Comando de Ejecución | Resultado Local Real |
|---|---|---|
| **Estructura** | `powershell -NoProfile -ExecutionPolicy Bypass -Command "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; & './tools/validate_repository_structure.ps1'"` | **Correcto**. 119 rutas obligatorias y 441 archivos validados. |
| **Base de Datos** | `powershell -NoProfile -ExecutionPolicy Bypass -Command "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; & './tools/validate_database_scripts.ps1'"` | **Correcto**. 19 scripts activos raíz, 1 paquete modular, 22 alcanzables. |
| **Documentación** | `powershell -NoProfile -ExecutionPolicy Bypass -Command "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; & './tools/validate_documentation_links.ps1'"` | **Correcto**. 34 Markdown y 41 enlaces validados (sin roturas). |
| **Quality Gates** | `powershell -NoProfile -ExecutionPolicy Bypass -Command "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; & './tools/run_quality_gates.ps1'"` | **Correcto**. Puertas de calidad aprobadas. |
| **Backend Test** | `dotnet test RIESGO_LAVADO.sln --configuration Release --no-restore` | **96 pruebas aprobadas**, 0 fallidas, 0 omitidas. |
| **Frontend Test** | `npm test -- --watch=false` | **165 pruebas aprobadas** (18 archivos de prueba). |
| **E2E Playwright** | `npm run e2e` | **7 pruebas aprobadas** en Playwright Chromium. |

### Métricas de Cobertura Reportadas por Quality Gates:
* **Backend:** Líneas: `22.15%` | Ramas: `21.21%`
* **Frontend:** Sentencias: `38.99%` | Ramas: `33.51%` | Funciones: `36.00%` | Líneas: `39.20%`

---

## 3. Decisiones y Diseño de la Arquitectura Definitiva

### 3.1 Módulo y Familia de Formularios
Se construirá un único módulo institucional denominado **Matrices de Riesgos**. Dentro de él se administrará inicialmente una familia denominada **Matriz de Riesgos**. 

La solución permitirá versionar de forma inmutable la estructura a través de un esquema JSON:
```text
Matriz de Riesgos
├── Versión 1 (Borrador -> Publicada -> Vigente)
├── Versión 2 (Clonada desde Versión 1)
└── Versiones futuras
```
El resultado consolidado principal será la **Matriz Consolidada de Riesgos**, la cual mostrará todas las evaluaciones realizadas incluso si pertenecen a versiones estructurales distintas, utilizando un mapeo por **claves canónicas**.

### 3.2 Fuente Funcional de Origen
El libro consolidado de origen (archivo Excel de Matrices de Riesgos institucional) se congela como evidencia histórica de origen y cálculo de huella digital, y cuenta con:
* 59 riesgos.
* 82 campos totales.
* 1,742 fórmulas de cálculo.
* 48 campos de captura o metadatos.
* 34 campos calculados.
* ~20 campos auxiliares internos.
* ~62 campos funcionales visibles.

### 3.3 Principios de la Solución
* **Configuración Dinámica:** Los formularios web en Angular 22 se renderizarán dinámicamente basados en el JSON del formulario, eliminando pantallas rígidas.
* **Versionamiento Inmutable:** Una versión en estado `PUBLICADA` no puede modificarse. Los cambios requerirán clonar la estructura y crear una nueva versión.
* **Cálculos Protegidos:** El servidor (ASP.NET Core 10) es la única autoridad de cálculo. El frontend puede previsualizar, pero los valores persistidos se calculan en backend.
* **Persistencia Híbrida:** Almacenamiento JSON para respuestas dinámicas, tablas relacionales estructurales, tablas específicas para controles, planes de mitigación, y una tabla de proyecciones optimizada para búsquedas y consolidación.
* **Prefijo Oracle Dedicado:** Todas las tablas, secuencias y objetos de este módulo utilizarán el prefijo `MR_`.

---

## 4. Estructura de Datos en Oracle (Prefijo `MR_`)

### 4.1 Tablas de Formularios y Configuración
* `MR_FAMILIA_FORMULARIO`: Identidad de la matriz configurable (código, nombre, estado).
* `MR_VERSION_FORMULARIO`: Estructura concreta versionada (versión, JSON de campos, estado, vigencias, huella digital).
* `MR_REGISTRO_CAMPO`: Registro semántico global de los campos e identificadores.
* `MR_DIFERENCIA_VERSION`: Detalle de diferencias entre versiones (campos agregados, modificados, retirados).
* `MR_APROBACION_FORMULARIO`: Log de aprobaciones, revisiones y rechazos de la configuración.
* `MR_PERMISO_FORMULARIO`: Definición de permisos a nivel de campo y rol.

### 4.2 Tablas de Operación y Transacciones
* `MR_RIESGO`: Entidad del riesgo con su código único e identidad permanente.
* `MR_RELACION_RIESGO`: Relación muchos a muchos entre riesgos.
* `MR_EVALUACION_RIESGO`: Evaluación transaccional (datos en JSON, resultados calculados en JSON, auditoría).
* `MR_PROYECCION_EVALUACION`: Tabla aplanada de columnas principales (valores e impacto inherente/residual, área, estado, etc.) optimizada para filtros y búsquedas veloces.
* `MR_FLUJO_EVALUACION`: Log de estados del ciclo de vida de la evaluación (Elaborado, Revisado, Aprobado).

### 4.3 Controles, Planes de Mitigación y Seguimiento
* `MR_CONTROL_RIESGO`: Catálogo de controles vinculados a una evaluación de riesgo.
* `MR_EVALUACION_CONTROL`: Evaluación individual de controles (diseño, ejecución, efectividad).
* `MR_PLAN_MITIGACION` y `MR_ACTIVIDAD_PLAN`: Planes de acción y actividades con responsables, fechas y porcentaje de avance.
* `MR_SENAL_ALERTA` y `MR_AUTOMONITOREO`: Indicadores de umbrales y revisiones de automonitoreo de áreas.
* `MR_ARCHIVO_ADJUNTO`: Metadatos de evidencias físicas. El archivo físico se almacena en el filesystem seguro del servidor; nunca en base64 en BD.
* `MR_EVENTO_AUDITORIA`: Trazabilidad completa (acción, valor anterior, valor posterior, motivo, usuario, IP, correlación).

---

## 5. Diseño de Servicios y Contratos REST

Se estructurará modularmente en el Backend en `Features/MatricesRiesgos`:

### Endpoints Principales:
* `GET/POST/PUT /api/matrices-riesgos/formularios`: Administración de familias y esquemas.
* `POST /api/matrices-riesgos/formularios/{id}/versiones/{v}/publicar`: Transición y vigencia de versiones.
* `GET/POST /api/matrices-riesgos/riesgos`: Gestión de la cabecera e identificación de riesgos.
* `GET/POST /api/matrices-riesgos/evaluaciones`: Creación de evaluaciones y almacenamiento de datos dinámicos.
* `GET/POST /api/matrices-riesgos/consolidado`: Matriz Consolidada de Riesgos e indicadores.
* `POST /api/matrices-riesgos/migraciones/libro-origen`: Procesamiento y conciliación de los 59 riesgos iniciales.

---

## 6. Plan Definitivo de Implementación de 0 a 100%

El desarrollo se ejecutará bajo el siguiente esquema de avance y entregables verificables:

| Avance | Fase | Entregable Verificable |
|---:|---|---|
| **0–5%** | Análisis funcional | Diccionario de 82 campos, mapeo de fórmulas y catálogos institucionales. |
| **5–10%** | Diseño técnico | Diagrama de base de datos final, esquema JSON y contratos API. |
| **10–25%** | Base de datos | Creación en Oracle de tablas `MR_`, llaves, secuencias e índices. |
| **25–45%** | Servicios estructurales | Implementación de APIs de configuración, familias, versiones y riesgos. |
| **45–55%** | Servicios funcionales | Motor de reglas, cálculos inherente/residual, flujos y auditoría en backend. |
| **55–72%** | Interfaz web | Renderizador dinámico de formularios, edición de planes y controles en Angular. |
| **72–80%** | Matriz Consolidada | Pantalla de proyecciones, filtros, exportación a Excel e indicadores visuales. |
| **80–88%** | Migración | Script y proceso de carga de los 59 riesgos origen con reporte de conciliación. |
| **88–95%** | Pruebas integrales | Pruebas unitarias de motor de cálculo, de versionamiento y pruebas E2E. |
| **95–98%** | Despliegue y capacitación | Preparación de entornos (Pruebas, UAT, Prod) y capacitación a analistas. |
| **98–100%** | Aceptación y cierre | Cierre de UAT con el usuario funcional, documentación final y firma de Javier Mejía. |

---

## 7. Control de Riesgos del Proyecto

* **Cálculos en Cliente vs. Servidor:** Para mitigar la manipulación, el backend siempre recalculará los valores oficiales.
* **Divergencias en Consolidación:** Las claves canónicas resolverán la visualización homogénea de versiones heterogéneas. Si un campo no existe en una versión previa, se presentará vacío, sin inventar datos.
* **Cuadre de Carga de Origen:** La migración exige un reporte automatizado que compare los cálculos del sistema contra las 1,742 fórmulas del libro origen para los 59 riesgos.

---

## 8. Definición de Terminado (DoD)

El módulo se declarará al 100% únicamente si:
1. La versión 1 está publicada y vigente.
2. Los 59 riesgos origen están completamente migrados y conciliados.
3. Se puede crear una Versión 2 sin alterar los registros e históricos de la Versión 1.
4. Las pruebas de Quality Gates (cobertura mínima, tests unitarios, tests E2E) pasan correctamente.
5. El propietario del producto (Javier Mejía) otorga la aceptación formal por escrito.