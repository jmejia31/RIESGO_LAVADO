# Diccionario Físico y Especificación de Contratos JSON
## Módulo: Matrices de Riesgos (SGRLA - IHSS)
### Versión: 1.0
### Estado: Preparado para revisión y aprobación

Este documento detalla la especificación formal del modelo físico de base de datos de **34 tablas relacionales** bajo el prefijo **`RL_MR_*`**, la definición del contrato JSON propietario del IHSS y los DTOs de acoplamiento del backend para el nuevo módulo modular de Matrices de Riesgos.

---

## 1. Contrato JSON Propietario de Metadatos de Formulario (Formulario A - Versión 1)

El formulario dinámico se parametriza mediante un esquema JSON almacenado en la tabla `RL_MR_VERSIONES_FORMULARIO`. La estructura controla de forma directa el renderizado de la interfaz en Angular y la validación en C#.

### 1.1 Especificación Formal de Propiedades del Contrato JSON
* **`versionContrato`** (String): Identificador de la versión del contrato de metadatos (ej. `"1.0"`).
* **`codigoFormulario`** (String): Código funcional único del formulario (ej. `"MATRIZ_RIESGOS_LAFT"`).
* **`nombreFormulario`** (String): Etiqueta descriptiva para visualización.
* **`version`** (Number): Entero incremental que define la versión de la plantilla.
* **`activo`** (Boolean): Define si todo el esquema de formulario se encuentra activo en el sistema.
* **`secciones`** (Array): Lista estructurada de bloques del formulario:
  * **`identificador`** (String): ID semántico único de la sección.
  * **`titulo`** (String): Encabezado visible.
  * **`orden`** (Number): Secuencia de renderizado (1-indexed).
  * **`campos`** (Array): Controles que componen la sección:
    * **`identificador`** (String): ID técnico del campo.
    * **`claveCanonica`** (String): Mapeo semántico directo con el diccionario físico y lógico del IHSS (ej. `"areaPrincipalId"`).
    * **`etiqueta`** (String): Nombre visible del control en la pantalla.
    * **`tipo`** (String): Tipo de control. Valores permitidos:
      * `"texto"`: Input de texto alfanumérico simple.
      * `"texto-largo"`: TextArea para comentarios o justificaciones extensas.
      * `"entero"`: Entrada de números enteros.
      * `"decimal"`: Entrada de números con punto decimal.
      * `"porcentaje"`: Entrada limitada de 0.00 a 100.00.
      * `"moneda"`: Entrada con formato monetario y soporte de divisas.
      * `"fecha"`: Selector de fecha simple (D/M/A).
      * `"fecha-hora"`: Selector de fecha y hora.
      * `"casilla"`: Control de selección simple tipo boolean (0 o 1).
      * `"seleccion-unica"`: RadioButtons o selector de opción única.
      * `"seleccion-multiple"`: Lista de CheckBoxes.
      * `"selector-catalogo"`: ComboBox enlazado de forma directa a una cabecera de `RL_MR_CATALOGOS`.
      * `"archivo"`: Botón para subir un único archivo adjunto.
      * `"archivos-multiples"`: Botón para adjuntar múltiples archivos.
      * `"grupo"`: Bloque contenedor para agrupar campos relacionados.
      * `"tabla-repetible"`: Estructura de grilla repetible para capturar múltiples registros homogéneos.
      * `"campo-calculado"`: Control de sólo lectura que computa su valor a partir de otros campos (ej. VRI).
      * `"campo-auxiliar"`: Campo técnico de soporte de cálculo no expuesto.
      * `"semaforo"`: Indicador de color condicional según umbrales de escala.
      * `"encabezado"`: Separador visual con texto decorativo.
      * `"texto-informativo"`: Párrafo informativo o guía legal.
    * **`codigoCatalogo`** (String, Condicional): Nombre del catálogo a cargar si el tipo es `"selector-catalogo"`.
    * **`valorPredeterminado`** (String/Number/Boolean, Opcional): Valor inicial asignado al control.
    * **`orden`** (Number): Posición de despliegue del campo en su sección.
    * **`activo`** (Boolean): Habilitación lógica del campo.
    * **`soloLectura`** (Boolean): Bloquea la edición del campo.
    * **`ayuda`** (String, Opcional): Tooltip o texto explicativo de ayuda al usuario.
    * **`validaciones`** (Object, Opcional): Reglas de validación aplicables:
      * **`obligatorio`** (Boolean): Validación de no nulo.
      * **`expresionObligatoriedad`** (String): Expresión lógica de obligatoriedad condicionada.
      * **`longitudMinima`** / **`longitudMaxima`** (Number): Rangos de longitud de texto.
      * **`minimo`** / **`maximo`** (Number): Valores límite para enteros, decimales y porcentajes.
      * **`patron`** (String): Expresión regular Regex para validaciones avanzadas (ej. correos, teléfonos).
    * **`visibilidad`** (Object, Opcional):
      * **`visible`** (Boolean): Por defecto visible.
      * **`expresionCondicional`** (String): Condición lógica para ocultar/mostrar dinámicamente en el frontend (ej. `"mostrar si areaPrincipalId == 1"`).
    * **`permisos`** (Object, Opcional):
      * Restricciones de lectura/escritura por rol a nivel de campo.
    * **`consolidacion`** / **`exportacion`** (Object, Opcional):
      * Directivas para generación de reportes automáticos Excel/PDF.

