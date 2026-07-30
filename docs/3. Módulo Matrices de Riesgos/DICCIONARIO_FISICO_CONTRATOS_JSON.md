# Diccionario Físico y Especificación de Contratos JSON
## Módulo: Matrices de Riesgos (SGRLA - IHSS)
### Versión: 1.0
### Estado: Preparado para revisión y aprobación

Este documento detalla la especificación formal del modelo físico de base de datos de 28 tablas relacionales bajo el prefijo **`RL_MR_*`**, la definición del contrato JSON propietario del IHSS y los DTOs de acoplamiento del backend para el nuevo módulo modular de Matrices de Riesgos.

---

## 1. Contrato JSON Propietario de Metadatos de Formulario (Formulario A - Versión 1)

El formulario dinámico se parametriza mediante un esquema JSON almacenado en la tabla `RL_MR_VERSIONES_FORMULARIO`. La estructura controla de forma directa el renderizado de la interfaz en Angular y la validación en C#.

### 1.1 Estructura Completa del Objeto JSON (Contrato Raíz)
```json
{
  "codigoFormulario": "MATRIZ_RIESGOS_LAFT",
  "nombreFormulario": "Matriz de Riesgos LA/FT - Formulario A",
  "version": 1,
  "secciones": [
    {
      "identificador": "identificacion_contexto",
      "titulo": "1. Identificación y Contexto",
      "orden": 1,
      "campos": [
        {
          "identificador": "area_principal_id",
          "claveCanonica": "areaPrincipalId",
          "etiqueta": "Área Responsable",
          "tipo": "selector-catalogo",
          "codigoCatalogo": "CAT_AREAS",
          "orden": 1,
          "obligatorio": true,
          "soloLectura": false,
          "ayuda": "Seleccione la división u oficina responsable del riesgo"
        },
        {
          "identificador": "tipo_riesgo",
          "claveCanonica": "tipoRiesgo",
          "etiqueta": "Tipo de Riesgo",
          "tipo": "texto",
          "orden": 2,
          "obligatorio": true,
          "longitudMaxima": 150
        },
        {
          "identificador": "procedimiento_vinculado",
          "claveCanonica": "procedimientoVinculado",
          "etiqueta": "Procedimiento Vinculado",
          "tipo": "texto",
          "orden": 3,
          "obligatorio": false,
          "longitudMaxima": 250
        }
      ]
    },
    {
      "identificador": "riesgo_inherente",
      "titulo": "2. Evaluación del Riesgo Inherente",
      "orden": 2,
      "campos": [
        {
          "identificador": "descripcion_riesgo",
          "claveCanonica": "descripcionRiesgo",
          "etiqueta": "Descripción del Evento",
          "tipo": "texto-largo",
          "orden": 1,
          "obligatorio": true,
          "longitudMaxima": 1000
        },
        {
          "identificador": "frecuencia_inherente_id",
          "claveCanonica": "frecuenciaInherenteId",
          "etiqueta": "Frecuencia",
          "tipo": "selector-catalogo",
          "codigoCatalogo": "CAT_FRECUENCIA",
          "orden": 2,
          "obligatorio": true
        },
        {
          "identificador": "impacto_inherente_id",
          "claveCanonica": "impactoInherenteId",
          "etiqueta": "Impacto",
          "tipo": "selector-catalogo",
          "codigoCatalogo": "CAT_IMPACTO",
          "orden": 3,
          "obligatorio": true
        }
      ]
    }
  ]
}
```

---

## 2. Diccionario de Datos Físico Definitivo (28 Tablas)

### 2.1 Tabla: `RL_MR_FAMILIAS_FORMULARIO`
Define las agrupaciones de matrices configurables.
* `FAM_ID` (NUMBER(15), Not Null): Clave primaria generada por `SEQ_RL_MR_FAMILIAS`.
* `FAM_CODIGO` (VARCHAR2(50), Not Null): Código semántico único (UQ).
* `FAM_NOMBRE` (VARCHAR2(150), Not Null): Nombre visible del grupo.
* `FAM_DESCRIPCION` (VARCHAR2(500), Null): Nota descriptiva.
* `FAM_ACTIVO` (NUMBER(1), Not Null, Default 1): Borrado lógico (0=Inactivo, 1=Activo).
* `FAM_FECHA_CREACION` (DATE, Not Null, Default SYSDATE).

