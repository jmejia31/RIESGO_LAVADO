# ANÁLISIS MAESTRO Y PLAN DEFINITIVO DE IMPLEMENTACIÓN
## MÓDULO MATRICES DE RIESGOS (SGRLA - IHSS)
### Arquitectura dirigida por metadatos, formularios dinámicos JSON, persistencia híbrida y versionamiento histórico

> [!IMPORTANT]
> **Estado documental:** este archivo se conserva como antecedente consolidado de las Intervenciones #8 y #9. La línea base funcional y técnica final vigente es [`Analisis_Definitivo_Modulo_Matrices_de_Riesgos.docx`](Analisis_Definitivo_Modulo_Matrices_de_Riesgos.docx), versión 1.1. El archivo Word nativo separa el riesgo de sus evaluaciones, amplía el modelo relacional, distingue las pruebas históricas de Fase 12 de las pruebas del nuevo desarrollo y diferencia las fórmulas verificadas en Excel de su aprobación funcional para producción.

---

## Control del Documento

| Elemento | Definición |
|---|---|
| **Nombre Oficial** | Análisis Maestro y Plan Definitivo de Implementación del Módulo Matrices de Riesgos |
| **Versión** | 1.0 Final Consolidada |
| **Fecha** | 30 de julio de 2026 |
| **Estado** | Antecedente consolidado; sustituido como línea base por la versión 1.1 en formato `.docx` nativo. |
| **Documento vigente** | [`Analisis_Definitivo_Modulo_Matrices_de_Riesgos.docx`](Analisis_Definitivo_Modulo_Matrices_de_Riesgos.docx) |

### Historial de Versiones del Documento
* **v1.0 (30/07/2026):** Versión final consolidada y unificada de los requerimientos analizados por los agentes y el propietario de requerimientos (Javier Mejía). Combina la validación de calidad de la Fase 12 y el diseño detallado del módulo dinámico de Matrices de Riesgos.

---

## 1. Introducción y Alineación de Objetivos

Este documento representa la síntesis definitiva para la construcción del módulo **Matrices de Riesgos** del Sistema de Gestión de Riesgo de Lavado de Activos y Financiamiento del Terrorismo (SGRLA) del Instituto Hondureño de Seguridad Social (IHSS).

Existe una relación complementaria entre los dos componentes clave de este análisis:
1. **La Validación Técnica Reproducible (Fase 12):** Establece una línea base histórica de calidad. Las 96 pruebas de servicios, 165 pruebas de interfaz web, 7 pruebas de extremo a extremo y los validadores ejecutados corresponden a la plataforma existente en esa intervención; no prueban que el módulo dinámico futuro ya esté implementado.
2. **El Plan Definitivo de Implementación (0 a 100%):** Define la visión a largo plazo para construir un módulo paramétrico dirigido por metadatos (JSON) en vez de pantallas estáticas, estructurando la persistencia en base de datos Oracle con prefijo modular y protegiendo el motor de reglas en el servidor.

---

## 2. Reporte Oficial de la Validación Técnica Realizada

La validación técnica del entorno local se ha ejecutado con éxito del 100%, arrojando los siguientes resultados reales:

| Validación | Comando de Ejecución | Resultado Local Real |
|---|---|---|
| **Estructura** | `validate_repository_structure.ps1` | **Correcto**. 119 rutas obligatorias y 442 archivos validados. |
| **Base de Datos** | `validate_database_scripts.ps1` | **Correcto**. 19 scripts activos raíz, 1 paquete modular, 22 alcanzables. |
| **Documentación** | `validate_documentation_links.ps1` | **Correcto**. 35 Markdown y 48 enlaces validados (sin roturas). |
| **Quality Gates** | `run_quality_gates.ps1` | **Correcto**. Puertas de calidad aprobadas. |
| **Backend Test** | `dotnet test RIESGO_LAVADO.sln --configuration Release` | **96 pruebas aprobadas**, 0 fallidas, 0 omitidas. |
| **Frontend Test** | `npm test -- --watch=false` | **165 pruebas aprobadas** (18 archivos de prueba). |
| **E2E Playwright** | `npm run e2e` | **7 pruebas aprobadas** en Playwright Chromium. |

