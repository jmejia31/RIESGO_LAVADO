# Analisis por modulo del Sistema de Gestion de Riesgos LA/FT

Fecha: 2026-06-18  
Base revisada: requerimiento del cliente, analisis funcional en PDF y codigo fuente actual del sistema.  
Sistema actual: API ASP.NET Core, frontend Angular y base Oracle con objetos `RL_*` mas consultas/integraciones a `DNP_IHSS.*`.

## 1. Resumen ejecutivo

El requerimiento del cliente pide cinco modulos de negocio:

1. Monitoreo de Listas y Positivos.
2. Matrices de Riesgos.
3. Indicadores de Riesgo.
4. Debida Diligencia de Inversiones.
5. Cumplimiento Normativo.

Ademas, el sistema necesita modulos transversales: seguridad, usuarios, roles, permisos, auditoria, catalogos, reporteria y continuidad/backup.

Segun el codigo actual, el avance real esta concentrado en:

- Modulo base: usuarios, roles, login, Active Directory, configuracion, menu por modulos y auditoria.
- Modulo de listas: monitoreo de juridicas/naturales/empleados, positivos, seguimientos, evidencias, tipo de listas, carga de listas y coincidencias patrono/empleado.
- Base de datos propia: `RL_USUARIOS`, `RL_ROLES`, `RL_MODULOS`, `RL_USUARIO_MODULOS`, `RL_AUDITORIA`, `RL_CONFIG_SISTEMA`, `RL_LOGIN_SLIDES`, `RL_DETALLE_EVIDENCIA`, `RL_CALIF_COINCIDENCIAS`, mas tablas usadas por el repositorio como `RL_LISTA_POSITIVOS`, `RL_DETALLE_LISTA` y `RL_TIPOS_DOCUMENTO`.

La recomendacion principal es no construir los siguientes modulos como pantallas aisladas. Cada modulo debe nacer con:

- Script SQL incremental.
- Registro en `RL_MODULOS`.
- Permisos en `RL_USUARIO_MODULOS`.
- DTOs, repositorio y controlador propios en backend.
- Servicio Angular y componente standalone.
- Auditoria con `RL_AUDITORIA`.
- Estados, historicos, evidencias y reportes desde el inicio.

## 2. Hallazgos tecnicos importantes

### 2.1 Patron de base de datos que debe respetarse

El proyecto ya usa un patron claro:

- Tablas propias con prefijo `RL_`.
- Secuencias `SEQ_RL_*`.
- Modulos registrados en `RL_MODULOS`.
- Permisos de usuario en `RL_USUARIO_MODULOS`.
- Auditoria general en `RL_AUDITORIA`.
- Integracion con DNP mediante consultas a `DNP_IHSS.*`.

Para informacion que el sistema debe controlar, conviene guardar en tablas `RL_*`. Para informacion maestra que pertenece a DNP, se debe consultar o sincronizar, pero no asumir permisos de escritura.

### 2.2 Riesgo de permisos contra DNP

El script `13_create_calificaciones_coincidencias.sql` indica que no hay `UPDATE` sobre `DNP_IHSS.REPORTE_COINCIDENCIAS` y por eso crea `RL_CALIF_COINCIDENCIAS`. Sin embargo, `ListasRepository.CalificarCoincidenciaAsync` todavia intenta actualizar directamente `DNP_IHSS.REPORTE_COINCIDENCIAS`.

Que hacer:

- Definir una sola fuente de verdad para la calificacion de coincidencias.
- Si no hay permiso de escritura en DNP, usar `RL_CALIF_COINCIDENCIAS` y ajustar las consultas para hacer `LEFT JOIN` contra esa tabla.
- Auditar en `RL_AUDITORIA` la calificacion, pero no duplicar estados contradictorios entre DNP y `RL_*`.

### 2.3 Control de acceso incompleto en rutas recientes

Las rutas Angular de `coincidencias-patrono` y `coincidencias-empleado` estan registradas como modulos en SQL, pero usan `canActivate: [() => true]`.

Que hacer:

- Cambiar a `moduloGuard(<MOD_ID>)`.
- Evitar IDs fijos cuando sea posible o documentarlos claramente.
- Mejorar el redireccionamiento de `home` para rutas nuevas.

### 2.4 Scripts faltantes o no visibles

