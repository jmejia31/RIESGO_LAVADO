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

## Validacion backend compartida

`backend/RL.API/Infrastructure/Files/FileUploadPolicy.cs` centraliza allowlists y validacion por familia; cada endpoint conserva sus reglas de negocio y delega aqui los controles comunes. La extension y el MIME declarado se contrastan con una allowlist, y el contenido se verifica mediante firma o estructura del formato. El backend no confia en que el nombre o el `Content-Type` enviado por el navegador prueben el formato real.

| Familia | Superficies | Extensiones y limite | Validacion de contenido |
|---|---|---|---|
| Evidencias | Seguimientos de Listas y evidencia fisica de Matrices | PDF, PNG, JPG/JPEG, DOC/DOCX, XLS/XLSX; 10 MB por archivo, salvo limite configurado para evidencias de Listas | Firmas PDF, PNG, JPEG y OLE; estructura ZIP/XML de DOCX/XLSX con DTD prohibido |
| Importacion de listas | Cautela | CSV, XML, XLS/XLSX; tope de aplicacion de 128 MiB (el servidor HTTP puede imponer un limite menor) | texto CSV sin bytes de control, XML con DTD prohibido y estructura de paquete para XLSX; el parser existente conserva la validacion funcional |
| Imagenes | Slides de Configuracion | JPG/JPEG, PNG, GIF, WebP; 10 MiB | firmas y terminadores/longitud del formato; la verificacion de decodificacion de imagen se certifica en la politica especifica de Configuracion |

Los nombres de archivo deben ser simples, sin separadores de ruta; los nombres persistidos se generan en el servidor y las rutas se resuelven dentro del directorio permitido. En Evidencias, la validacion de firma es obligatoria y ya no existe una opcion de configuracion para desactivarla. La carga de archivos de Configuracion tiene una certificacion especifica posterior para cubrir decodificacion y archivos truncados.
