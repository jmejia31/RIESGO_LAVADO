# Fase 9 — Expediente de preparación y autorización Oracle

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha de preparación:** 2026-08-06.
- **Repositorio:** `jmejia31/RIESGO_LAVADO`.
- **Rama técnica:** `desarrollo`.
- **Rama estable:** `main` — no modificar ni fusionar sin autorización expresa.
- **PR de revisión:** #20 — debe permanecer abierto y en borrador.
- **Modelo objetivo:** 17 tablas y 17 secuencias `RL_MR_*`.
- **Estado de la Fase 9:** expediente técnico preparado; diligenciamiento externo pendiente.
- **Autorización para la Fase 10:** **NO OTORGADA**.
- **Oracle ejecutado durante esta fase:** **NO**.
- **Script `05` ejecutado:** **NO**.
- **Script `06` ejecutado:** **NO**.

---

## 1. Propósito

Este expediente establece los requisitos técnicos, operativos, de seguridad y de evidencia que deben cumplirse antes de solicitar autorización para ejecutar la transición física del Módulo Matrices de Riesgos en una base Oracle exclusiva de pruebas.

La Fase 9 no instala objetos, no elimina objetos, no migra datos y no certifica el modelo. Su resultado es un paquete preparado para revisión del DBA, del responsable funcional y de Javier Mejía.

La ejecución destructiva pertenece exclusivamente a la Fase 10 y requiere una autorización expresa, separada y verificable.

---

## 2. Alcance autorizado de esta fase

### Incluido

- Preparación del inventario Oracle de solo lectura.
- Definición de la identificación mínima del ambiente.
- Definición de responsables y participantes.
- Requisitos de aislamiento y ausencia de información productiva.
- Requisitos de respaldo y prueba de restauración.
- Permisos mínimos para preflight, transición y certificación.
- Método seguro de suministro de la cadena de conexión.
- Plan previsto de ejecución de la Fase 10.
- Plan de contingencia ante fallos parciales de DDL.
- Checklist de evidencias y criterios de entrada/salida.
- Formato separado de autorización.

### Excluido

- Conexión a Oracle.
- Ejecución del preflight contra una instancia real.
- Ejecución del script `05`.
- Ejecución del script `06`.
- Ejecución de `CREATE`, `ALTER`, `DROP`, `TRUNCATE` o migraciones.
- Configuración de secretos en GitHub, código fuente o documentación.
- Modificación o fusión de `main`.
- Despliegue en Producción.

---

## 3. Identificación obligatoria del ambiente Oracle

Los siguientes campos deben ser diligenciados por el DBA y verificados antes de autorizar la Fase 10.

| Campo | Valor requerido | Estado |
|---|---|---|
| Nombre lógico del ambiente | `PENDIENTE_DBA` | Pendiente |
| Finalidad | Pruebas exclusivas del modelo de Matrices | Pendiente de confirmación |
| Instancia o servicio Oracle | `PENDIENTE_DBA` | Pendiente |
| Host o infraestructura | `PENDIENTE_DBA` | Pendiente |
| Versión exacta de Oracle | `PENDIENTE_DBA` | Pendiente |
| Esquema | Debe ser `RIESGO_LAVADO` | Pendiente de evidencia |
| Clasificación del ambiente | Desarrollo, QA o certificación; nunca Producción | Pendiente |
| Propietario técnico | `PENDIENTE_DBA` | Pendiente |
| Responsable funcional | `PENDIENTE_RESPONSABLE` | Pendiente |
| Fecha de identificación | `AAAA-MM-DD` | Pendiente |
| Zona horaria | `PENDIENTE_DBA` | Pendiente |
| Ventana propuesta | `AAAA-MM-DD HH:MM–HH:MM` | Pendiente |

### Evidencia mínima de identidad

La evidencia deberá incluir, sin contraseñas ni cadenas completas:

```sql
SELECT SYS_CONTEXT('USERENV', 'DB_NAME') FROM DUAL;
SELECT SYS_CONTEXT('USERENV', 'SERVER_HOST') FROM DUAL;
SELECT SYS_CONTEXT('USERENV', 'SESSION_USER') FROM DUAL;
SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM DUAL;
SELECT BANNER FROM V$VERSION;
```

La consulta a `V$VERSION` depende de los permisos del usuario. Su ausencia debe documentarse y suplirse con evidencia emitida por el DBA.