El backend usa `RL_LISTA_POSITIVOS`, `RL_DETALLE_LISTA`, `RL_TIPOS_DOCUMENTO` y secuencias relacionadas, pero esos objetos no aparecen completos en los scripts visibles de `database`.

Que hacer:

- Confirmar si existen en la base real.
- Si existen, crear script de documentacion o baseline.
- Si no existen, crear script incremental idempotente antes de seguir creciendo el modulo.

## 3. Modulo base: Seguridad, usuarios, roles, permisos y auditoria

### Estado actual

Existe una base funcional:

- Login JWT, refresh token y logout.
- Usuarios locales y usuarios de dominio.
- Roles `ADMINISTRADOR`, `SUPERVISOR`, `ANALISTA`.
- Validacion de Active Directory.
- Recuperacion y cambio de contrasena.
- Menu por `RL_MODULOS` y `RL_USUARIO_MODULOS`.
- Bitacora en `RL_AUDITORIA`.

### Brechas

- Los permisos son por modulo, no por accion.
- Hay rutas que saltan `moduloGuard`.
- Auditoria existe, pero debe usarse de forma obligatoria en todos los modulos nuevos.
- No hay modelo explicito de areas/responsables.
- La configuracion sensible debe mantenerse fuera de archivos con secretos reales.

### Que hacer

- Cerrar este modulo antes de avanzar fuerte con Matrices.
- Estandarizar permisos para todos los modulos.
- Agregar, si Cumplimiento lo requiere, permisos por accion: ver, crear, editar, eliminar, aprobar, imprimir, exportar.

### Como hacerlo

Base de datos sugerida:

- Mantener `RL_MODULOS`.
- Mantener `RL_USUARIO_MODULOS`.
- Agregar opcionalmente:
  - `RL_ACCIONES`: catalogo de acciones.
  - `RL_MODULO_ACCIONES`: acciones disponibles por modulo.
  - `RL_USUARIO_MODULO_ACCIONES`: permisos finos por usuario.
  - `RL_AREAS`: areas institucionales responsables.

Backend:

- Extender `CatalogosController` para exponer modulos, acciones y permisos.
- Mantener auditoria con `IAuditoriaRepository`.
- No mezclar logica nueva en `AuthController` si pertenece a permisos o catalogos.

Frontend:

- Usar `moduloGuard` en toda ruta protegida.
- Crear directiva o helper para ocultar botones por accion si se implementan permisos finos.

## 4. Administracion y catalogos

### Estado actual

Ya existen:

- Configuracion del sistema.
- Slides de login.
- Tipos de listas de cautela.
- Carga de listas.
- Catalogos basicos de roles, dominios y modulos.

### Brechas

El requerimiento pide que el sistema pueda crecer: nuevas listas, indicadores, matrices, normativas, factores y estados. Actualmente no hay un modulo generalizado de catalogos LA/FT.

### Que hacer

Crear un modulo de catalogos LA/FT separado de la configuracion visual.

### Como hacerlo

Base de datos sugerida:

- `RL_CAT_TIPOS_PERSONA`: natural, juridica, patrono, empleado, proveedor, vinculado.
- `RL_CAT_ESTADOS_EXPEDIENTE`: activo, pasivo, en analisis, cerrado, falso positivo, pendiente, vencido.
- `RL_CAT_NIVELES_RIESGO`: bajo, medio, alto, critico o los niveles definidos por Cumplimiento.
- `RL_CAT_PERIODICIDADES`: mensual, trimestral, semestral, anual, por evento.
- `RL_CAT_TIPOS_EVIDENCIA`: documento, acta, informe, captura, constancia.
- `RL_CAT_FUENTES_DATOS`: DNP, archivo, manual, sistema financiero, compras, RRHH.

Backend:

- Crear `CatalogosLaftController`.
- Crear `CatalogosLaftRepository`.
- Registrar en `Program.cs`.

Frontend:

- Crear ruta `/catalogos-laft`.
- Registrar modulo en `RL_MODULOS`.
- Usar pantallas simples de mantenimiento con auditoria.

## 5. Modulo 1: Monitoreo de Listas y Positivos

### Estado actual

Es el modulo mas avanzado. El sistema ya contempla:

- Juridicas, naturales y empleados.
- Consulta de coincidencias desde `DNP_IHSS.REPORTE_COINCIDENCIAS` y vistas de DNP.
- Registro manual o complementario de positivos en `RL_LISTA_POSITIVOS`.
- Seguimientos historicos en `RL_DETALLE_LISTA`.
- Evidencias en `RL_DETALLE_EVIDENCIA`.
- Descarga, eliminacion y auditoria de evidencias.
- Tipos de listas en `DNP_IHSS.TIPO_LISTAS_CAUTELA`.
- Carga de listas a `DNP_IHSS.LISTA_CAUTELA`.
- Reporte impreso auditado.
- Coincidencias patrono y empleado.

### Brechas

- Formalizar como llega el positivo desde DNP: consulta directa, API, job, vista, archivo o carga manual temporal.
- Corregir la fuente de calificacion entre DNP y `RL_CALIF_COINCIDENCIAS`.
- Agregar estados formales del expediente: activo, pasivo, en analisis, cerrado, falso positivo.
- Implementar seguimiento de noticias con estado activo/pasivo, solicitado en observaciones del cliente.
- Validar evidencias tambien en backend: extension, MIME, tamano maximo y ruta segura.
- Proteger rutas de coincidencias con `moduloGuard`.
- Completar reporteria historica por fechas, lista, estado, usuario y tipo de persona.

### Que hacer

Cerrar funcionalmente este modulo antes de iniciar Matrices, porque Matrices e Indicadores dependeran de sus resultados.

### Como hacerlo

Base de datos:

- Confirmar/normalizar `RL_LISTA_POSITIVOS`.
- Confirmar/normalizar `RL_DETALLE_LISTA`.
- Usar `RL_DETALLE_EVIDENCIA`.
- Usar `RL_CALIF_COINCIDENCIAS` si no se puede actualizar DNP.
- Agregar:
  - `RL_ESTADOS_POSITIVO` o catalogo comun de estados.
  - `RL_NOTICIAS_SEGUIMIENTO` para seguimiento de noticias.
  - `RL_POSITIVO_ESTADO_HIST` para historico de cambios de estado, si no se quiere mezclar todo en `RL_DETALLE_LISTA`.

Backend:

- Mantener `ListasController`, pero considerar dividirlo despues:
  - `MonitoreoListasController`.
  - `TipoListasController`.
  - `CargaListasController`.
  - `CoincidenciasController`.
- Ajustar `CalificarCoincidenciaAsync` para usar la tabla correcta.
- Crear validacion centralizada de archivos.

Frontend:

- Cerrar filtros y reportes.
- Agregar pantalla o pestana de seguimiento de noticias.
- Cambiar guards de coincidencias.
- Mantener exportacion PDF/Excel, pero evitar que toda la logica siga creciendo en un solo componente.

## 6. Modulo 2: Matrices de Riesgos

### Estado actual

No se observa implementacion funcional del modulo de Matrices. Es el siguiente modulo natural, pero es el mas delicado porque mezcla:

- Matriz institucional de riesgos.
- Perfilamiento individual de patronos, proveedores y empleados.
- Controles/mitigadores.
- Planes de accion.
- Mapas de calor.
- Reporteria estadistica.

### Que pide el cliente

- Identificacion de factores de riesgo.
- Perfilamiento/scoring por patronos, proveedores y empleados.
- Ponderaciones por factor, por ejemplo: proveedores 50%, clientes 25%, empleados 25%.
- Variables definidas por Cumplimiento.
- Calificacion por factor y consolidada institucional.
- Envio o integracion de calificacion de patronos hacia DNP.
- Resumen por factor e institucional.
- Mapa de calor por riesgo inherente y residual.
- Comparativos por fechas, por ejemplo cierre anual vs actual.
- Solidez de controles.
- Graficos por nivel de riesgo.

### Que hacer

Separar el modulo en submodulos. No copiar el Excel como una pantalla gigante.

Submodulos recomendados:

1. Configuracion metodologica.
2. Factores y variables.
3. Escalas, ponderaciones y colores.
4. Perfilamiento de sujetos: patronos, proveedores, empleados.
5. Matriz institucional de riesgos.
6. Controles y mitigadores.
7. Planes de accion.
8. Mapa de calor y reporteria.
9. Historicos/versiones.

### Como hacerlo

Base de datos sugerida:

- `RL_MATRIZ_VERSION`
  - `MTV_ID`
  - `MTV_NOMBRE`
  - `MTV_FECHA_INICIO`
  - `MTV_FECHA_FIN`
  - `MTV_ESTADO`
  - `MTV_USR_CREACION_ID`
  - `MTV_FECHA_CREACION`

- `RL_FACTORES_RIESGO`
  - `FCR_ID`
  - `FCR_NOMBRE`
  - `FCR_DESCRIPCION`
  - `FCR_PONDERACION`
  - `FCR_ACTIVO`

- `RL_VARIABLES_RIESGO`
  - `VAR_ID`
  - `VAR_FCR_ID`
  - `VAR_NOMBRE`
  - `VAR_DESCRIPCION`
  - `VAR_PONDERACION`
  - `VAR_TIPO_DATO`
  - `VAR_FUENTE_DATO`
  - `VAR_ACTIVA`

- `RL_ESCALAS_RIESGO`
  - `ESC_ID`
  - `ESC_NOMBRE`
  - `ESC_VALOR_MIN`
  - `ESC_VALOR_MAX`
  - `ESC_NIVEL`
  - `ESC_COLOR`
  - `ESC_PUNTAJE`

- `RL_EVALUACIONES_RIESGO`
  - `EVR_ID`
  - `EVR_VERSION_ID`
  - `EVR_TIPO_SUJETO`
  - `EVR_SUJETO_ID`
  - `EVR_DOCUMENTO`
  - `EVR_NOMBRE`
  - `EVR_RIESGO_INHERENTE`
  - `EVR_RIESGO_RESIDUAL`
  - `EVR_NIVEL_RIESGO`
  - `EVR_FECHA_CALCULO`
  - `EVR_ESTADO`

- `RL_EVALUACION_DETALLE`
  - `EVD_ID`
  - `EVD_EVR_ID`
  - `EVD_FACTOR_ID`
  - `EVD_VARIABLE_ID`
  - `EVD_VALOR_OBSERVADO`
  - `EVD_PUNTAJE`
  - `EVD_PONDERACION`
  - `EVD_RESULTADO`

- `RL_RIESGOS_INSTITUCIONALES`
  - `RIN_ID`
  - `RIN_CODIGO`
  - `RIN_DESCRIPCION`
  - `RIN_CAUSA`
  - `RIN_CONSECUENCIA`
  - `RIN_PROBABILIDAD`
  - `RIN_IMPACTO`
  - `RIN_RIESGO_INHERENTE`
  - `RIN_RIESGO_RESIDUAL`
  - `RIN_RESPONSABLE`
  - `RIN_ESTADO`

- `RL_CONTROLES_RIESGO`
  - `CTR_ID`
  - `CTR_RIN_ID`
  - `CTR_DESCRIPCION`
  - `CTR_TIPO`
  - `CTR_DISENO`
  - `CTR_EJECUCION`
  - `CTR_SOLIDEZ`
  - `CTR_EVIDENCIA`

- `RL_PLANES_ACCION`
  - `PLA_ID`
  - `PLA_RIN_ID`
  - `PLA_ACCION`
  - `PLA_RESPONSABLE`
  - `PLA_FECHA_INICIO`
  - `PLA_FECHA_VENCIMIENTO`
  - `PLA_AVANCE`
  - `PLA_ESTADO`

Backend:

- Crear `MatricesRiesgosController`.
- Crear `MatricesRiesgosRepository`.
- Crear `MatricesRiesgosDto.cs`.
- Crear endpoints:
  - `GET /api/matrices/factores`
  - `POST /api/matrices/factores`
  - `GET /api/matrices/variables`
  - `POST /api/matrices/evaluaciones/calcular`
  - `GET /api/matrices/evaluaciones`
  - `GET /api/matrices/mapa-calor`
  - `GET /api/matrices/reportes/perfil-institucional`

Frontend:

- Crear `/matrices-riesgos`.
- Usar tabs:
  - Metodologia.
  - Factores y variables.
  - Evaluaciones.
  - Matriz institucional.
  - Controles.
  - Planes.
  - Mapa de calor.
  - Reportes.

Orden recomendado:

1. Definir formula oficial con Cumplimiento.
2. Cargar catalogos de factores, variables, escalas y ponderaciones.
3. Crear version activa de metodologia.
4. Implementar calculo de una evaluacion individual.
5. Guardar resultado historico.
6. Construir mapa de calor.
7. Integrar resultado hacia DNP solo si existe permiso o mecanismo formal.

## 7. Modulo 3: Indicadores de Riesgo

### Estado actual

No se observa implementacion del modulo de Indicadores.

### Que pide el cliente

Indicadores iniciales:

- Nivel de actualizacion de expedientes de patronos, empleados y proveedores.
- Monto de transacciones con proveedores de alto riesgo.
- Reportes de Operaciones Sospechosas.
- Cumplimiento de normativa.

### Que hacer

Construir indicadores sobre fichas tecnicas. Primero se define cada indicador, luego se calcula y se muestra en tablero.

### Como hacerlo

Base de datos sugerida:

- `RL_INDICADORES`
  - `IND_ID`
  - `IND_NOMBRE`
  - `IND_OBJETIVO`
  - `IND_FORMULA`
  - `IND_NUMERADOR_DESC`
  - `IND_DENOMINADOR_DESC`
  - `IND_FUENTE_DATOS`
  - `IND_PERIODICIDAD`
  - `IND_META`
  - `IND_UMBRAL_AMARILLO`
  - `IND_UMBRAL_ROJO`
  - `IND_RESPONSABLE`
  - `IND_ACTIVO`

- `RL_INDICADOR_MEDICIONES`
  - `MED_ID`
  - `MED_IND_ID`
  - `MED_PERIODO`
  - `MED_FECHA_CORTE`
  - `MED_NUMERADOR`
  - `MED_DENOMINADOR`
  - `MED_RESULTADO`
  - `MED_NIVEL`
  - `MED_OBSERVACION`
  - `MED_USR_CREACION_ID`

- `RL_INDICADOR_EVIDENCIAS`
  - Similar a `RL_DETALLE_EVIDENCIA`, pero asociado a mediciones.

Backend:

- Crear `IndicadoresController`.
- Exponer CRUD de fichas tecnicas.
- Exponer registro/calculo de mediciones.
- Exponer tablero por periodo.

Frontend:

- Ruta `/indicadores-riesgo`.
- Pantallas:
  - Fichas tecnicas.
  - Mediciones.
  - Tablero/semaforos.
  - Historico y exportacion.

Dependencias:

- Indicador de cumplimiento normativo depende del modulo de Cumplimiento.
- Indicador de proveedores de alto riesgo depende de Matrices y fuente financiera/compras.
- Indicador de expedientes depende de DNP o expedientes institucionales.

## 8. Modulo 4: Debida Diligencia de Inversiones

### Estado actual

No se observa implementacion independiente. Debe reutilizar logica del modulo de Monitoreo, pero como expediente preventivo.

### Que pide el cliente

- Identificar empresa o persona natural.
- Registrar fechas y gestiones.
- Registrar listas revisadas.
- Adjuntar evidencias.
- Reportes por empresa, fecha, resumen e individual.
- Impresion similar a un estado de informacion.
- Actualizaciones con historico.

### Que hacer

Crear expediente de debida diligencia independiente de positivos. Puede consultar listas, pero no debe depender de que DNP haya generado una coincidencia.

### Como hacerlo

Base de datos sugerida:

- `RL_DD_EXPEDIENTES`
  - `DDE_ID`
  - `DDE_TIPO_PERSONA`
  - `DDE_DOCUMENTO`
  - `DDE_NOMBRE`
  - `DDE_TIPO_RELACION`
  - `DDE_FECHA_SOLICITUD`
  - `DDE_FECHA_REVISION`
  - `DDE_ESTADO`
  - `DDE_RESOLUCION`
  - `DDE_USR_RESPONSABLE_ID`

- `RL_DD_LISTAS_REVISADAS`
  - `DDL_ID`
  - `DDL_DDE_ID`
  - `DDL_TIPO_LISTA_CAUTELA_ID`
  - `DDL_RESULTADO`
  - `DDL_OBSERVACION`

- `RL_DD_SEGUIMIENTOS`
  - `DDS_ID`
  - `DDS_DDE_ID`
  - `DDS_COMENTARIO`
  - `DDS_ESTADO_ANTERIOR`
  - `DDS_ESTADO_NUEVO`
  - `DDS_USR_CREACION_ID`
  - `DDS_FECHA_CREACION`