### Cobertura de Código:
* **Backend:** Líneas: `22.15%` | Ramas: `21.21%`
* **Frontend:** Sentencias: `38.99%` | Ramas: `33.51%` | Funciones: `36.00%` | Líneas: `39.20%`

---

## 3. Identificación Oficial de la Solución y Nomenclatura

Para evitar confusiones en los contratos de datos, pantallas y base de datos, se adopta la siguiente nomenclatura oficial inmutable:
* **Nombre del Módulo:** Matrices de Riesgos.
* **Nombre del Instrumento Administrado:** Matriz de Riesgos.
* **Nombre de la Consulta Transversal:** Matriz Consolidada de Riesgos.
* **Primera Configuración:** Matriz de Riesgos - Formulario A - Versi&oacute;n 1.
* **Versiones Posteriores:** Formulario B, Formulario C y siguientes.
* **Regla de Oro:** Queda prohibido utilizar la denominación "Matriz Maestra de Riesgos" en el ámbito funcional del proyecto.

---

## 4. Objetivo General del Módulo

El objetivo es transformar la Matriz de Riesgos administrada mediante el libro Excel consolidado de origen en un módulo institucional dinámico, versionado, auditable, mantenible, escalable, seguro y configurable.

La solución deberá permitir:
* Registrar riesgos con código único e identidad permanente.
* Crear y administrar formularios dinámicos a través de esquemas JSON.
* Versionar formularios de forma inmutable (clonar versiones vigentes, editar borradores y publicar de forma transaccional).
* Calcular el riesgo inherente, la efectividad individual/ponderada de controles y el riesgo residual de forma protegida en el servidor.
* Administrar controles, planes de mitigación, actividades, evidencias físicas y automonitoreo periódico.
* Generar la Matriz Consolidada de Riesgos que reúna evaluaciones de diferentes versiones.
* Asegurar que la adición de nuevos campos de tipos soportados, cambios de orden o etiquetas no requieran modificaciones en código C# o TS.

---

## 5. Principio Arquitectónico Definitivo

La solución adoptará una arquitectura híbrida y dirigida por metadatos (metadata-driven):

```text
Configuración JSON Versionada
            ↓
Motor Dinámico Angular (Frontend)
            ↓
API y Servicios ASP.NET Core (Backend)
            ↓
Persistencia Híbrida Oracle (MR_)
```

### 5.1 ¿Por qué evitar una solución completamente rígida?
No se crearán tablas físicas en Oracle con 82 columnas rígidas e inalterables vinculadas a controles fijos en el frontend. Hacerlo provocaría que cualquier cambio de requerimientos (como añadir un campo de texto o cambiar una etiqueta) obligara a modificar la base de datos, los modelos del backend, los servicios, los componentes del frontend, los reportes, las exportaciones y la suite de pruebas.

### 5.2 ¿Por qué evitar una solución puramente JSON (NoSQL)?
No se almacenará toda la información en un único campo CLOB de tipo JSON. La integridad transaccional, seguridad, auditoría, concurrencia, relaciones, búsquedas, indicadores de rendimiento y reportes cruzados requieren una estructura relacional limpia. Por tanto, se persistirán los datos operacionales de búsqueda (como áreas, códigos, puntajes inherentes/residuales y estados) en columnas de base de datos dedicadas, mientras que el detalle dinámico del formulario se almacenará en campos JSON.

---

## 6. Responsabilidad de Componentes y Reglas de Seguridad