---

## 4. Confirmación de aislamiento y ausencia de datos productivos

El DBA y el responsable funcional deberán declarar por escrito:

- que la instancia o esquema no corresponde a Producción;
- que no existe tráfico de usuarios finales;
- que no existen integraciones productivas conectadas;
- que no se utilizarán credenciales productivas;
- que los objetos y datos `RL_MR_*` existentes son de prueba o cuentan con disposición autorizada;
- que la reconstrucción no afectará otros módulos institucionales;
- que `RL_USUARIOS`, `RL_AUDITORIA` y `SEQ_RL_AUDITORIA` pertenecen al esquema de pruebas y pueden ser utilizados por la certificación;
- que existe capacidad para restaurar el estado previo.

### Estado actual

**Confirmación escrita recibida:** NO.

**Bloqueante:** mientras esta declaración no esté firmada, la Fase 10 no puede iniciar.

---

## 5. Responsables y segregación de funciones

| Rol | Responsabilidad | Persona | Confirmado |
|---|---|---|---|
| Propietario de la autorización | Autorizar o rechazar la Fase 10 | Javier Mejía | Pendiente |
| DBA ejecutor | Conectar, respaldar, ejecutar y conservar evidencia | `PENDIENTE_DBA` | Pendiente |
| DBA observador o revisor | Verificar comandos, esquema y resultados | `PENDIENTE_DBA_REVISOR` | Pendiente |
| Responsable funcional | Confirmar disposición de datos y resultado esperado | `PENDIENTE_RESPONSABLE` | Pendiente |
| Responsable técnico | Verificar commit, hash, scripts y pruebas | `PENDIENTE_TECNICO` | Pendiente |
| Custodio de evidencias | Conservar logs y acta sin secretos | `PENDIENTE_CUSTODIO` | Pendiente |

La misma persona no debe aprobar, ejecutar y certificar en solitario cuando la operación implique retiro físico de objetos.

---

## 6. Línea base técnica obligatoria

Antes de la ventana deben registrarse:

| Elemento | Valor esperado |
|---|---|
| Rama | `desarrollo` |
| Commit base de Fase 9 | Debe registrarse después del cierre técnico de esta fase |
| PR | #20, abierto y en borrador |
| `main` | Sin cambios ni fusión |
| Script destructivo | `database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql` |
| Preflight de solo lectura | `database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql` |
| Modelo esperado | 17 tablas y 17 secuencias |
| Quality Gate | Debe finalizar en `success` sobre el commit autorizado |
| Hash SHA-256 del script `06` | `PENDIENTE_CALCULAR_EN_VENTANA` |
| Hash SHA-256 del script `07` | `PENDIENTE_CALCULAR_EN_VENTANA` |

No debe ejecutarse una copia local diferente del archivo versionado y aprobado.

---

## 7. Respaldo y restauración

### 7.1 Respaldo previo obligatorio

El DBA deberá documentar:

- tipo de respaldo;
- alcance exacto: esquema completo o base completa;
- herramienta y versión;
- fecha y hora de inicio y finalización;
- ubicación lógica del respaldo, sin exponer rutas sensibles;
- tamaño;
- hash o mecanismo de integridad disponible;
- responsable;
- política de retención;
- evidencia de finalización exitosa.

| Campo | Valor |
|---|---|
| Identificador del respaldo | `PENDIENTE_DBA` |
| Tipo | `PENDIENTE_DBA` |
| Alcance | `PENDIENTE_DBA` |
| Inicio | `PENDIENTE_DBA` |
| Finalización | `PENDIENTE_DBA` |
| Resultado | Pendiente |
| Responsable | `PENDIENTE_DBA` |

### 7.2 Prueba de restauración

No basta con crear el respaldo. Debe existir evidencia de que puede restaurarse en un destino controlado o mediante un procedimiento previamente probado.

La evidencia debe indicar:

- identificador de la prueba;
- destino de restauración;
- tiempo de recuperación;
- validaciones posteriores;
- resultado;
- responsable;
- fecha.

**Prueba de restauración validada:** NO.

**Bloqueante:** no se autoriza la Fase 10 sin respaldo y restauración verificables.

---

## 8. Inventario físico previo de solo lectura

El archivo preparado para esta actividad es:

```text
database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql
```

