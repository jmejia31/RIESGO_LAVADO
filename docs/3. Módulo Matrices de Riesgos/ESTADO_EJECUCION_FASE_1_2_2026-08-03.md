# Estado de Ejecución — Fase 1.2
## Alineación del repositorio con el DDL dinámico definitivo

**Fecha:** 3 de agosto de 2026  
**Rama de trabajo:** `desarrollo`  
**Rama estable:** `main` — intacta  
**Estado:** implementación técnica corregida y pendiente de revisión, compilación y certificación Oracle.

---

## 1. Condiciones establecidas por Codex

La Fase 1.2 continúa abierta. Antes de ejecutar Oracle debían resolverse los siguientes bloqueantes:

1. retirar la fachada transitoria y hacer operativas las nueve vinculaciones de evidencias;
2. asignar explícitamente `OracleTransaction` a todos los comandos de los casos de uso transaccionales;
3. vincular cada traza con la regla declarada por la versión publicada del formulario;
4. no publicar la copia local divergente sobre `origin/desarrollo`;
5. impedir la publicación de credenciales Oracle codificadas y rotar la contraseña expuesta localmente;
6. ejecutar posteriormente validador, compilación, pruebas unitarias y pruebas Oracle controladas.

---

## 2. Commits publicados después de la revisión

| Cambio | Commit | Estado |
|---|---|---|
| Repositorio operativo: evidencias, transacciones y regla versionada | `b70635673867b150b3aa6e85ec210a0514241d88` | Implementado; pendiente de pruebas |
| Registro directo del repositorio, sin fachada, en `Program.cs` | `b4a0f9228165c5aef284012d38c35d25e222791e` | Implementado |
| Eliminación física de `MatricesRiesgosRepositoryFacade.cs` | `f0754c77fe4a54c68336cba5c1494dcc240f1f36` | Completado |
| Validador reforzado para DDL, transacciones, evidencias y secretos | `689ad980e4b62521277fb6712955400066448436` | Publicado; pendiente de ejecución local |
| Exclusiones adicionales de configuraciones y secretos locales | `b1b3082b456b8efb03acb19864d099521661dc5f` | Completado |
| Semilla idempotente de regla de cálculo versionada | `14b438f0de42cd2cda70954f9f3c5711e6cd5ed6` | Publicada; no ejecutada |
| Formulario inicial vinculado a la regla `CALCULO_VRI_VRR` versión `1.0` | `7e5b07d36e13dcbc2e897989f935110819213b4c` | Publicado; no ejecutado |

Los commits anteriores de inicio de Fase 1.2 permanecen en el historial remoto. Ningún cambio fue aplicado a `main`.

---

## 3. Evidencias operativas

Las nueve vinculaciones se concentran ahora en `MatricesRiesgosRepository`:

- riesgo;
- evaluación;
- control;
- plan;
- actividad;
- alerta;
- automonitoreo;
- revisión;
- aprobación.

Cada operación:

1. verifica la existencia física de la evidencia;
2. resuelve la evaluación relacionada cuando el modelo lo permite;
3. inserta en la tabla puente `RL_MR_EVI_*` correspondiente;
4. registra `RL_MR_AUDITORIA` dentro de la misma transacción cuando existe una evaluación relacionada;
5. registra la auditoría transversal institucional mediante `IAuditoriaRepository.RegistrarAsync` cuando el servicio está disponible.

No permanece ningún `NotSupportedException` ni fachada registrada en el flujo activo.

### Observación para revisión

La aprobación de formulario no tiene una relación física directa con una evaluación. Su vínculo utiliza auditoría transversal institucional; no se inventa un `AUD_EVALUACION_ID` para satisfacer `RL_MR_AUDITORIA`.

---

## 4. Atomicidad Oracle

Se incorporó un único constructor de comandos que:

- activa `BindByName`;
- recibe opcionalmente `OracleTransaction`;
- asigna explícitamente `command.Transaction = transaction`.

En creación y actualización de evaluación, los siguientes pasos comparten la misma conexión y transacción:

- evaluación;
- proyección;
- flujo inicial o transición;
- revisión histórica;
- traza de cálculo;
- auditoría específica del módulo.

Ante una excepción se ejecuta `RollbackAsync`; solo se confirma cuando todos los pasos obligatorios han finalizado.

---

## 5. Regla vinculada a la versión del formulario

La traza ya no selecciona la última regla activa global.

El proceso actual:

1. toma `EVA_VERSION_ID`;
2. obtiene el `VER_JSON` de esa versión;
3. exige que la versión esté `PUBLISHED`; para nuevas evaluaciones también exige `VER_VIGENTE = 1`;
4. extrae el código y la versión de la regla declarada;
5. resuelve exactamente `REG_CODIGO`, `REG_VERSION` y `REG_ACTIVA = 1`;
6. persiste ese `REG_ID` en `TRA_REGLA_ID`.

Las semillas ahora registran:

```text
Código: CALCULO_VRI_VRR
Versión: 1.0
Algoritmo: MATRICES_VRI_ADITIVO_1_9
Fórmula institucional: VRI = Frecuencia + Impacto - 1
Dominio: 1–9
```

No se fijaron intervalos Bajo/Moderado/Alto/Crítico en C# ni en la semilla. Esos parámetros continúan sujetos a definición funcional y versionamiento en la Fase 1.4.