### 6.1 Responsabilidad de la Configuración JSON
Cada versión de la Matriz de Riesgos tendrá una configuración JSON completa que controlará:
* Secciones y subsecciones del formulario.
* Campos, etiquetas, descripciones y ayudas.
* Orden de visualización y tipo de componente (texto, número, catálogo, fecha, etc.).
* Obligatoriedad, longitudes, rangos y expresiones regulares de validación.
* Catálogos dinámicos vinculados.
* Reglas de visibilidad condicional y dependencias.
* Permisos de edición y lectura por rol.
* Columnas incluidas en la Matriz Consolidada y exportaciones.

### 6.2 Responsabilidad del Backend (.NET 10)
El backend es la única autoridad de seguridad y cálculo. Protegerá la base de datos y la consistencia del sistema:
* Validación estricta de datos contra el esquema JSON de la versión exacta al momento de guardar.
* Generación del código del riesgo secuencial y único.
* Cálculo del riesgo inherente, residual, progreso de planes y efectividad de controles.
* Control transaccional de estados de versión, asegurando que solo exista una versión vigente (`PUBLISHED_ACTIVE`).
* Auditoría obligatoria de cambios y accesos.

### 6.3 Elementos Prohibidos en el JSON
Para evitar riesgos de seguridad (como inyección de código y ejecución arbitraria), queda estrictamente prohibido incluir en el JSON:
* Código JavaScript arbitrario.
* Consultas SQL o comandos DDL/DML.
* Expresiones de C# dinámico o invocación de métodos reflexivos.
* Comandos del sistema operativo.
* Uso de funciones tipo `eval()` o manipulación directa del DOM del navegador.
El JSON puede referenciar una regla de cálculo por su identificador registrado (ej. `REG_CALCULO_INHERENTE_V1`), pero nunca contendrá su código fuente ejecutable.

---

## 7. Estructura Funcional de la Matriz de Riesgos

La Matriz de Riesgos se organizará en siete secciones funcionales visibles:

### 7.1 Identificación y Contexto Organizacional
* **Campos:** Número (secuencial), Código del riesgo (único y permanente), Área principal (catálogo), Áreas relacionadas (selección múltiple), Área consolidada (derivada de la jerarquía), Tipo de riesgo, Procedimiento y Objetivos estratégicos (catálogo).
* **Reglas:** El número y el código son generados por el sistema y no son editables.

### 7.2 Riesgo Inherente
* **Campos:** Descripción del riesgo, Causas, Efectos, Frecuencia (escala 1 a 5), Impacto (escala 1 a 5), Valor del riesgo inherente (VRI) y Nivel del riesgo inherente.
* **Fórmula metodológica verificada en el libro de origen:**
  $$\text{VRI} = \text{Frecuencia} + \text{Impacto} - 1$$
  *La expresión fue comprobada estructuralmente en el libro. Su escala, rangos e implementación en el servidor deberán recibir aprobación funcional antes de declararse regla institucional definitiva.*

### 7.3 Responsabilidad y Relaciones
* **Campos:** Dueño del riesgo (usuario/área), Responsables adicionales, Regímenes afectados, Riesgos relacionados (transversalidad), Amenazas, Vulnerabilidades y Activos de información (estos tres últimos obligatorios y visibles únicamente para riesgos de tecnología).

### 7.4 Controles Existentes
* **Campos:** Colección repetible de controles. Cada control tendrá: Identificador, Tipo (Preventivo, Detectivo, Correctivo), Descripción, Escala de efectividad, Nivel y porcentaje calculado, Nivel de automatización (Manual, Semiautomático, Automático), Estado, Comentarios y Evidencias físicas obligatorias.

### 7.5 Valoración Residual y Respuesta al Riesgo
* **Fórmula de Efectividad Total Ponderada (ETP) verificada en el libro:**
  $$\text{ETP} = 0.70 \times \text{Efectividad Preventiva} + 0.15 \times \text{Efectividad Detectiva} + 0.15 \times \text{Efectividad Correctiva}$$