- `RL_DD_EVIDENCIAS`
  - igual estructura base de evidencias, asociada al seguimiento o expediente.

Backend:

- Crear `DebidaDiligenciaController`.
- Crear endpoints de expediente, listas revisadas, seguimiento, evidencias, impresion.
- Reutilizar validacion de archivos.

Frontend:

- Ruta `/debida-diligencia-inversiones`.
- Vista con filtros por empresa/persona, fecha, estado y resolucion.
- Formulario de expediente.
- Panel de listas revisadas.
- Historial y evidencias.
- Generacion de PDF.

## 9. Modulo 5: Cumplimiento Normativo

### Estado actual

No se observa implementacion del modulo.

### Que pide el cliente

- Registrar normativas aplicables LA/FT.
- Registrar articulos, exigencias, aplicabilidad al IHSS.
- Estado: cumplido, no cumplido, en proceso, no aplica.
- Procesos documentados.
- Acciones a desarrollar.
- Referencias/evidencias.
- Observaciones.
- Seguimientos en diferentes fechas.
- Porcentaje de cumplimiento.
- Varias normativas administrables.

### Que hacer

Crear una matriz normativa con articulos, evaluaciones, evidencias y planes de accion.

### Como hacerlo

Base de datos sugerida:

- `RL_NORMATIVAS`
  - `NOR_ID`
  - `NOR_NOMBRE`
  - `NOR_TIPO`
  - `NOR_ENTIDAD_EMISORA`
  - `NOR_FECHA_EMISION`
  - `NOR_FECHA_VIGENCIA`
  - `NOR_ESTADO`
  - `NOR_DOCUMENTO_URL`

- `RL_NORMATIVA_ARTICULOS`
  - `NAR_ID`
  - `NAR_NOR_ID`
  - `NAR_NUMERO`
  - `NAR_TEXTO`
  - `NAR_EXIGENCIA`
  - `NAR_TEMA`
  - `NAR_APLICA_IHSS`
  - `NAR_AREA_RESPONSABLE`

- `RL_CUMPLIMIENTO_EVALUACIONES`
  - `CUE_ID`
  - `CUE_NAR_ID`
  - `CUE_ESTADO_CUMPLIMIENTO`
  - `CUE_PORCENTAJE`
  - `CUE_PROCESO_DOCUMENTADO`
  - `CUE_REFERENCIA`
  - `CUE_OBSERVACION`
  - `CUE_FECHA_EVALUACION`
  - `CUE_USR_ID`

- `RL_CUMPLIMIENTO_ACCIONES`
  - `CUA_ID`
  - `CUA_CUE_ID`
  - `CUA_ACCION`
  - `CUA_RESPONSABLE`
  - `CUA_FECHA_COMPROMISO`
  - `CUA_AVANCE`
  - `CUA_ESTADO`

- `RL_CUMPLIMIENTO_SEGUIMIENTOS`
  - historico por fecha, usuario, comentario y cambio de estado.

- `RL_CUMPLIMIENTO_EVIDENCIAS`
  - documentos asociados a articulos, evaluaciones o acciones.

Backend:

- Crear `CumplimientoNormativoController`.
- CRUD de normativas y articulos.
- Evaluaciones por articulo.
- Planes de accion.
- Evidencias.
- Dashboard de cumplimiento por normativa, estado y area.

Frontend:

- Ruta `/cumplimiento-normativo`.
- Tabs:
  - Normativas.
  - Articulos/exigencias.
  - Evaluacion.
  - Acciones.
  - Evidencias.
  - Reportes.

Integracion:

- Este modulo alimenta el indicador de cumplimiento normativo.

## 10. Reporteria integral

### Estado actual

Hay exportaciones/reportes en el modulo de listas, pero no una capa de reporteria integral.

### Que hacer

Crear reportes por modulo y un tablero gerencial consolidado.

### Como hacerlo

Reportes minimos:

- Monitoreo:
  - Positivos por fecha, lista, estado, tipo de persona.
  - Expediente individual imprimible.
  - Evidencias y seguimientos por caso.

- Matrices:
  - Mapa de calor actual.
  - Mapa de calor comparativo.
  - Perfil institucional por nivel.
  - Perfil por factor.
  - Solidez de controles.
  - Clientes, empleados y proveedores por nivel.

