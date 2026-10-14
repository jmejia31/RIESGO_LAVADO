# Estándar institucional de carga de archivos

**Todas las interfaces de carga de archivos/documentos deben utilizar InstitutionalFileUploadComponent.**

## Componente compartido

`frontend/rl-app/src/app/shared/components/institutional-file-upload/` contiene el componente de selección y presentación `app-institutional-file-upload` y las políticas tipadas de cada consumidor. La superficie institucional mantiene una única zona dashed, centrada, accesible, con selección por click, teclado y drag/drop, lista de archivos pendientes y retiro local antes de persistir.

El componente no conoce módulos, entidades, autorización ni endpoints. El padre conserva la responsabilidad del upload HTTP, asociación con la entidad, persistencia, auditoría y autorización. El backend continúa siendo autoridad para formato, tamaño, nombre y permisos.

## Política por consumidor

- Evidencias de Monitoreo de Listas y Matrices de Riesgos: PDF, imágenes, Word y Excel, máximo 10 MB. La política de Monitoreo de Listas se deriva de la respuesta del backend; la política de Matrices refleja el límite validado por el servicio.
- Importación de Listas: CSV, XLSX y XML. La UI no inventa un límite de tamaño que no esté definido por el contrato.
- Imágenes de slides de Configuración: JPG, JPEG, PNG, GIF y WebP, conforme a las extensiones aceptadas por el endpoint.

Los consumidores pueden configurar label, extensiones, tamaño, cantidad, selección múltiple, requerido y estado disabled sin clonar la UI. Una política de presentación no amplía la aceptación del backend.

## Superficies auditadas

| Contexto | Superficie | Estado |
|---|---|---|
| Monitoreo de Listas | Primer seguimiento y nuevos adjuntos del seguimiento | Usa shared uploader |
| Listas | Importar archivo de cautela | Usa shared uploader |
| Matrices de Riesgos | Evidencia por control | Usa shared uploader |
| Configuración | Imagen de slide de login | Usa shared uploader |

El HTML contiene cinco instancias de carga de usuario en esos cuatro templates. No hay `<input type="file">` fuera del componente compartido. La salida de archivos PDF/Excel y las vistas previas que manejan blobs no son superficies de selección/carga y no se migran.

## Validación automática

`node tools/validate_institutional_file_upload_standard.js` escanea TypeScript y templates Angular, falla ante un input de archivo crudo fuera del componente, exige un solo componente declarado y una sola implementación del texto canónico `Arrastra archivos aquí o haz clic`, y verifica los consumidores de Listas y Matrices. Quality Gates y el quality gate local ejecutan el validador para impedir regresiones.