* **Fórmula de Riesgo Residual (VRR) verificada en el libro:**
  $$\text{VRR} = \text{REDONDEAR}(\text{MÁXIMO}(1, \text{VRI} \times (1 - \text{ETP})), 0)$$
  *Las ponderaciones, la distribución entre Frecuencia Residual e Impacto Residual y los semáforos deberán quedar respaldados por conciliación, casos de prueba y aprobación funcional.*
* **Tratamiento:** Respuesta al riesgo (Aceptar, Evitar, Transferir, Mitigar) y justificación.

### 7.6 Planes de Mitigación y Actividades
* Colección repetible de planes vinculados. Cada plan incluye: Descripción, Responsable, Fechas (inicial y final), Presupuesto (no negativo), Estado y Avance. Cada actividad del plan detallará su avance de 0 a 100%, evidencias físicas y comentarios.

### 7.7 Automonitoreo y Alertas
* Evaluaciones periódicas del estado del riesgo y controles, comentarios de la Unidad de Gestión de Riesgos, semáforos de alertas por vencimiento de planes e historial de automonitoreo.

---

## 8. Versionamiento y Ciclo de Vida de Formularios

### 8.1 Estados Oficiales de Versión
* `DRAFT`: Versión editable en fase de diseño.
* `IN_REVIEW`: Versión enviada a revisión y validación técnica.
* `APPROVED`: Aprobada para su puesta en marcha.
* `PUBLISHED_ACTIVE`: Versión vigente para capturar nuevas evaluaciones de riesgos.
* `RETIRED`: Histórica, no permite nuevas capturas pero mantiene todas las consultas e históricos.
* `ARCHIVED`: Conservada fuera de la operación habitual del sistema.

### 8.2 Reglas de Transición y Concurrencia
* Solo puede existir una versión `PUBLISHED_ACTIVE` a la vez.
* La publicación de una nueva versión (ej. Formulario B) retirará de forma automática y transaccional a la anterior (Formulario A).
* Si la activación del Formulario B falla en la base de datos, el Formulario A continuará activo.
* Los registros históricos creados con la versión A conservarán su asociación inmutable a esa versión.

---

## 9. Modelo de Datos Oracle (Prefijo `MR_`)

Para integrarse de forma limpia con el monolito institucional y respetar el estándar de nombres del IHSS, se definen los siguientes objetos de base de datos con el prefijo `MR_` (equivalentes a la nomenclatura canónica):

| Tabla Oficial Oracle | Equivalente Canónico | Responsabilidad |
|---|---|---|
| `MR_FAMILIA_FORMULARIO` | `RISK_FORM_FAMILY` | Cabecera y código de la familia de matrices (ej. Matriz de Riesgos). |
| `MR_VERSION_FORMULARIO` | `RISK_FORM_VERSION` | Almacena el JSON de la versión, su hash, estado, vigencia y aprobadores. |
| `MR_REGISTRO_CAMPO` | `RISK_FORM_FIELD_REGISTRY` | Inventario semántico de campos para mantener su clave canónica. |
| `MR_RIESGO` | `RISK_RECORD` | Cabecera del riesgo (código único, área propietaria, fecha de creación). |
| `MR_EVALUACION_RIESGO` | `RISK_RECORD_EVALUATION` | Transacción de la evaluación (datos JSON, cálculos oficiales JSON, versión). |
| `MR_PROYECCION_EVALUACION` | `RISK_RECORD_PROJECTION` | Tabla plana con las columnas principales para filtros rápidos y reportes. |
| `MR_RELACION_RIESGO` | `RISK_RECORD_RELATION` | Relación N:N de riesgos transversales. |
| `MR_CONTROL_RIESGO` | `RISK_RECORD_CONTROL` | Detalle estructurado de los controles por evaluación. |
| `MR_PLAN_MITIGACION` | `RISK_RECORD_PLAN` | Planes de acción asociados a la evaluación del riesgo. |
| `MR_ACTIVIDAD_PLAN` | `RISK_RECORD_ACTIVITY` | Actividades por plan con responsables, fechas y porcentaje de avance. |
| `MR_ARCHIVO_ADJUNTO` | `RISK_ATTACHMENT` | Metadatos de evidencias (nombre, extensión, hash, ID en almacenamiento). |
| `MR_EVENTO_AUDITORIA` | `RISK_AUDIT_EVENT` | Historial de auditoría completo (valores anteriores, nuevos, IP y motivo). |