### 2.2 Tabla: `RL_MR_VERSIONES_FORMULARIO`
Esquemas JSON de formularios versionados.
* `VER_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_VERSIONES`.
* `VER_FAMILIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_FAMILIAS_FORMULARIO`.
* `VER_CODIGO` (VARCHAR2(30), Not Null): Identificador visual (ej. `FORM_A`).
* `VER_VERSION` (NUMBER(5), Not Null): Número de versión secuencial.
* `VER_JSON` (CLOB, Not Null): Estructura de campos (CHECK IS JSON).
* `VER_HASH` (VARCHAR2(64), Not Null): Hash SHA-256 inmutable de la configuración.
* `VER_ESTADO` (VARCHAR2(20), Not Null, Default 'DRAFT'): Estados: `DRAFT`, `IN_REVIEW`, `APPROVED`, `PUBLISHED`, `RETIRED`, `ARCHIVED`.
* `VER_VIGENTE` (NUMBER(1), Not Null, Default 0): Vigencia exclusiva por familia (CHECK IN (0,1)).
* `VER_FECHA_INICIO` (DATE, Null).
* `VER_FECHA_FIN` (DATE, Null).
* `VER_USR_CREACION` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.

### 2.3 Tabla: `RL_MR_CAMPOS_FORMULARIO`
Catálogo de campos canónicos unificados.
* `CAM_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_CAMPOS`.
* `CAM_CLAVE_CANONICA` (VARCHAR2(100), Not Null): Código canónico funcional único (ej. `areaPrincipalId`).
* `CAM_NOMBRE_MOSTRAR` (VARCHAR2(150), Not Null): Texto visible por defecto.
* `CAM_TIPO_DATO` (VARCHAR2(50), Not Null): ej. `NUMBER`, `VARCHAR2`.

