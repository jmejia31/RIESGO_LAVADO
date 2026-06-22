# Propuesta de Mejora: Catalogos LAFT para Seguimientos, Estados y Evidencias

## 1. Resumen ejecutivo

Actualmente el sistema ya permite registrar seguimientos y adjuntar evidencias dentro del modulo de Monitoreo de Listas. Esta funcionalidad esta operativa y permite documentar comentarios, cargar archivos, consultar historial y dejar auditoria de las acciones principales.

La mejora propuesta consiste en ordenar y estandarizar esa informacion usando catalogos LAFT. En lugar de depender solamente de texto libre, el usuario podria seleccionar valores controlados como:

- Estado del caso: pendiente, en analisis, confirmado, falso positivo, cerrado.
- Accion realizada: revision, validacion documental, escalamiento, cierre, solicitud de soporte.
- Tipo de evidencia: oficio, informe, captura, acta, constancia, documento.

El objetivo no es cambiar la autenticacion ni los accesos de Active Directory. Active Directory seguiria validando quien es el usuario. Esta mejora se enfoca en que el sistema registre mejor que hizo el usuario dentro del proceso LAFT.

## 2. Que existe actualmente

### En el sistema

Actualmente, en la pantalla de Monitoreo de Listas, cuando se abre el historial de un registro, el usuario puede:

- consultar seguimientos anteriores;
- agregar una nota o comentario;
- adjuntar archivos de evidencia;
- descargar o eliminar evidencias;
- ver el historial de acciones realizadas.

La pantalla funciona, pero la informacion se captura principalmente como comentario libre. Eso permite flexibilidad, pero dificulta clasificar despues los casos por estado, tipo de accion o tipo de evidencia.

### En base de datos

Hoy existen estructuras como:

- `RL_LISTA_POSITIVOS`: registro principal del caso o coincidencia monitoreada.
- `RL_DETALLE_LISTA`: historial de seguimientos o comentarios.
- `RL_DETALLE_EVIDENCIA`: archivos adjuntos como evidencia.
- `RL_TIPOS_DOCUMENTO`: tipos de documento, por ejemplo DNI, RTN o numero patronal.
- `DNP_IHSS.TIPO_LISTAS_CAUTELA`: tipos de listas de cautela.
- `RL_AUDITORIA`: bitacora de acciones del sistema.

Estas tablas ya permiten operar el flujo actual.

## 3. Que queremos mejorar

Queremos pasar de un seguimiento basado principalmente en texto libre a un seguimiento mas estructurado.

Ejemplo actual:

```text
Comentario:
Se reviso el caso y se adjunto informe.
```

Ejemplo propuesto:

```text
Accion realizada: Validacion documental
Estado resultante: En analisis
Tipo de evidencia: Informe
Comentario: Se reviso el caso y se adjunto informe.
Archivo: informe_revision.pdf
```

Con esto, el sistema no solo guarda el comentario, sino tambien clasifica la accion realizada y el estado del caso.

## 4. Que cambios se verian en pantalla

La mejora se veria principalmente en el modulo **Monitoreo de Listas**, especificamente en la ventana de **Seguimiento e Historial de Controles**.

Hoy se muestra:

- Nota o comentario.
- Archivos de evidencia.
- Boton Guardar Seguimiento.

Con la mejora se agregarian campos como:

- Accion realizada.
- Estado resultante.
- Tipo de evidencia.
- Nota o comentario.
- Archivos de evidencia.

Tambien se podria crear una nueva pantalla administrativa llamada **Catalogos LAFT**, donde usuarios autorizados puedan mantener los valores de esos catalogos.

Ejemplo:

- Catalogo de estados LAFT.
- Catalogo de acciones de seguimiento.
- Catalogo de tipos de evidencia.

## 5. Informacion que se vera afectada

La informacion afectada seria solamente la relacionada con el seguimiento de casos LAFT dentro del sistema.

### Se afectaria

- Seguimientos registrados en Monitoreo de Listas.
- Evidencias adjuntas.
- Estados internos del caso.
- Reportes o consultas futuras por estado, accion o tipo de evidencia.
- Auditoria de cambios relacionados con el seguimiento.

### No se afectaria

- Active Directory.
- Claves de usuarios.
- Autenticacion.
- Roles existentes.
- Permisos actuales por modulo.
- Listas de cautela ya cargadas.
- Registros historicos existentes, salvo que se decida migrarlos.

## 6. Cambios tecnicos propuestos

### Backend

Se propone agregar componentes separados para no mezclar esta mejora con los catalogos generales actuales.

Nuevos archivos posibles:

- `CatalogosLaftController.cs`
- `CatalogosLaftService.cs`
- `CatalogosLaftRepository.cs`

Archivos existentes que podrian ajustarse:

- `ListasController.cs`
- `ListasRepository.cs`
- DTOs relacionados con seguimiento y evidencia.

### Frontend

Archivos principales afectados:

- `monitoreo-listas.component.ts`
- `monitoreo-listas.component.html`
- `listas.service.ts`

Pantalla nueva opcional:

- `catalogos-laft.component.ts`
- `catalogos-laft.component.html`

### Base de datos

Tablas nuevas propuestas:

- `RL_CAT_ESTADOS_LAFT`
- `RL_CAT_ACCIONES_SEGUIMIENTO`
- `RL_CAT_TIPOS_EVIDENCIA`

Columnas nuevas posibles:

En `RL_DETALLE_LISTA`:

- `DLL_ESTADO_LAFT_ID`
- `DLL_ACCION_SEGUIMIENTO_ID`

