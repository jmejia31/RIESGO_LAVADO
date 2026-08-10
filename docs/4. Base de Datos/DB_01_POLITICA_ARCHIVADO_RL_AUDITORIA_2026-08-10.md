# DB-01 — Política de archivado de `RL_AUDITORIA`

**Fecha:** 2026-08-10  
**Repositorio:** `jmejia31/RIESGO_LAVADO`  
**Rama:** `desarrollo`  
**Base de inicio:** `ba8aaa9429aff7357bec12f0e8f1bd4e9eb94aac`  
**Retención institucional aprobada:** **NO DEFINIDA**  
**Borrado automático:** **PROHIBIDO**  
**DDL/DML de archivo ejecutado en DB-01:** **NO**

---

## 1. Objetivo

Definir una política técnicamente segura para controlar el crecimiento histórico de `RL_AUDITORIA` sin degradar trazabilidad, integridad, investigación, cumplimiento ni capacidad probatoria.

DB-01 es una fase de **política, diseño, diagnóstico y controles de repositorio**. No crea una tabla histórica, no mueve registros y no elimina datos.

La regla principal es **NO DELETE AUTOMÁTICO**.

---

## 2. Estado actual verificado

### 2.1 Estructura física vigente

`RL_AUDITORIA` es la bitácora general y contiene:

- `AUD_ID NUMBER(20)` como clave primaria;
- `AUD_TABLA VARCHAR2(50)`;
- `AUD_REGISTRO_ID VARCHAR2(50)`;
- `AUD_ACCION VARCHAR2(10)`;
- `AUD_DATOS_ANT CLOB`;
- `AUD_DATOS_NVO CLOB`;
- `AUD_USR_ID NUMBER(10)`;
- `AUD_USR_EMAIL VARCHAR2(150)`;
- `AUD_IP VARCHAR2(45)`;
- `AUD_FECHA DATE`;
- `AUD_MODULO VARCHAR2(50)`.

La secuencia vigente es `SEQ_RL_AUDITORIA`. Los índices declarados actualmente son la PK, `IDX_RL_AUD_TABLA (AUD_TABLA)` e `IDX_RL_AUD_USR (AUD_USR_ID)`.

### 2.2 Contrato de aplicación vigente

El backend registra nuevos eventos mediante `INSERT INTO RL_AUDITORIA` con `SEQ_RL_AUDITORIA.NEXTVAL` y `SYSDATE`. La consulta funcional realiza conteo + paginación Oracle 11g y ordena por `AUD_FECHA DESC, AUD_ID DESC`, con filtros opcionales por acción, módulo, tabla, fechas y búsqueda textual.

DB-01 no cambia ese contrato.

### 2.3 Resultado heredado de DB-03

DB-03 ejecutó los perfiles Q09/Q10 de auditoría y cerró con `SIN_CAMBIO`. Con el volumen actual no se justifica crear un índice nuevo. En particular, la búsqueda `LIKE '%texto%'` no se convierte en candidata automática a B-tree.

La optimización por índices y la política de archivado son decisiones independientes.

---

## 3. Política de retención

### 3.1 Plazo

El repositorio no contiene una decisión institucional aprobada que establezca cuántos meses o años debe mantenerse la auditoría en línea o en histórico.

Por tanto:

> **Mientras el plazo de retención y la fecha de corte no sean aprobados por Cumplimiento/Legal, ningún registro se considera elegible para purga.**

DB-01 no establece por intuición un plazo de 12, 24, 60 meses ni cualquier otro.

### 3.2 Fecha de corte

Toda futura ejecución de archivo deberá recibir una fecha de corte explícita, aprobada y registrada en el manifiesto del lote. La fecha se basará en `AUD_FECHA` y nunca en la fecha de ejecución del proceso como sustituto implícito.

### 3.3 Retención extraordinaria / `LEGAL_HOLD`