### 2.4 Tabla: `RL_MR_APROBACIONES_FORMULARIO`
Bitácora de transiciones de aprobación de plantillas.
* `APR_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_APROBACIONES`.
* `APR_VERSION_ID` (NUMBER(15), Not Null): FK a `RL_MR_VERSIONES_FORMULARIO`.
* `APR_ESTADO_NVO` (VARCHAR2(20), Not Null): Estado al que transiciona.
* `APR_COMENTARIO` (VARCHAR2(1000), Null): Observaciones.
* `APR_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
* `APR_FECHA` (DATE, Default SYSDATE).

### 2.5 Tabla: `RL_MR_PERMISOS_FORMULARIO`
Matriz de seguridad a nivel de sección/campo por Rol.
* `PER_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_PERMISOS`.
* `PER_VERSION_ID` (NUMBER(15), Not Null): FK a `RL_MR_VERSIONES_FORMULARIO`.
* `PER_ROL_ID` (NUMBER(3), Not Null): Identificador de rol institucional.
* `PER_SECCION_ID` (VARCHAR2(100), Not Null): Clave de la sección/campo.
* `PER_PERMISO` (VARCHAR2(20), Not Null): CHECK IN ('LECTURA', 'EDICION', 'OCULTO').

### 2.6 Tabla: `RL_MR_RIESGOS`
Identidad inmutable y permanente del riesgo.
* `RIE_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_RIESGOS`.
* `RIE_CODIGO` (VARCHAR2(30), Not Null): Código incremental autogenerado (ej. `RIE-0001`).
* `RIE_FECHA_CREACION` (DATE, Default SYSDATE).
* `RIE_ACTIVO` (NUMBER(1), Default 1): Borrado lógico.

### 2.7 Tabla: `RL_MR_RELACIONES_RIESGO`
Relaciones de transversalidad entre riesgos padres e hijos.
* `REL_RIE_PADRE_ID` (NUMBER(15), Not Null): FK a `RL_MR_RIESGOS`.
* `REL_RIE_HIJO_ID` (NUMBER(15), Not Null): FK a `RL_MR_RIESGOS`.
* Clave Primaria Compuesta (`REL_RIE_PADRE_ID`, `REL_RIE_HIJO_ID`).

### 2.8 Tabla: `RL_MR_EVALUACIONES_RIESGO`
Respuestas e instancias de evaluación del riesgo.
* `EVA_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_EVALUACIONES`.
* `EVA_RIESGO_ID` (NUMBER(15), Not Null): FK a `RL_MR_RIESGOS`.
* `EVA_VERSION_ID` (NUMBER(15), Not Null): FK a `RL_MR_VERSIONES_FORMULARIO`.
* `EVA_DATA_JSON` (CLOB, Not Null): Respuestas dinámicas (CHECK IS JSON).
* `EVA_DATA_CALC_JSON` (CLOB, Not Null): Resultados del cómputo oficial (CHECK IS JSON).
* `EVA_FECHA_REGISTRO` (DATE, Default SYSDATE).
* `EVA_USR_REGISTRO` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
* `EVA_VERSION_ROW` (NUMBER(10), Default 1): Versión incremental de fila para concurrencia optimista.
* `EVA_ACTIVO` (NUMBER(1), Default 1): Borrado lógico.

### 2.9 Tabla: `RL_MR_REVISIONES_EVALUACION`
Instantáneas históricas de auditoría transaccional.
* `REV_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_REVISIONES`.
* `REV_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
* `REV_DATOS_JSON` (CLOB, Not Null): Snapshot completo (CHECK IS JSON).
* `REV_FECHA` (DATE, Default SYSDATE).
* `REV_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.

### 2.10 Tabla: `RL_MR_PROYECCIONES_EVALUACION`
Estructura relacional plana optimizada para vistas rápidas de mapa de calor.
* `PROY_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_PROYECCIONES`.
* `PROY_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
* `PROY_CODIGO_RIESGO` (VARCHAR2(30), Not Null): Código copiado del riesgo (Índice).
* `PROY_AREA_PRINCIPAL` (VARCHAR2(100), Not Null).
* `PROY_VRI` (NUMBER(3), Not Null): Valor del Riesgo Inherente (1-9).
* `PROY_VRR` (NUMBER(3), Not Null): Valor del Riesgo Residual (1-9).
* `PROY_NIVEL_INHERENTE` (VARCHAR2(20), Not Null).
* `PROY_NIVEL_RESIDUAL` (VARCHAR2(20), Not Null) (Índice).
* `PROY_RESPUESTA_RIESGO` (VARCHAR2(50), Not Null).
* `PROY_ESTADO_EVALUACION` (VARCHAR2(30), Default 'BORRADOR') (Índice).
* `PROY_DUENO_RIESGO` (VARCHAR2(150), Not Null) (Índice).
* `PROY_FECHA_EVAL` (DATE, Not Null).

### 2.11 Tabla: `RL_MR_FLUJOS_EVALUACION`
Trazabilidad de estados del ciclo de vida de la evaluación.
* `FLU_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_FLUJOS`.
* `FLU_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
* `FLU_ESTADO` (VARCHAR2(30), Not Null): `BORRADOR`, `EN_REVISION`, `OBSERVADA`, `APROBADA`, `RECHAZADA`, `CERRADA`.
* `FLU_MOTIVO` (VARCHAR2(1000), Null): Requerido en caso de rechazo u observación.
* `FLU_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
* `FLU_FECHA` (DATE, Default SYSDATE).