### 1.2 Comportamiento de Validaciones y Propiedades Desconocidas o Faltantes
* **Propiedades Desconocidas:** Si el payload del formulario enviado por el Frontend contiene propiedades dinámicas no definidas en el contrato JSON de metadatos de la versión activa del formulario, el Backend descartará dichas propiedades de forma automática durante la sanitización del payload para evitar contaminación del CLOB inmutable, registrando una advertencia no bloqueante en los logs del servidor.
* **Propiedades Obligatorias Faltantes:** Si el payload omite algún campo marcado como `"obligatorio": true` en el contrato de metadatos, o cuya `"expresionObligatoriedad"` evalúe como verdadera, el motor de validación del Backend rechazará la transacción inmediatamente, arrojando una excepción HTTP 400 (Bad Request) estructurada con el código de error `VAL_CAMPO_OBLIGATORIO` y la clave canónica del campo faltante, impidiendo su almacenamiento o transición de estado en base de datos.

### 1.3 Ejemplo de Configuración JSON Completa (Formulario A - Versión 1)
```json
{
  "versionContrato": "1.0",
  "codigoFormulario": "MATRIZ_RIESGOS_LAFT",
  "nombreFormulario": "Matriz de Riesgos LA/FT - Formulario A",
  "version": 1,
  "activo": true,
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
          "valorPredeterminado": "",
          "orden": 1,
          "activo": true,
          "soloLectura": false,
          "ayuda": "Seleccione la división u oficina responsable del riesgo",
          "validaciones": {
            "obligatorio": true
          }
        },
        {
          "identificador": "tipo_riesgo",
          "claveCanonica": "tipoRiesgo",
          "etiqueta": "Tipo de Riesgo",
          "tipo": "texto",
          "valorPredeterminado": "Operativo",
          "orden": 2,
          "activo": true,
          "soloLectura": false,
          "validaciones": {
            "obligatorio": true,
            "longitudMaxima": 150
          }
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
          "activo": true,
          "soloLectura": false,
          "validaciones": {
            "obligatorio": true,
            "longitudMaxima": 1000
          }
        },
        {
          "identificador": "frecuencia_inherente_id",
          "claveCanonica": "frecuenciaInherenteId",
          "etiqueta": "Frecuencia",
          "tipo": "selector-catalogo",
          "codigoCatalogo": "CAT_FRECUENCIA",
          "orden": 2,
          "activo": true,
          "soloLectura": false,
          "validaciones": {
            "obligatorio": true
          }
        },
        {
          "identificador": "impacto_inherente_id",
          "claveCanonica": "impactoInherenteId",
          "etiqueta": "Impacto",
          "tipo": "selector-catalogo",
          "codigoCatalogo": "CAT_IMPACTO",
          "orden": 3,
          "activo": true,
          "soloLectura": false,
          "validaciones": {
            "obligatorio": true
          }
        },
        {
          "identificador": "valor_riesgo_inherente",
          "claveCanonica": "vri",
          "etiqueta": "Valor Riesgo Inherente",
          "tipo": "campo-calculado",
          "orden": 4,
          "activo": true,
          "soloLectura": true,
          "ayuda": "VRI = Frecuencia + Impacto - 1"
        }
      ]
    }
  ]
}
```