---

## 6. Seguridad de credenciales

La revisión de Codex detectó una contraseña Oracle dentro de una prueba preparada únicamente en la copia local divergente.

### Acciones obligatorias fuera del repositorio

1. eliminar o deshacer ese archivo local antes de cualquier commit;
2. no copiar su contenido a `origin/desarrollo`;
3. mover la conexión de pruebas a variables de entorno, .NET User Secrets o configuración local ignorada;
4. **rotar inmediatamente la contraseña expuesta mediante el operador o DBA autorizado**;
5. limpiar la credencial de historiales, respaldos y archivos temporales locales donde haya quedado registrada.

La rotación no puede ejecutarse desde GitHub ni desde este repositorio.

### Protecciones publicadas

`.gitignore` excluye configuraciones locales, archivos de secretos, entornos y configuraciones Oracle de prueba. El validador examina backend, frontend, scripts y workflows buscando patrones de cadenas Oracle y contraseñas codificadas sin imprimir el valor detectado.

Una búsqueda estática en el remoto no encontró coincidencias para los patrones comunes `Password=`, `Pwd=` o `User Id=`. Esto no certifica la copia local de Codex ni sustituye la rotación.

---

## 7. Validador integral

`validate_matrices_dynamic_ddl_alignment.ps1` ahora comprueba:

- ausencia del workflow temporal;
- ausencia de la fachada;
- ausencia de `NotSupportedException` en el repositorio;
- eliminación de columnas incompatibles y tablas retiradas;
- uso de `FLU_ESTADO`;
- propagación explícita de `OracleTransaction`;
- resolución de reglas por versión publicada, código y versión;
- presencia de las nueve vinculaciones de evidencias;
- registro directo de `MatricesRiesgosRepository` en DI;
- protecciones del script `05`;
- posibles secretos Oracle codificados.

Los scripts de retiro histórico permanecen fuera del análisis de incompatibilidades porque contienen referencias antiguas deliberadas para eliminación controlada.

---

## 8. Copia local divergente

La copia local preparada por otro trabajo no debe confirmarse ni publicarse sobre el remoto.

Procedimiento seguro para revisión:

```powershell
git fetch origin desarrollo
git worktree add ..\RIESGO_LAVADO_REVISION_CODEX --detach origin/desarrollo
cd ..\RIESGO_LAVADO_REVISION_CODEX
```

La copia original con cambios preparados debe permanecer intacta hasta que su responsable:

- retire la credencial;
- compare sus cambios con `origin/desarrollo`;
- conserve únicamente trabajo legítimo no duplicado;
- descarte las versiones incompatibles del repositorio y las pruebas inseguras.

No se autoriza `git push --force`, `reset --hard`, merge ni rebase sobre el trabajo local sin revisión manual.

---

## 9. Validaciones pendientes

Todavía no se declara aprobada ni cerrada la Fase 1.2.

Pendientes:

1. crear y ejecutar pruebas Oracle reales de creación, actualización, transición, evidencia, proyección, traza, auditoría y rollback;
2. probar en Oracle la vinculación de las nueve tablas puente;
3. probar que una versión sin referencia de regla sea rechazada;
4. probar que una regla inexistente o inactiva sea rechazada;
5. revisar el tratamiento histórico de versiones retiradas o archivadas;
6. solicitar autorización antes de ejecutar el script `05`.

El validador, la compilación Release y las suites automatizadas ya fueron ejecutados correctamente en local y en CI. Estos resultados reducen el pendiente, pero no sustituyen las pruebas transaccionales Oracle.

No se ha ejecutado ningún DDL o DML de estos scripts en Oracle durante esta intervención.

---

## 10. Estado real

```text
Workflow temporal: eliminado.
Fachada transitoria: eliminada.
Nueve vinculaciones de evidencias: implementadas en el repositorio activo.
OracleTransaction: propagada explícitamente en operaciones transaccionales.
Regla de traza: vinculada por código y versión a VER_JSON.
Semilla de regla y referencia del formulario: publicadas, no ejecutadas.
Protección contra secretos: ampliada.
Credencial local expuesta: requiere rotación externa inmediata.
Validador: ejecutado correctamente; falso positivo local de configuración ignorada corregido el 4 de agosto de 2026.
Compilación: correcta en Release, sin errores ni advertencias.
Pruebas backend: 188 correctas en la verificación local más reciente.
Pruebas Oracle reales: no ejecutadas.
Script 05: no ejecutado.
Fase 1.2: abierta y pendiente de revisión de Codex.
main: intacta.
```

---

## 11. Solicitud de revisión a Codex

Revisar los commits señalados y confirmar:

1. operación real de las nueve vinculaciones;
2. propagación de `OracleTransaction` en cada comando transaccional;
3. resolución de `TRA_REGLA_ID` desde la versión exacta del formulario;
4. coherencia de las semillas `03` y `04`;
5. cobertura y posibles falsos positivos del validador;
6. estrategia de auditoría para aprobación de formulario;
7. pruebas necesarias antes de ejecutar Oracle.

La Fase 1.2 no debe cerrarse hasta contar con compilación, pruebas automatizadas, pruebas Oracle reales y nueva aprobación expresa.