### 2.12 Tabla: `RL_MR_CONTROLES_RIESGO`
Controles asociados al riesgo en su evaluación.
* `CON_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_CONTROLES`.
* `CON_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
* `CON_TIPO` (VARCHAR2(30), Not Null): `PREVENTIVO`, `DETECTIVO`, `CORRECTIVO`.
* `CON_DESCRIPCION` (VARCHAR2(500), Not Null).
* `CON_AUTOMATIZACION` (VARCHAR2(30), Not Null): `MANUAL`, `SEMIAUTOMATICO`, `AUTOMATICO`.
* `CON_ESTADO` (VARCHAR2(20), Not Null): ej. `ACTIVO`.

### 2.13 Tabla: `RL_MR_EVALUACIONES_CONTROL`
Mediciones cuantitativas de efectividad del control.
* `ECO_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_EVAL_CONTROLES`.
* `ECO_CONTROL_ID` (NUMBER(15), Not Null): FK a `RL_MR_CONTROLES_RIESGO`.
* `ECO_EFECTIVIDAD` (NUMBER(5,2), Not Null): Porcentaje (0.00 a 100.00).
* `ECO_COMENTARIO` (VARCHAR2(500), Null).

### 2.14 Tabla: `RL_MR_PLANES`
Planes de mitigación y tratamiento.
* `PLA_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_PLANES`.
* `PLA_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
* `PLA_DESCRIPCION` (VARCHAR2(500), Not Null).
* `PLA_AVANCE` (NUMBER(5,2), Default 0): Porcentaje de avance.
* `PLA_PRESUPUESTO` (NUMBER(15,2), Default 0): Costo estimado.
* `PLA_FECHA_INICIO` (DATE, Not Null).
* `PLA_FECHA_FIN` (DATE, Not Null).
* `PLA_ESTADO` (VARCHAR2(30), Not Null).

### 2.15 Tabla: `RL_MR_ACTIVIDADES`
Actividades en las que se descompone cada plan.
* `ACT_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_ACTIVIDADES`.
* `ACT_PLAN_ID` (NUMBER(15), Not Null): FK a `RL_MR_PLANES`.
* `ACT_DESCRIPCION` (VARCHAR2(500), Not Null).
* `ACT_RESPONSABLE` (VARCHAR2(150), Not Null).
* `ACT_AVANCE` (NUMBER(5,2), Default 0).
* `ACT_FECHA_INICIO` (DATE, Not Null).
* `ACT_FECHA_FIN` (DATE, Not Null).
* `ACT_ESTADO` (VARCHAR2(30), Not Null).

### 2.16 Tabla: `RL_MR_EVIDENCIAS`
Metadatos de archivos adjuntos.
* `EVI_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_EVIDENCIAS`.
* `EVI_NOMBRE_ARCHIVO` (VARCHAR2(255), Not Null).
* `EVI_EXTENSION` (VARCHAR2(10), Not Null).
* `EVI_TAMANO` (NUMBER(15), Not Null): Tamaño en bytes.
* `EVI_HASH` (VARCHAR2(64), Not Null): HASH SHA-256 para validación de integridad.
* `EVI_RUTA` (VARCHAR2(500), Not Null): Ruta de almacenamiento físico/S3.
* `EVI_USR_CREACION` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
* `EVI_FECHA_CREACION` (DATE, Default SYSDATE).

### 2.17 Tabla: `RL_MR_EVI_EVALUACION`
Tabla asociativa evaluación-evidencia con integridad referencial física.
* `EVE_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
* `EVE_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
* Clave Primaria Compuesta (`EVE_EVALUACION_ID`, `EVE_EVIDENCIA_ID`).

### 2.18 Tabla: `RL_MR_EVI_CONTROL`
Tabla asociativa control-evidencia con integridad referencial física.
* `EVC_CONTROL_ID` (NUMBER(15), Not Null): FK a `RL_MR_CONTROLES_RIESGO`.
* `EVC_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
* Clave Primaria Compuesta (`EVC_CONTROL_ID`, `EVC_EVIDENCIA_ID`).

### 2.19 Tabla: `RL_MR_EVI_ACTIVIDAD`
Tabla asociativa actividad-evidencia con integridad referencial física.
* `EVA_ACTIVIDAD_ID` (NUMBER(15), Not Null): FK a `RL_MR_ACTIVIDADES`.
* `EVA_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
* Clave Primaria Compuesta (`EVA_ACTIVIDAD_ID`, `EVA_EVIDENCIA_ID`).