---

## 2. Diccionario de Datos Físico Definitivo (34 Tablas)

A continuación se detalla la especificación física y reglas de integridad para la totalidad de las 34 tablas relacionales del nuevo módulo:

### 2.1 Tablas de Definición de Formularios y Seguridad (5 Tablas)
1. **`RL_MR_FAMILIAS_FORMULARIO`**: Agrupación lógica de matrices.
   * `FAM_ID` (NUMBER(15), Not Null): Clave primaria generada por `SEQ_RL_MR_FAMILIAS`.
   * `FAM_CODIGO` (VARCHAR2(50), Not Null): Código semántico único (UQ).
   * `FAM_NOMBRE` (VARCHAR2(150), Not Null): Nombre visible del grupo.
   * `FAM_DESCRIPCION` (VARCHAR2(500), Null): Nota descriptiva.
   * `FAM_ACTIVO` (NUMBER(1), Not Null, Default 1): Borrado lógico (0=Inactivo, 1=Activo) (CHECK IN (0,1)).
   * `FAM_FECHA_CREACION` (DATE, Not Null, Default SYSDATE).
2. **`RL_MR_VERSIONES_FORMULARIO`**: Configuración JSON inmutable del formulario.
   * `VER_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_VERSIONES`.
   * `VER_FAMILIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_FAMILIAS_FORMULARIO`.
   * `VER_CODIGO` (VARCHAR2(30), Not Null): Identificador visual (ej. `FORM_A`).
   * `VER_VERSION` (NUMBER(5), Not Null): Número de versión secuencial.
   * `VER_JSON` (CLOB, Not Null): Estructura de campos (CHECK IS JSON).
   * `VER_HASH` (VARCHAR2(64), Not Null): Hash SHA-256 inmutable de la configuración.
   * `VER_ESTADO` (VARCHAR2(20), Not Null, Default 'DRAFT'): CHECK IN ('DRAFT', 'IN_REVIEW', 'APPROVED', 'PUBLISHED', 'RETIRED', 'ARCHIVED').
   * `VER_VIGENTE` (NUMBER(1), Not Null, Default 0): Vigencia exclusiva por familia (CHECK IN (0,1)).
   * `VER_FECHA_INICIO` (DATE, Null).
   * `VER_FECHA_FIN` (DATE, Null).
   * `VER_USR_CREACION` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
3. **`RL_MR_CAMPOS_FORMULARIO`**: Registro semántico de campos canónicos.
   * `CAM_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_CAMPOS`.
   * `CAM_CLAVE_CANONICA` (VARCHAR2(100), Not Null): Código canónico funcional único (UQ) (ej. `areaPrincipalId`).
   * `CAM_NOMBRE_MOSTRAR` (VARCHAR2(150), Not Null): Texto visible por defecto.
   * `CAM_TIPO_DATO` (VARCHAR2(50), Not Null): ej. `NUMBER`, `VARCHAR2`.
