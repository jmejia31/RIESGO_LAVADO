# Matriz de trazabilidad - Fase 10

| Requisito | Implementación principal | Prueba automatizada | Evidencia |
|---|---|---|---|
| Bloquear cierre cuando existe un plan requerido pendiente | `MatricesRiesgosAppService.CambiarEstadoAsync` y repositorio de Matrices | `CambiarEstado_CierreConPlanRequeridoPendiente_RechazaSinCambiar` | `Evidencia_Funcional/fase10_validacion_api_final.json` |
| Crear, editar y cambiar estado de planes | `MatricesRiesgosController`, `MatricesRiesgosAppService` y `MatricesRiesgosRepository` | Casos `CrearPlan_*`, `ActualizarPlan_*`, `CambiarEstadoPlan_*` y contratos HTTP Angular | Evidencia funcional API |
| Inactivar y reactivar planes con motivo | Endpoints `inactivar`/`reactivar` y reglas de aplicación | `InactivarPlan_MotivoValido_RecortaYDelega`, familia `ReactivarPlan_*` y pruebas de componente/servicio Angular | Evidencia funcional API y auditoría |
| Cargar evidencia validando extensión, MIME, firma, tamaño y hash | `MatricesRiesgosAppService.CargarEvidenciaAsync` | `CargarEvidencia_FirmaInvalida_RechazaAntesDelRepositorio`, `CargarEvidencia_ArchivoValido_RegistraHashYRetornaMetadata` | Evidencia funcional API y SQL DBA |
| Descargar sin exponer ni aceptar rutas fuera del almacenamiento | `DescargarEvidenciaAsync` y contrato público de evidencia | `DescargarEvidencia_RutaFueraDelAlmacenamiento_RechazaSinAuditar`, `EvidenciaPublica_NoSerializaRutaFisica` | Evidencia funcional API |
| Vista previa segura y limitada a 10 MB | `MatricesRiesgosComponent` | Pruebas de texto, PDF, error, tamaño, cierre y descarga de vista previa | Quality gates y E2E autenticado |
| Inactivar evidencia conservando archivo físico | Endpoint y repositorio de inactivación lógica | `InactivarEvidencia_MotivoValido_RecortaYDelega` y prueba de componente | Evidencia funcional API y auditoría |
| Mantener trazabilidad funcional | `RL_MR_HISTORIAL` y endpoint de historial | Pruebas de servicios y flujo API | `fase10_validacion_auditoria.json` y SQL DBA |
| Autorizar el módulo 10 | `ModuloAuthorizeAttribute(10)` y `moduloGuard(10)` | Guards unitarios y E2E autenticado de Matrices | Quality gates |

## Criterio de cierre

La fase se considera cerrada cuando las pruebas Backend/Frontend/E2E están en verde, los pisos de cobertura no retroceden, la evidencia funcional está disponible, la validación DBA puede reproducirse sin escrituras y la aprobación queda registrada en el documento oficial.