### 2.20 Tabla: `RL_MR_SENALES_ALERTA`
Registro de alertas gatilladas por automonitoreo.
* `ALE_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_SENALES`.
* `ALE_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
* `ALE_CODIGO` (VARCHAR2(50), Not Null).
* `ALE_INDICADOR` (VARCHAR2(150), Not Null).
* `ALE_ESTADO` (VARCHAR2(30), Default 'INACTIVO') (CHECK IN ('ACTIVO', 'INACTIVO')).
* `ALE_FECHA_DISPARO` (DATE, Null).

### 2.21 Tabla: `RL_MR_AUTOMONITOREO`
Log periódico de automonitoreo.
* `MON_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_AUTOMONITOREO`.
* `MON_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
* `MON_ESTADO_RIESGO` (VARCHAR2(30), Not Null).
* `MON_ESTADO_CONTR` (VARCHAR2(30), Not Null).
* `MON_RESULTADO` (VARCHAR2(1000), Not Null).
* `MON_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
* `MON_FECHA` (DATE, Default SYSDATE).

### 2.22 Tabla: `RL_MR_CATALOGOS`
Cabeceras de catálogos paramétricos.
* `CAT_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_CATALOGOS`.
* `CAT_CODIGO` (VARCHAR2(50), Not Null Unique): ej. `CAT_FRECUENCIA`.
* `CAT_NOMBRE` (VARCHAR2(150), Not Null).
* `CAT_ACTIVO` (NUMBER(1), Default 1).

### 2.23 Tabla: `RL_MR_ELEMENTOS_CATALOGO`
Opciones que componen cada catálogo paramétrico.
* `ELE_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_ELEMENTOS`.
* `ELE_CATALOGO_ID` (NUMBER(15), Not Null): FK a `RL_MR_CATALOGOS`.
* `ELE_CODIGO` (VARCHAR2(50), Not Null).
* `ELE_VALOR` (VARCHAR2(255), Not Null).
* `ELE_ORDEN` (NUMBER(5), Default 0).
* `ELE_ACTIVO` (NUMBER(1), Default 1).

### 2.24 Tabla: `RL_MR_REGLAS_CALCULO`
Metadatos de lógica y ponderaciones en C#.
* `REG_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_REGLAS`.
* `REG_CODIGO` (VARCHAR2(50), Not Null).
* `REG_VERSION` (VARCHAR2(20), Not Null).
* `REG_NOMBRE` (VARCHAR2(150), Not Null).
* `REG_ALGORITMO_ID` (VARCHAR2(100), Not Null): Enlace a C# (Índice).
* `REG_ACTIVA` (NUMBER(1), Default 1).

### 2.25 Tabla: `RL_MR_TRAZAS_CALCULO`
Bitácoras de cómputo para auditoría matemática.
* `TRA_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_TRAZAS`.
* `TRA_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
* `TRA_REGLA_ID` (NUMBER(15), Not Null): FK a `RL_MR_REGLAS_CALCULO`.
* `TRA_ENTRADAS_JSON` (CLOB, Not Null): Parámetros (CHECK IS JSON).
* `TRA_RESULTADOS_JSON` (CLOB, Not Null): Salidas (CHECK IS JSON).
* `TRA_FECHA` (DATE, Default SYSDATE).
* `TRA_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.

### 2.26 Tabla: `RL_MR_LOTES_IMPORTACION`
Lotes del Excel de migración inicial.
* `LOT_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_LOTES`.
* `LOT_HASH_EXCEL` (VARCHAR2(64), Not Null).
* `LOT_ESTADO` (VARCHAR2(30), Not Null).
* `LOT_FECHA` (DATE, Default SYSDATE).
* `LOT_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.

### 2.27 Tabla: `RL_MR_DETALLES_IMPORTACION`
Filas y logs de procesamiento de la importación.
* `DET_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_DETALLES_IMP`.
* `DET_LOTE_ID` (NUMBER(15), Not Null): FK a `RL_MR_LOTES_IMPORTACION`.
* `DET_INDICE_FILA` (NUMBER(10), Not Null).
* `DET_CODIGO_RIESGO` (VARCHAR2(30), Null).
* `DET_ESTADO` (VARCHAR2(20), Not Null).
* `DET_ERROR_LOG` (CLOB, Null).