Un caso abierto de investigación, requerimiento legal, incidente de seguridad, auditoría externa, litigio o instrucción de Cumplimiento puede bloquear el tratamiento histórico de los eventos relacionados aunque superen la fecha de corte general.

La implementación física futura deberá disponer de un mecanismo verificable para excluir esos eventos/lotes. DB-01 no inventa todavía su modelo físico.

---

## 4. Modelo operativo aprobado: `COPY_ONLY`

La primera implementación física futura deberá ser **solo de copia**:

1. identificar el conjunto candidato bajo una fecha de corte aprobada;
2. excluir registros sujetos a retención extraordinaria;
3. copiar al destino histórico previamente autorizado;
4. reconciliar origen e histórico;
5. certificar el lote y su manifiesto;
6. mantener `RL_AUDITORIA` intacta.

La copia no implica autorización de eliminación.

### Invariante de identidad

El histórico deberá preservar el `AUD_ID` original y todos los campos de auditoría necesarios para reconstruir el evento. No debe generar un identificador sustituto que impida reconciliar 1:1 con la fuente.

### Orden y consulta

El histórico deberá conservar semánticamente el orden `AUD_FECHA DESC, AUD_ID DESC` y filtros equivalentes cuando la aplicación incorpore consulta histórica. DB-01 no modifica aún el endpoint ni la UI.

---

## 5. Reconciliación obligatoria

Ningún lote se considera archivado/certificado solo porque una sentencia de copia finalice sin error.

Como mínimo deben registrarse:

- identificador único de lote;
- fecha/hora de inicio y fin;
- esquema y ambiente, sin credenciales;
- fecha de corte aprobada;
- cantidad de candidatos;
- cantidad copiada;
- `MIN(AUD_ID)` y `MAX(AUD_ID)` del lote;
- `MIN(AUD_FECHA)` y `MAX(AUD_FECHA)`;
- verificación de ausencia de `AUD_ID` faltantes respecto al conjunto candidato;
- verificación de ausencia de duplicados en destino;
- resultado `CONCILIADO` o `RECHAZADO`;
- responsable técnico y aprobador funcional.

Un lote con diferencia de conteo, duplicados, faltantes o error de integridad queda `RECHAZADO` y no habilita acciones posteriores.

---

## 6. Borrado y purga

### Prohibiciones DB-01

DB-01 **no autoriza**:

- borrado automático por antigüedad;
- `TRUNCATE` de auditoría;
- jobs periódicos de purga;
- triggers de limpieza;
- eliminación de un lote inmediatamente después de copiarlo;
- eliminación basada únicamente en falta de espacio o rendimiento;
- eliminación de evidencias por una fecha de corte no aprobada.

Cualquier política futura de eliminación, incluso manual, deberá ser una decisión separada de DB-01 y requerirá aprobación formal de Cumplimiento/Legal, evidencia de archivo reconciliado, recuperación probada y autorización técnica específica.

**Hasta esa aprobación separada, la fuente se conserva.**

---

## 7. Destino histórico futuro

DB-01 no crea DDL. Si posteriormente se aprueba una tabla o esquema histórico, su diseño deberá cumplir como mínimo:

- compatibilidad Oracle 11g;
- preservación de `AUD_ID` y campos de auditoría;
- CLOB conservados sin truncamiento silencioso;
- acceso no más permisivo que `RL_AUDITORIA`;
- separación clara entre escritura de auditoría activa y lectura histórica;
- trazabilidad de lote;
- posibilidad de restaurar/consultar el evento sin transformar su significado.

`RL_AUDITORIA_HIST` puede usarse como nombre conceptual en futuros diseños, pero **no existe ni queda autorizada su creación por DB-01**.

### Particionamiento

No se presupone que Oracle Partitioning esté licenciado/disponible. Cualquier propuesta de particionamiento requiere primero validar edición/licenciamiento y volumen real. No es una condición para cerrar DB-01.

---