El preflight:

- valida `CURRENT_SCHEMA = RIESGO_LAVADO`;
- exige `RL_USUARIOS`;
- exige `RL_AUDITORIA`;
- exige `SEQ_RL_AUDITORIA`;
- identifica base, host, usuario de sesión y fecha del servidor;
- lista tablas `RL_MR_*`;
- lista secuencias `SEQ_RL_MR_*`;
- cuenta registros reales por tabla `RL_MR_*`;
- reporta objetos inválidos;
- reporta restricciones deshabilitadas;
- no contiene DDL ni DML;
- no ejecuta el script `06`.

### Ejecución prevista

Esta instrucción se conserva únicamente como parte del plan; no fue ejecutada en la Fase 9:

```text
sqlplus /nolog
CONNECT mediante mecanismo seguro aprobado por el DBA
@database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql
```

### Tratamiento del resultado

- Si existen datos `RL_MR_*`, debe emitirse una decisión escrita sobre su conservación, exportación o eliminación.
- Si el esquema no es `RIESGO_LAVADO`, se detiene el proceso.
- Si faltan objetos institucionales, se detiene el proceso.
- Si existen objetos inválidos o restricciones deshabilitadas, deben analizarse antes de la transición.
- La salida debe almacenarse sin credenciales y asociarse al commit autorizado.

---

## 9. Permisos mínimos

### Para el preflight de Fase 9

Solo se requieren permisos de conexión y lectura sobre vistas del diccionario del propio esquema, tablas institucionales y objetos `RL_MR_*`.

### Para la Fase 10

El usuario ejecutor requerirá privilegios suficientes para retirar y crear tablas, secuencias, índices y restricciones dentro del esquema autorizado. Los privilegios deben limitarse al ambiente exclusivo y a la ventana aprobada.

No se autoriza:

- uso de cuentas SYS o SYSTEM salvo justificación formal del DBA;
- privilegios sobre Producción;
- publicación de contraseñas;
- almacenamiento de secretos en Git;
- reutilización de credenciales personales en evidencias.

---

## 10. Método seguro para la conexión Oracle

La conexión de la suite se suministrará únicamente mediante:

```text
ConnectionStrings__OracleDB
```

como variable de entorno o User Secrets.

La habilitación de las pruebas requiere además:

```text
RL_ORACLE_INTEGRATION_REQUIRED=true
```

Controles:

- no colocar la cadena en `appsettings.json` versionado;
- no colocarla en archivos `.md`, `.sql`, `.ps1`, logs o capturas;
- no enviar contraseñas por el PR;
- enmascarar identificadores sensibles en evidencias públicas;
- eliminar variables temporales al terminar la ventana;
- rotar la credencial si se expone accidentalmente.

---

## 11. Plan previsto para la Fase 10

La siguiente secuencia es un plan, no una autorización:

1. Confirmar participantes y ventana.
2. Confirmar commit exacto de `desarrollo`.
3. Confirmar Quality Gate en `success`.
4. Calcular y registrar hashes de los scripts `06` y `07`.
5. Confirmar respaldo completo.
6. Confirmar prueba de restauración.
7. Conectarse mediante mecanismo seguro.
8. Confirmar `CURRENT_SCHEMA = RIESGO_LAVADO`.
9. Ejecutar el preflight `07` de solo lectura.
10. Revisar el inventario y el conteo de datos.
11. Obtener decisión escrita sobre datos existentes.
12. Obtener autorización expresa de Javier Mejía y conformidad del DBA.
13. Ejecutar el script `06` únicamente con el parámetro literal `EJECUTAR`.
14. Conservar la salida completa de SQL*Plus.
15. Detenerse ante cualquier error.
16. No ejecutar todavía semillas o ajustes adicionales sin el orden autorizado.
17. Pasar a la Fase 11 para certificación física y funcional.

El paso 13 permanece prohibido hasta que el formato separado de autorización esté completo y firmado.

---

## 12. Plan de contingencia

Oracle realiza commits implícitos alrededor de operaciones DDL. Por tanto, un `ROLLBACK` no garantiza revertir objetos ya eliminados o creados antes de un fallo.

Ante cualquier error durante la Fase 10:

1. Detener inmediatamente la ejecución.
2. No improvisar comandos correctivos.
3. Conservar el log completo y el código Oracle del error.
4. Registrar el último objeto procesado.
5. Bloquear el acceso funcional al módulo.
6. Comparar el inventario físico contra el estado previo.
7. Decidir entre completar controladamente o restaurar el respaldo.
8. La restauración debe ser ejecutada por el DBA conforme al procedimiento validado.
9. Repetir el preflight después de restaurar.
10. Emitir un acta de incidente antes de reintentar.

### Criterio de restauración obligatoria

Debe restaurarse el estado previo cuando exista cualquiera de estas condiciones:

- pérdida de objetos institucionales;
- creación parcial no conciliable;
- errores de permisos que dejen el modelo incompleto;
- ausencia de una de las 17 tablas o secuencias tras la ejecución;
- restricciones o índices críticos ausentes;
- imposibilidad de ejecutar la suite de certificación;
- duda sobre la integridad de datos o trazabilidad.

---

## 13. Evidencias obligatorias

La carpeta o repositorio de evidencias deberá contener referencias a:

- solicitud de cambio;
- autorización firmada;
- identificación del ambiente;
- declaración de no Producción;
- respaldo exitoso;
- prueba de restauración;
- commit autorizado;
- Quality Gate aprobado;
- hashes de scripts;
- salida del preflight `07`;
- inventario previo;
- decisión sobre datos existentes;
- participantes y tiempos de la ventana;
- log de la Fase 10;
- inventario posterior;
- resultado de la suite Oracle;
- acta de cierre o incidente.

No se deben almacenar:

- contraseñas;
- cadenas completas de conexión;
- tokens;
- archivos de secretos;
- datos personales innecesarios;
- capturas con credenciales visibles.

---

## 14. Criterios de entrada para la Fase 10

Todos deben cumplirse:

- [ ] Ambiente Oracle exclusivo identificado.
- [ ] Confirmación escrita de no Producción.
- [ ] Responsable DBA designado.
- [ ] Revisor DBA designado.
- [ ] Responsable funcional designado.
- [ ] Ventana aprobada.
- [ ] Respaldo completo exitoso.
- [ ] Restauración validada.
- [ ] Preflight `07` ejecutado y revisado.
- [ ] Datos existentes conciliados o autorizados para retiro.
- [ ] Commit exacto registrado.
- [ ] Quality Gate en `success`.
- [ ] Hashes de scripts registrados.
- [ ] Conexión segura preparada.
- [ ] Plan de contingencia aceptado.
- [ ] Autorización expresa de Javier Mejía.
- [ ] Conformidad del DBA ejecutor.

Mientras una sola casilla permanezca pendiente, la ejecución del script `06` continúa bloqueada.

---

## 15. Criterios de salida de la Fase 9

La Fase 9 se considera técnicamente preparada cuando:

- existe el preflight de solo lectura;
- existe este expediente;
- existe un formato separado de autorización;
- existe una puerta automática que verifica los controles;
- los Quality Gates aprueban;
- se documentan claramente los campos externos pendientes;
- no se ejecutó Oracle;
- no se otorgó implícitamente autorización para la Fase 10.

El diligenciamiento por DBA y la firma de autorización son requisitos externos posteriores, no resultados que puedan inventarse desde el repositorio.

---

## 16. Dictamen de la Fase 9

```text
EXPEDIENTE TECNICO: PREPARADO
PREFLIGHT DE SOLO LECTURA: PREPARADO, NO EJECUTADO
AMBIENTE ORACLE EXCLUSIVO: PENDIENTE DE IDENTIFICACION Y EVIDENCIA
RESPALDO: PENDIENTE
PRUEBA DE RESTAURACION: PENDIENTE
AUTORIZACION FASE 10: NO OTORGADA
SCRIPT 05: NO EJECUTADO
SCRIPT 06: NO EJECUTADO
CERTIFICACION ORACLE: PENDIENTE
```

La Fase 10 continúa bloqueada hasta completar el checklist y recibir autorización expresa y separada.

---

## 17. Pendiente independiente de seguridad

`npm ci` continúa reportando:

```text
13 vulnerabilidades
6 moderadas
6 altas
1 crítica
```

No se aplicó `npm audit fix --force`, porque podría introducir cambios incompatibles. Este pendiente requiere una fase de seguridad separada antes de Producción y deberá recordarse al final de cada fase hasta su resolución formal.