### 2.28 Tabla: `RL_MR_AUDITORIA`
Log transaccional granular a nivel de campo JSON.
* `AUD_ID` (NUMBER(20), Not Null): Clave primaria por `SEQ_RL_MR_AUDITORIA`.
* `AUD_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
* `AUD_CAMPO_CLAVE` (VARCHAR2(100), Not Null): Campo modificado.
* `AUD_VALOR_ANT` (CLOB, Null).
* `AUD_VALOR_NVO` (CLOB, Null).
* `AUD_IP` (VARCHAR2(45), Null).
* `AUD_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
* `AUD_FECHA` (DATE, Default SYSDATE).

---

## 3. Contratos de Datos y DTOs en el Backend (C# - Contracts)

Los contratos residirán bajo la ubicación existente de la arquitectura monolítica:
`Features/MatricesRiesgos/Contracts`

### 3.1 DTO de Creación (Riesgo Nuevo)
```csharp
namespace RL.API.Features.MatricesRiesgos.Contracts
{
    public sealed class CrearEvaluacionRiesgoRequest
    {
        public long VersionFormularioId { get; set; }
        public System.Text.Json.JsonElement RespuestasDinamicas { get; set; } // Payload dinámico JSON
        public string IpOrigen { get; set; } = string.Empty;
    }
}
```

### 3.2 DTO de Actualización (Riesgo Existente)
```csharp
namespace RL.API.Features.MatricesRiesgos.Contracts
{
    public sealed class ActualizarEvaluacionRiesgoRequest
    {
        public long EvaluacionId { get; set; }
        public long RiesgoId { get; set; }
        public long VersionFormularioId { get; set; }
        public long VersionRow { get; set; } // Concurrencia optimista
        public string Estado { get; set; } = "BORRADOR"; // Borrador, En Revisión, Aprobada, etc.
        public System.Text.Json.JsonElement RespuestasDinamicas { get; set; }
        public string IpOrigen { get; set; } = string.Empty;
    }
}
```

---

## 4. Casos Teóricos de Validación de Paridad (Excel de 59 Riesgos)

Las fórmulas de cálculo del VRI (Valor del Riesgo Inherente) y VRR (Valor del Riesgo Residual) de la metodología aprobada se validarán de acuerdo a los siguientes escenarios y ponderaciones del Excel de 59 Riesgos:

### 4.1 Escenario 1 (Riesgo Inherente)
* *Entradas:* Frecuencia = 4, Impacto = 5.
* *Fórmula VRI:* `VRI = Frecuencia + Impacto - 1`
* *Cálculo:* `VRI = 4 + 5 - 1 = 8`.
* *Nivel Inherente:* `8` clasifica como **Alto / Crítico** según la escala aditiva (rango 1 a 9 del Excel).

### 4.2 Escenario 2 (Ponderación ETP - Efectividad de Controles)
* *Fórmula:* Basada exclusivamente en los tipos de control Preventivo (70%), Detectivo (15%) y Correctivo (15%):
  `ETP = (Preventivo x 0.70) + (Detectivo x 0.15) + (Correctivo x 0.15)`
* *Entradas:* Preventivo = 100% (1.00), Detectivo = 80% (0.80), Correctivo = 90% (0.90).
* *Cálculo:* `(1.00 x 0.70) + (0.80 x 0.15) + (0.90 x 0.15) = 0.70 + 0.12 + 0.135 = 0.955 = 95.5%`.

### 4.3 Escenario 3 (Riesgo Residual)
* *Entradas:* VRI = 8, ETP = 95.5%.
* *Fórmula VRR:* `VRR = ROUND(MAX(1, VRI x (1 - ETP/100)), 0)` (para ETP expresado como porcentaje).
* *Cálculo:* `VRR = ROUND(MAX(1, 8 x (1 - 0.955)), 0) = ROUND(MAX(1, 0.36), 0) = ROUND(1, 0) = 1`.
* *Resultado:* El valor entero resultante es `1`.

### 4.4 Escenario 4 (Coherencia Residual - Mapa de Calor)
* *Validación Compuesta:* El sistema validará que se cumpla la coherencia residual aritmética:
  `VRR 2 = Frecuencia residual + Impacto residual - 1`
  La evaluación del mapa de calor conciliará esta igualdad antes de permitir el cierre o aprobación de la evaluación.