## 8. Seguridad y privacidad

La auditoría contiene potencialmente CLOB de estado anterior/nuevo, identificadores, correo e IP. Por ello:

- el diagnóstico DB-01 versionado usa solo agregados;
- no imprime `AUD_DATOS_ANT`, `AUD_DATOS_NVO`, `AUD_USR_EMAIL` ni `AUD_IP`;
- ningún manifiesto debe incluir credenciales o cadenas de conexión;
- el histórico debe heredar controles de acceso de auditoría;
- exportaciones/evidencias deben sanear información sensible antes de versionarse.

---

## 9. Diagnóstico de crecimiento

Se incorpora `database/auditoria/archivado/01_db01_diagnostico_rl_auditoria_solo_lectura.sql` para levantar sin DML/DDL:

- total de registros;
- fecha mínima/máxima;
- crecimiento mensual;
- distribución por acción;
- distribución por módulo;
- top 20 de tablas auditadas por volumen;
- longitud agregada de los CLOB, sin revelar contenido.

El script es manual y de solo lectura. CI lo valida estáticamente y no conecta Oracle.

---

## 10. Umbrales y disparadores de reevaluación

DB-01 no fija un número arbitrario de filas como umbral de archivo. Debe reevaluarse la necesidad física cuando ocurra cualquiera de estos eventos:

- crecimiento material sostenido de `RL_AUDITORIA`;
- degradación observable de Q09/Q10 o de la bitácora funcional;
- presión de almacenamiento validada por DBA;
- requerimiento formal de Cumplimiento/Legal;
- cambio de política institucional de conservación;
- cambio de topología/licenciamiento Oracle.

Ante degradación de consultas, se vuelve a perfilar antes de crear índices, siguiendo DB-03.

---

## 11. Recuperación y reversibilidad

Antes de cualquier futura decisión que retire datos del almacenamiento activo debe existir una prueba de recuperación desde histórico. La prueba deberá demostrar:

1. localización por `AUD_ID`;
2. preservación de fecha, acción, módulo, usuario/contexto y CLOB;
3. equivalencia de los campos relevantes contra la fuente/manifiesto;
4. consulta ordenada y filtrable;
5. permisos correctos.

DB-01 permanece reversible porque no modifica físicamente los datos.

---

## 12. Matriz de autorización futura

| Acción | Estado tras DB-01 | Aprobación futura requerida |
|---|---|---|
| Diagnóstico agregado SELECT | Permitido en ambiente autorizado | Operación DBA estándar |
| Definir plazo/fecha de corte | Pendiente | Cumplimiento/Legal |
| Diseñar destino histórico | Permitido como diseño | Arquitectura + DBA |
| Crear destino histórico | No autorizado | DDL explícito |
| Copiar lote a histórico | No autorizado | DML explícito + manifiesto |
| Reconciliar lote | Obligatorio si hay copia | DBA + responsable funcional |
| Automatizar copia | No autorizado | Decisión operativa posterior |
| Borrado automático | **Prohibido** | Fuera de DB-01 |
| Purga manual de fuente | No autorizada | Política separada + Cumplimiento/Legal |
| Crear índices | No recomendado actualmente | Nuevo profiling DB-03 si cambia volumen |

---

## 13. Criterios de cierre DB-01

DB-01 queda técnicamente cerrada cuando:

- esta política está versionada;
- existe diagnóstico agregado de solo lectura;
- CI bloquea DDL/DML/purga/scheduler en el paquete DB-01;
- CI verifica que el esquema base y el contrato de auditoría esperado no se hayan desalineado;
- no se haya creado tabla histórica ni índice;
- no se haya movido ni eliminado un solo registro;
- `main` permanezca intacta y el PR de desarrollo continúe en borrador.

El cierre de DB-01 **no equivale a ejecutar un archivo físico**. Define el contrato seguro para una futura implementación cuando exista retención institucional aprobada y autorización separada.