4. **`RL_MR_APROBACIONES_FORMULARIO`**: Historial de firmas y transiciones de publicación.
   * `APR_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_APROBACIONES`.
   * `APR_VERSION_ID` (NUMBER(15), Not Null): FK a `RL_MR_VERSIONES_FORMULARIO`.
   * `APR_ESTADO_NVO` (VARCHAR2(20), Not Null): Estado al que transiciona.
   * `APR_COMENTARIO` (VARCHAR2(1000), Null): Observaciones.
   * `APR_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
   * `APR_FECHA` (DATE, Default SYSDATE).
5. **`RL_MR_PERMISOS_FORMULARIO`**: Matriz de seguridad con soporte granular por Rol.
   * `PER_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_PERMISOS`.
   * `PER_VERSION_ID` (NUMBER(15), Not Null): FK a `RL_MR_VERSIONES_FORMULARIO`.
   * `PER_ROL_ID` (NUMBER(3), Not Null): Identificador de rol institucional.
   * `PER_AMBITO` (VARCHAR2(20), Not Null): Alcance del permiso (CHECK IN ('FORMULARIO', 'SECCION', 'CAMPO')).
   * `PER_OBJETIVO_CLAVE` (VARCHAR2(100), Not Null): Identificador o Clave Canónica del objetivo (sección, campo o código de formulario).
   * `PER_PERMISO` (VARCHAR2(20), Not Null): CHECK IN ('LECTURA', 'EDICION', 'OCULTO').

### 2.2 Tablas del Ciclo de Vida del Riesgo y Evaluaciones (6 Tablas)
6. **`RL_MR_RIESGOS`**: Identidad inmutable y permanente del riesgo.
   * `RIE_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_RIESGOS`.
   * `RIE_CODIGO` (VARCHAR2(30), Not Null): Código incremental autogenerado único (UQ) (ej. `RIE-0001`).
   * `RIE_FECHA_CREACION` (DATE, Default SYSDATE).
   * `RIE_ACTIVO` (NUMBER(1), Default 1): Borrado lógico (CHECK IN (0,1)).
7. **`RL_MR_RELACIONES_RIESGO`**: Relaciones de transversalidad entre riesgos padres e hijos.
   * `REL_RIE_PADRE_ID` (NUMBER(15), Not Null): FK a `RL_MR_RIESGOS`.
   * `REL_RIE_HIJO_ID` (NUMBER(15), Not Null): FK a `RL_MR_RIESGOS`.
   * Clave Primaria Compuesta (`REL_RIE_PADRE_ID`, `REL_RIE_HIJO_ID`).
8. **`RL_MR_EVALUACIONES_RIESGO`**: Respuestas e instancias de evaluación del riesgo.
   * `EVA_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_EVALUACIONES`.
   * `EVA_RIESGO_ID` (NUMBER(15), Not Null): FK a `RL_MR_RIESGOS`.
   * `EVA_VERSION_ID` (NUMBER(15), Not Null): FK a `RL_MR_VERSIONES_FORMULARIO`.
   * `EVA_DATA_JSON` (CLOB, Not Null): Respuestas dinámicas (CHECK IS JSON).
   * `EVA_DATA_CALC_JSON` (CLOB, Not Null): Resultados del cómputo oficial (CHECK IS JSON).
   * `EVA_FECHA_REGISTRO` (DATE, Default SYSDATE).
   * `EVA_USR_REGISTRO` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
   * `EVA_VERSION_ROW` (NUMBER(10), Default 1): Versión incremental de fila para concurrencia optimista.
   * `EVA_ACTIVO` (NUMBER(1), Default 1): Borrado lógico (CHECK IN (0,1)).
9. **`RL_MR_REVISIONES_EVALUACION`**: Instantáneas históricas de auditoría transaccional.
   * `REV_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_REVISIONES`.
   * `REV_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
   * `REV_DATOS_JSON` (CLOB, Not Null): Snapshot completo (CHECK IS JSON).
   * `REV_FECHA` (DATE, Default SYSDATE).
   * `REV_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
10. **`RL_MR_PROYECCIONES_EVALUACION`**: Estructura relacional plana optimizada para búsquedas rápidas.
    * `PROY_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_PROYECCIONES`.
    * `PROY_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
    * `PROY_CODIGO_RIESGO` (VARCHAR2(30), Not Null): Código copiado del riesgo (Índice).
    * `PROY_AREA_PRINCIPAL` (VARCHAR2(100), Not Null).
    * `PROY_VRI` (NUMBER(3), Not Null): Valor del Riesgo Inherente (1-9) (CHECK BETWEEN 1 AND 9).
    * `PROY_VRR` (NUMBER(3), Not Null): Valor del Riesgo Residual (1-9) (CHECK BETWEEN 1 AND 9).
    * `PROY_NIVEL_INHERENTE` (VARCHAR2(20), Not Null).
    * `PROY_NIVEL_RESIDUAL` (VARCHAR2(20), Not Null) (Índice).
    * `PROY_RESPUESTA_RIESGO` (VARCHAR2(50), Not Null).
    * `PROY_ESTADO_EVALUACION` (VARCHAR2(30), Default 'BORRADOR') (Índice).
    * `PROY_DUENO_RIESGO` (VARCHAR2(150), Not Null) (Índice).
    * `PROY_FECHA_EVAL` (DATE, Not Null).