En `RL_DETALLE_EVIDENCIA`:

- `EVI_TIPO_EVIDENCIA_ID`

Tambien podria crearse una tabla historica si se desea controlar cambios de estado de forma mas formal:

- `RL_HIST_ESTADOS_LAFT`

Esta tabla permitiria saber cuando cambio un caso de pendiente a en analisis, de en analisis a confirmado, etc.

## 7. De donde vendra y a donde ira la informacion

### Flujo actual

1. El usuario inicia sesion con su cuenta institucional.
2. Active Directory valida su identidad.
3. El sistema carga sus permisos internos.
4. El usuario entra a Monitoreo de Listas.
5. Registra un comentario y adjunta evidencia.
6. El sistema guarda el comentario en `RL_DETALLE_LISTA`.
7. El sistema guarda la evidencia en `RL_DETALLE_EVIDENCIA`.
8. La bitacora registra la accion en `RL_AUDITORIA`.

### Flujo propuesto

1. El usuario inicia sesion igual que hoy.
2. Active Directory valida la identidad igual que hoy.
3. El sistema carga permisos igual que hoy.
4. El usuario abre Monitoreo de Listas.
5. El sistema consulta los catalogos LAFT:
   - estados desde `RL_CAT_ESTADOS_LAFT`;
   - acciones desde `RL_CAT_ACCIONES_SEGUIMIENTO`;
   - tipos de evidencia desde `RL_CAT_TIPOS_EVIDENCIA`.
6. El usuario registra seguimiento seleccionando valores controlados.
7. El sistema guarda:
   - comentario en `RL_DETALLE_LISTA`;
   - accion seleccionada en `DLL_ACCION_SEGUIMIENTO_ID`;
   - estado resultante en `DLL_ESTADO_LAFT_ID`;
   - archivo en `RL_DETALLE_EVIDENCIA`;
   - tipo de evidencia en `EVI_TIPO_EVIDENCIA_ID`.
8. La bitacora registra la accion en `RL_AUDITORIA`.

## 8. Impacto en los tres ambientes

### Ambiente de desarrollo

En desarrollo se harian los cambios iniciales de codigo y base de datos.

Se validaria:

- que los nuevos catalogos carguen correctamente;
- que el formulario de seguimiento guarde los nuevos campos;
- que las evidencias se mantengan funcionando;
- que la auditoria registre los cambios;
- que no se afecte el login con Active Directory.

Este ambiente es donde se ajustaria el diseno y la logica antes de pasar a pruebas.

### Ambiente de pruebas

En pruebas se verificaria el flujo completo con usuarios reales o usuarios de prueba.

Se validaria:

- registro de seguimiento con estado, accion y evidencia;
- actualizacion de seguimientos existentes;
- descarga y eliminacion de evidencias;
- reportes o consultas por estado;
- permisos por modulo;
- auditoria;
- comportamiento con usuarios Active Directory.

Tambien aqui se revisaria si es necesario migrar datos historicos o dejarlos como estan.

### Ambiente de produccion

En produccion se aplicarian los cambios ya validados.

El despliegue deberia hacerse de forma controlada:

1. respaldo de base de datos;
2. ejecucion de scripts de nuevas tablas/columnas;
3. despliegue backend;
4. despliegue frontend;
5. prueba rapida de login, Monitoreo de Listas y Bitacora;
6. confirmacion con usuarios clave.

La recomendacion es no aplicar directamente en produccion sin pasar antes por pruebas.

## 9. Riesgos y cuidados

### Riesgos

- Cambiar formularios puede requerir capacitacion breve a usuarios.
- Si se agregan columnas obligatorias sin valores por defecto, podria fallar el guardado de seguimientos antiguos.
- Si se migra informacion historica, hay que definir reglas claras.
- Un catalogo mal configurado podria afectar la calidad de los reportes.

### Cuidados recomendados

- No hacer obligatorios los nuevos campos al inicio, o usar valores por defecto como "No clasificado".
- Mantener comentario libre para no perder contexto humano.
- Auditar cambios de catalogos.
- Hacer pruebas con casos reales.
- No tocar Active Directory ni autenticacion.

## 10. Beneficios esperados

La mejora permitiria:

- saber cuantos casos estan pendientes, en analisis, confirmados o cerrados;
- identificar que acciones se realizaron sobre cada caso;
- clasificar evidencias por tipo;
- mejorar reportes de seguimiento;
- reducir ambiguedad en comentarios libres;
- fortalecer auditoria y cumplimiento;
- facilitar supervision por parte de jefaturas o cumplimiento.

## 11. Recomendacion

La recomendacion es manejar esta mejora como una **fase posterior**, no como un bloqueo del cierre actual de modulos base.

El sistema ya permite registrar seguimientos y evidencias. Lo que se propone es mejorar la calidad y trazabilidad de esa informacion mediante catalogos formales.

Para cierre actual, el punto podria documentarse asi:

```text
Catalogos base: Completo con observacion.
Los catalogos actuales de sistema, tipos de documento y tipos de listas se encuentran operativos.
La separacion de catalogos LAFT para estados, acciones y tipos de evidencia se recomienda como mejora futura para fortalecer trazabilidad, reporteria y control de seguimiento.
```

## 12. Decision pendiente

Antes de implementar, se deberia confirmar:

- si el jefe o cliente requiere estados formales de caso;
- cuales seran los estados permitidos;
- cuales seran las acciones de seguimiento;
- cuales seran los tipos de evidencia;
- si los campos seran obligatorios u opcionales;
- si se migraran datos historicos;
- quien administrara los catalogos LAFT.