---

## 10. Proceso de Carga y Migración de los 59 Riesgos Origen

La migración de los 59 riesgos del libro de Excel consolidado se realizará bajo estrictos controles de conciliación:
1. **Congelación del Libro:** Se calcula el hash SHA-256 del archivo Excel consolidado origen como huella digital inmutable.
2. **Normalización:** Las descripciones compuestas del Excel se mapearán a los campos independientes de causas, efectos y descripción general. Las áreas y responsables se transformarán a sus equivalentes de los catálogos del sistema.
3. **Cálculo de Paridad:** Se procesará cada fila en el backend del sistema ejecutando las fórmulas oficiales. El sistema contrastará el resultado calculado contra las 1,742 fórmulas del libro Excel origen.
4. **Reporte de Conciliación:** Se generará un informe automatizado donde:
   $$\text{59 Riesgos Excel} = \text{Riesgos Migrados Aprobados} + \text{Riesgos Rechazados Documentados}$$
   *No se permitirá que ningún riesgo sea omitido silenciosamente. Cualquier diferencia en los decimales o en el nivel de riesgo inherente/residual debido a la metodología del Excel vs. el sistema deberá quedar plenamente justificada e informada al usuario funcional.*

---

## 11. Plan de Ejecución de 0 a 100%

| Avance | Etapa | Entregable Verificable |
|---:|---|---|
| **0–5%** | Confirmación funcional | Inventario de 82 campos, reglas de cálculo aprobadas y firma de alcance. |
| **5–10%** | Diseño técnico | Documento de arquitectura, JSON Schema y contratos REST. |
| **10–25%** | Base de datos Oracle | Scripts DDL ejecutados (tablas `MR_`, índices, secuencias y auditoría). |
| **25–45%** | Backend base | APIs operativas para formularios, versiones, catálogos y persistencia. |
| **45–55%** | Reglas y cálculos | Motor de cálculo implementado y pruebas unitarias de paridad aprobadas. |
| **55–72%** | Frontend dinámico | Renderizador Angular de secciones y campos, pantallas de captura y controles. |
| **72–80%** | Matriz Consolidada | Tabla de proyecciones, filtros cruzados, mapa de calor y exportación Excel. |
| **80–88%** | Migración | Proceso de importación de los 59 riesgos del Excel con reporte de cuadre. |
| **88–94%** | Pruebas integrales | Pruebas de regresión, seguridad, concurrencia y pruebas E2E. |
| **94–97%** | Documentación | Manuales técnicos, manuales funcionales y guías de administración. |
| **97–100%** | Despliegue y UAT | Instalación en ambientes del IHSS, validación de DBA y aceptación formal. |

---

## 12. Definición de Terminado (Definition of Done - DoD)

El módulo se considerará finalizado al 100% únicamente si cumple con las siguientes condiciones:
* Los 82 campos del libro consolidado están completamente catalogados y mapeados.
* La Versión 1 (Formulario A) de la Matriz de Riesgos está publicada y activa en el sistema.
* Los 59 riesgos del libro Excel de origen están completamente importados, recalculados y conciliados en la base de datos Oracle.
* El backend recalcula y valida de forma protegida el riesgo inherente y residual.
* El frontend dibuja correctamente los formularios y condicionales a partir de la configuración JSON.
* Las exportaciones a Excel y PDF cuadran exactamente con las consultas en pantalla.
* Se ha demostrado que se puede clonar y publicar una versión B sin alterar los históricos de la versión A.
* Todas las pruebas de Quality Gates pasan con éxito y no existen defectos de severidad alta o crítica.
* Se cuenta con la aceptación formal por escrito del propietario del producto (Javier Mejía).
