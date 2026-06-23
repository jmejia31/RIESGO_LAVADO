# Motivo de Eliminacion en Monitoreo de Listas

## Objetivo

Fortalecer la trazabilidad del modulo de Monitoreo de Listas exigiendo un motivo obligatorio antes de eliminar evidencias o seguimientos.

Este cambio mejora la auditoria sin cambiar la estructura de base de datos y sin realizar eliminaciones fisicas.

## Alcance Implementado

Aplica solamente a:

- Evidencias asociadas a seguimientos.
- Seguimientos historicos.

No aplica en esta fase a:

- Roles o permisos por accion.
- Active Directory.
- Tablas DNP.
- Tipos de lista.
- Slides de configuracion.
- Estructura de base de datos.

## Flujo Funcional

1. El usuario presiona eliminar evidencia o eliminar seguimiento.
2. El sistema solicita un motivo obligatorio en pantalla.
3. Si el motivo esta vacio, la eliminacion no continua.
4. El frontend envia el motivo al backend.
5. El backend vuelve a validar que el motivo exista.
6. El repositorio realiza eliminacion logica.
7. La bitacora guarda el estado anterior y el motivo de eliminacion.

## Eliminacion Logica

El sistema no borra fisicamente la evidencia ni el seguimiento.

Para evidencias:

- Se actualiza `RL_DETALLE_EVIDENCIA.EVI_ESTADO_REGISTRO = 0`.
- Se registra el usuario en `EVI_USR_INACTIVO_ID`.
- Se registra la fecha en `EVI_FECHA_INACTIVO`.
- El archivo fisico se conserva en servidor.

Para seguimientos:

- Se actualiza `RL_DETALLE_LISTA.DLL_ESTADO_REGISTRO = 0`.
- Se registra el usuario en `DLL_USR_INACTIVO_ID`.
- Se registra la fecha en `DLL_FECHA_INACTIVO`.

## Auditoria

El motivo de eliminacion se guarda en `RL_AUDITORIA.AUD_DATOS_NVO`.

Ejemplo conceptual:

```json
{
  "Estado": 0,
  "UsrInactivoId": 4,
  "TipoEliminacion": "LOGICA",
  "MotivoEliminacion": "Archivo cargado por error"
}
```

La accion registrada sigue siendo:

```text
DELETE
```

Esto permite que la bitacora continue filtrando eliminaciones de forma consistente.

## Filtro Rapido en Bitacora

La pantalla de bitacora incluye el acceso rapido `Documentos eliminados`.

Ese filtro no usa una tabla adicional. Internamente consulta `RL_AUDITORIA` con:

- `AUD_ACCION = DELETE`
- `AUD_MODULO = MonitoreoListas`
- `AUD_TABLA = RL_DETALLE_EVIDENCIA`

El filtro formal por tabla se envia como parametro `tabla` al endpoint de auditoria, para evitar usar el campo de busqueda general como filtro tecnico.

## Archivos Principales

- `backend/RL.API/DTOs/CatalogoPositivosDto.cs`
- `backend/RL.API/Controllers/AuditoriaController.cs`
- `backend/RL.API/Controllers/ListasController.cs`
- `backend/RL.API/Repositories/AuditoriaRepository.cs`
- `backend/RL.API/Repositories/ListasRepository.cs`
- `frontend/rl-app/src/app/core/services/auditoria.service.ts`
- `frontend/rl-app/src/app/core/services/listas.service.ts`
- `frontend/rl-app/src/app/features/admin/bitacora/bitacora.component.ts`
- `frontend/rl-app/src/app/features/admin/monitoreo-listas/monitoreo-listas.component.ts`

## Consideraciones

Este cambio no implementa todavia la restriccion de "solo Cumplimiento".

La validacion por rol debe agregarse cuando negocio confirme oficialmente que rol representa a Cumplimiento, por ejemplo:

- `SUPERVISOR`
- `CUMPLIMIENTO`
- otro rol definido por administracion

## Validacion Tecnica

Despues de implementar el cambio se debe validar:

- El backend compila correctamente.
- El frontend compila correctamente.
- No se puede eliminar evidencia sin motivo.
- No se puede eliminar seguimiento sin motivo.
- La bitacora registra accion `DELETE`.
- `AUD_DATOS_NVO` contiene `MotivoEliminacion`.