11. **`RL_MR_FLUJOS_EVALUACION`**: Trazabilidad de estados del ciclo de vida de la evaluación.
    * `FLU_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_FLUJOS`.
    * `FLU_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
    * `FLU_ESTADO` (VARCHAR2(30), Not Null): `BORRADOR`, `EN_REVISION`, `OBSERVADA`, `APROBADA`, `RECHAZADA`, `CERRADA`.
    * `FLU_MOTIVO` (VARCHAR2(1000), Null): Requerido en caso de rechazo u observación.
    * `FLU_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
    * `FLU_FECHA` (DATE, Default SYSDATE).

### 2.3 Tablas de Controles, Mitigación y Evidencias (14 Tablas)
12. **`RL_MR_CONTROLES_RIESGO`**: Controles asociados al riesgo en su evaluación.
    * `CON_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_CONTROLES`.
    * `CON_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
    * `CON_TIPO` (VARCHAR2(30), Not Null): `PREVENTIVO`, `DETECTIVO`, `CORRECTIVO` (CHECK IN ('PREVENTIVO', 'DETECTIVO', 'CORRECTIVO')).
    * `CON_DESCRIPCION` (VARCHAR2(500), Not Null).
    * `CON_AUTOMATIZACION` (VARCHAR2(30), Not Null): `MANUAL`, `SEMIAUTOMATICO`, `AUTOMATICO` (CHECK IN ('MANUAL', 'SEMIAUTOMATICO', 'AUTOMATICO')).
    * `CON_ESTADO` (VARCHAR2(20), Not Null): ej. `ACTIVO`.
13. **`RL_MR_EVALUACIONES_CONTROL`**: Mediciones cuantitativas de efectividad del control.
    * `ECO_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_EVAL_CONTROLES`.
    * `ECO_CONTROL_ID` (NUMBER(15), Not Null): FK a `RL_MR_CONTROLES_RIESGO`.
    * `ECO_EFECTIVIDAD` (NUMBER(5,2), Not Null): Porcentaje (0.00 a 100.00) (CHECK BETWEEN 0 AND 100).
    * `ECO_COMENTARIO` (VARCHAR2(500), Null).
14. **`RL_MR_PLANES`**: Planes de mitigación y tratamiento.
    * `PLA_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_PLANES`.
    * `PLA_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
    * `PLA_DESCRIPCION` (VARCHAR2(500), Not Null).
    * `PLA_AVANCE` (NUMBER(5,2), Default 0): Porcentaje de avance (CHECK BETWEEN 0 AND 100).
    * `PLA_PRESUPUESTO` (NUMBER(15,2), Default 0): Costo estimado (CHECK >= 0).
    * `PLA_FECHA_INICIO` (DATE, Not Null).
    * `PLA_FECHA_FIN` (DATE, Not Null).
    * `PLA_ESTADO` (VARCHAR2(30), Not Null).
15. **`RL_MR_ACTIVIDADES`**: Actividades en las que se descompone cada plan.
    * `ACT_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_ACTIVIDADES`.
    * `ACT_PLAN_ID` (NUMBER(15), Not Null): FK a `RL_MR_PLANES`.
    * `ACT_DESCRIPCION` (VARCHAR2(500), Not Null).
    * `ACT_RESPONSABLE` (VARCHAR2(150), Not Null).
    * `ACT_AVANCE` (NUMBER(5,2), Default 0) (CHECK BETWEEN 0 AND 100).
    * `ACT_FECHA_INICIO` (DATE, Not Null).
    * `ACT_FECHA_FIN` (DATE, Not Null).
    * `ACT_ESTADO` (VARCHAR2(30), Not Null).
16. **`RL_MR_EVIDENCIAS`**: Metadatos centralizados de archivos adjuntos.
    * `EVI_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_EVIDENCIAS`.
    * `EVI_NOMBRE_ARCHIVO` (VARCHAR2(255), Not Null).
    * `EVI_EXTENSION` (VARCHAR2(10), Not Null).
    * `EVI_TAMANO` (NUMBER(15), Not Null): Tamaño en bytes (CHECK > 0).
    * `EVI_HASH` (VARCHAR2(64), Not Null): HASH SHA-256 para validación de integridad.
    * `EVI_RUTA` (VARCHAR2(500), Not Null): Ruta de almacenamiento físico/S3.
    * `EVI_USR_CREACION` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
    * `EVI_FECHA_CREACION` (DATE, Default SYSDATE).