- Indicadores:
  - Semaforo por periodo.
  - Tendencia historica.
  - Indicadores vencidos o sin medicion.

- Debida diligencia:
  - Expedientes por empresa/persona.
  - Expedientes por fecha y resolucion.
  - Informe individual.

- Cumplimiento:
  - Cumplimiento por normativa.
  - Articulos pendientes.
  - Acciones vencidas.
  - Evidencias faltantes.

Base de datos:

- Usar vistas `VW_RL_*` para reportes pesados.
- Evitar que la UI haga calculos criticos.
- Materializar resultados solo si el rendimiento lo requiere.

## 11. Backup, recuperacion y continuidad

### Estado actual

El PDF lo recomienda como modulo tecnico. No se observa implementacion funcional en el sistema.

### Que hacer

No necesariamente debe ser una pantalla de usuario final, pero si debe existir trazabilidad tecnica de respaldos.

### Como hacerlo

Base de datos sugerida:

- `RL_BACKUP_LOG`
  - `BKL_ID`
  - `BKL_TIPO`
  - `BKL_FECHA_INICIO`
  - `BKL_FECHA_FIN`
  - `BKL_ESTADO`
  - `BKL_RUTA`
  - `BKL_TAMANIO`
  - `BKL_MENSAJE`
  - `BKL_USR_ID`

- `RL_RESTORE_TEST_LOG`
  - pruebas de restauracion, resultado y evidencia.

Operacion:

- Programar respaldo Oracle y respaldo de carpeta de evidencias.
- Registrar resultado.
- Alertar fallos.
- Probar restauracion periodicamente.

## 12. Roadmap recomendado

### Fase A: Cierre del modulo base

Resultado esperado: permisos, auditoria y catalogos minimos estables.

Acciones:

- Corregir guards de rutas.
- Confirmar scripts faltantes.
- Revisar secretos/configuracion.
- Definir si se agregan permisos por accion.

### Fase B: Cierre de Monitoreo de Listas

Resultado esperado: modulo de positivos cerrado funcionalmente.

Acciones:

- Definir integracion oficial con DNP.
- Corregir calificacion DNP vs `RL_CALIF_COINCIDENCIAS`.
- Completar estados y seguimiento de noticias.
- Endurecer validacion backend de evidencias.
- Completar reportes por filtros e historicos.

### Fase C: Diseno profundo de Matrices

Resultado esperado: documento funcional y modelo de datos aprobado antes de construir UI final.

Acciones:

- Aprobar metodologia, variables, pesos, escalas y formulas.
- Separar matriz institucional de scoring individual.
- Crear scripts SQL y endpoints iniciales.

### Fase D: Indicadores

Resultado esperado: indicadores medibles y trazables.

Acciones:

- Crear fichas tecnicas.
- Definir fuentes por indicador.
- Crear mediciones historicas y tablero.

### Fase E: Debida Diligencia

Resultado esperado: expediente preventivo independiente.

Acciones:

- Reutilizar patron de seguimiento/evidencia.
- Crear reportes e impresion individual.

### Fase F: Cumplimiento Normativo

Resultado esperado: matriz normativa con acciones, evidencias y porcentajes.

Acciones:

- Crear normativas, articulos, evaluaciones y acciones.
- Conectar con indicador de cumplimiento.

### Fase G: Reporteria integral

Resultado esperado: vision gerencial y operativa consolidada.

Acciones:

- Crear vistas o endpoints de reporteria.
- Consolidar mapa de calor, perfiles, indicadores y cumplimiento.

## 13. Prioridades inmediatas

1. Corregir la inconsistencia de calificacion de coincidencias entre DNP y `RL_CALIF_COINCIDENCIAS`.
2. Proteger `coincidencias-patrono` y `coincidencias-empleado` con `moduloGuard`.
3. Confirmar scripts faltantes de `RL_LISTA_POSITIVOS`, `RL_DETALLE_LISTA` y `RL_TIPOS_DOCUMENTO`.
4. Cerrar funcionalmente Monitoreo de Listas.
5. Definir formalmente la metodologia de Matrices antes de programar.
6. Crear catalogos LA/FT reutilizables.
7. Construir Matrices como modulo independiente con versionamiento e historico.

