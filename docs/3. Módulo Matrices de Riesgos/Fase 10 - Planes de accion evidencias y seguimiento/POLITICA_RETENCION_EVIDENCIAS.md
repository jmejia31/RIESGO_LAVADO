# Política operativa de retención de evidencias

Estado: lineamiento aplicable a Fase 10; el plazo legal definitivo debe ser ratificado por Cumplimiento, Archivo Institucional y Seguridad de la Información.

## Principios

1. Las evidencias son información institucional restringida y se almacenan fuera de `wwwroot`, con acceso exclusivo mediante la API autorizada.
2. La metadata Oracle y el archivo físico forman una sola unidad de custodia. Cada archivo debe conservar nombre físico no predecible, tamaño, MIME, extensión, SHA-256, usuario y fecha.
3. La inactivación es lógica: oculta la evidencia de los flujos activos, registra motivo/usuario/fecha y no elimina el archivo físico.
4. No se ejecutará eliminación automática mientras el plazo institucional y cualquier suspensión por auditoría, investigación o litigio no estén aprobados.

## Acceso y custodia

- Aplicar mínimo privilegio al directorio de evidencias y al esquema Oracle.
- Prohibir enlaces públicos, exposición de rutas físicas y credenciales dentro del repositorio.
- Registrar cargas, descargas, inactivaciones y reactivaciones con usuario, fecha, IP y motivo cuando corresponda.
- Validar firma real, MIME, extensión y límite de tamaño antes de aceptar la carga.
- Integrar el almacenamiento con el control antimalware institucional antes de producción cuando dicho servicio esté disponible; hasta entonces, mantener la lista cerrada de formatos y la validación de firma.

## Respaldo y recuperación

- Incluir metadata y archivos en el mismo plan de respaldo, con cifrado en tránsito y reposo según la plataforma institucional.
- Probar restauración de una muestra y verificar SHA-256 al menos en cada ejercicio institucional de continuidad.
- Documentar responsable, fecha, alcance, resultado y excepciones de cada prueba de restauración.

## Retención y disposición

- La fecha base es la de cierre definitivo de la matriz o, si es posterior, la del último evento de auditoría/investigación relacionado.
- El plazo numérico no se fija en este repositorio para evitar contradecir normativa vigente; debe configurarse solo después de aprobación formal institucional.
- Toda disposición requiere autorización dual del propietario funcional y del custodio documental, evidencia del hash afectado y registro inalterable de la operación.
- Una retención legal, investigación o auditoría suspende cualquier eliminación hasta notificación escrita de liberación.

## Revisión

Revisar esta política al menos una vez por año o cuando cambie la normativa, el almacenamiento, el proveedor antimalware o el modelo de autorización. El propietario funcional debe conservar la aprobación vigente fuera del código fuente y referenciarla en el expediente de la fase.