#### Tablas Asociativas de Evidencias (Trazabilidad Física Directa Completa)
Para garantizar integridad referencial estricta al 100% bajo llaves foráneas reales en Oracle, se definen las siguientes tablas de cruce físico directas para cada entidad:
17. **`RL_MR_EVI_RIESGO`**:
    * `EVR_RIESGO_ID` (NUMBER(15), Not Null): FK a `RL_MR_RIESGOS`.
    * `EVR_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
    * Clave Primaria Compuesta (`EVR_RIESGO_ID`, `EVR_EVIDENCIA_ID`).
18. **`RL_MR_EVI_EVALUACION`**:
    * `EVE_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
    * `EVE_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
    * Clave Primaria Compuesta (`EVE_EVALUACION_ID`, `EVE_EVIDENCIA_ID`).
19. **`RL_MR_EVI_CONTROL`**:
    * `EVC_CONTROL_ID` (NUMBER(15), Not Null): FK a `RL_MR_CONTROLES_RIESGO`.
    * `EVC_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
    * Clave Primaria Compuesta (`EVC_CONTROL_ID`, `EVC_EVIDENCIA_ID`).
20. **`RL_MR_EVI_PLAN`**:
    * `EVP_PLAN_ID` (NUMBER(15), Not Null): FK a `RL_MR_PLANES`.
    * `EVP_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
    * Clave Primaria Compuesta (`EVP_PLAN_ID`, `EVP_EVIDENCIA_ID`).
21. **`RL_MR_EVI_ACTIVIDAD`**:
    * `EVA_ACTIVIDAD_ID` (NUMBER(15), Not Null): FK a `RL_MR_ACTIVIDADES`.
    * `EVA_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
    * Clave Primaria Compuesta (`EVA_ACTIVIDAD_ID`, `EVA_EVIDENCIA_ID`).
22. **`RL_MR_EVI_ALERTA`**:
    * `EVA_ALERTA_ID` (NUMBER(15), Not Null): FK a `RL_MR_SENALES_ALERTA`.
    * `EVA_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
    * Clave Primaria Compuesta (`EVA_ALERTA_ID`, `EVA_EVIDENCIA_ID`).
23. **`RL_MR_EVI_AUTOMONITOREO`**:
    * `EVM_MONITOREO_ID` (NUMBER(15), Not Null): FK a `RL_MR_AUTOMONITOREO`.
    * `EVM_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
    * Clave Primaria Compuesta (`EVM_MONITOREO_ID`, `EVM_EVIDENCIA_ID`).
24. **`RL_MR_EVI_REVISION`**:
    * `EVV_REVISION_ID` (NUMBER(15), Not Null): FK a `RL_MR_REVISIONES_EVALUACION`.
    * `EVV_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
    * Clave Primaria Compuesta (`EVV_REVISION_ID`, `EVV_EVIDENCIA_ID`).
25. **`RL_MR_EVI_APROBACION`**:
    * `EVAP_APROBACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_APROBACIONES_FORMULARIO`.
    * `EVAP_EVIDENCIA_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVIDENCIAS`.
    * Clave Primaria Compuesta (`EVAP_APROBACION_ID`, `EVAP_EVIDENCIA_ID`).

### 2.4 Tablas Transversales de Cómputo, Carga y Seguimiento (9 Tablas)
26. **`RL_MR_SENALES_ALERTA`**: Registro de alertas gatilladas por automonitoreo.
    * `ALE_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_SENALES`.
    * `ALE_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
    * `ALE_CODIGO` (VARCHAR2(50), Not Null).
    * `ALE_INDICADOR` (VARCHAR2(150), Not Null).
    * `ALE_ESTADO` (VARCHAR2(30), Default 'INACTIVO') (CHECK IN ('ACTIVO', 'INACTIVO')).
    * `ALE_FECHA_DISPARO` (DATE, Null).
27. **`RL_MR_AUTOMONITOREO`**: Log periódico de automonitoreo.
    * `MON_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_AUTOMONITOREO`.
    * `MON_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
    * `MON_ESTADO_RIESGO` (VARCHAR2(30), Not Null).
    * `MON_ESTADO_CONTR` (VARCHAR2(30), Not Null).
    * `MON_RESULTADO` (VARCHAR2(1000), Not Null).
    * `MON_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
    * `MON_FECHA` (DATE, Default SYSDATE).
28. **`RL_MR_CATALOGOS`**: Cabeceras de catálogos paramétricos.
    * `CAT_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_CATALOGOS`.
    * `CAT_CODIGO` (VARCHAR2(50), Not Null Unique): ej. `CAT_FRECUENCIA`.
    * `CAT_NOMBRE` (VARCHAR2(150), Not Null).
    * `CAT_ACTIVO` (NUMBER(1), Default 1).
29. **`RL_MR_ELEMENTOS_CATALOGO`**: Opciones que componen cada catálogo paramétrico.
    * `ELE_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_ELEMENTOS`.
    * `ELE_CATALOGO_ID` (NUMBER(15), Not Null): FK a `RL_MR_CATALOGOS`.
    * `ELE_CODIGO` (VARCHAR2(50), Not Null).
    * `ELE_VALOR` (VARCHAR2(255), Not Null).
    * `ELE_ORDEN` (NUMBER(5), Default 0).
    * `ELE_ACTIVO` (NUMBER(1), Default 1).
30. **`RL_MR_REGLAS_CALCULO`**: Metadatos de lógica y ponderaciones en C#.
    * `REG_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_REGLAS`.
    * `REG_CODIGO` (VARCHAR2(50), Not Null).
    * `REG_VERSION` (VARCHAR2(20), Not Null).
    * `REG_NOMBRE` (VARCHAR2(150), Not Null).
    * `REG_ALGORITMO_ID` (VARCHAR2(100), Not Null): Enlace a C# (Índice).
    * `REG_ACTIVA` (NUMBER(1), Default 1).
31. **`RL_MR_TRAZAS_CALCULO`**: Bitácoras de cómputo para auditoría matemática.
    * `TRA_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_TRAZAS`.
    * `TRA_EVALUACION_ID` (NUMBER(15), Not Null): FK a `RL_MR_EVALUACIONES_RIESGO`.
    * `TRA_REGLA_ID` (NUMBER(15), Not Null): FK a `RL_MR_REGLAS_CALCULO`.
    * `TRA_ENTRADAS_JSON` (CLOB, Not Null): Parámetros (CHECK IS JSON).
    * `TRA_RESULTADOS_JSON` (CLOB, Not Null): Salidas (CHECK IS JSON).
    * `TRA_FECHA` (DATE, Default SYSDATE).
    * `TRA_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
32. **`RL_MR_LOTES_IMPORTACION`**: Lotes del Excel de migración inicial.
    * `LOT_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_LOTES`.
    * `LOT_HASH_EXCEL` (VARCHAR2(64), Not Null).
    * `LOT_ESTADO` (VARCHAR2(30), Not Null).
    * `LOT_FECHA` (DATE, Default SYSDATE).
    * `LOT_USR_ID` (NUMBER(10), Not Null): FK a `RL_USUARIOS`.
33. **`RL_MR_DETALLES_IMPORTACION`**: Filas y logs de procesamiento de la importación.
    * `DET_ID` (NUMBER(15), Not Null): Clave primaria por `SEQ_RL_MR_DETALLES_IMP`.
    * `DET_LOTE_ID` (NUMBER(15), Not Null): FK a `RL_MR_LOTES_IMPORTACION`.
    * `DET_INDICE_FILA` (NUMBER(10), Not Null).
    * `DET_CODIGO_RIESGO` (VARCHAR2(30), Null).
    * `DET_ESTADO` (VARCHAR2(20), Not Null).
    * `DET_ERROR_LOG` (CLOB, Null).
34. **`RL_MR_AUDITORIA`**: Log transaccional granular a nivel de campo JSON.
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
📁 **[Features/MatricesRiesgos/Contracts](../../backend/RL.API/Features/MatricesRiesgos/Contracts)**

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
