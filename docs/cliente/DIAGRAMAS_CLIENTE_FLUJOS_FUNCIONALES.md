# DIAGRAMAS FUNCIONALES PARA CLIENTE

**Proyecto:** Sistema de Gestión de Riesgos LA/FT  
**Versión:** 1.0  
**Enfoque:** Explicación visual para cliente no técnico  
**Fecha:** 2026-06-29

---

## 1. Propósito de este documento

Este documento explica, de forma sencilla y visual, cómo funcionará el sistema desde el punto de vista del usuario y del negocio.

No utiliza lenguaje de programación, nombres de archivos, tablas, endpoints ni detalles técnicos. Está pensado para presentarse a personal funcional, jefaturas, usuarios administrativos, cumplimiento, auditoría o clientes que necesitan entender qué hace el sistema sin conocer desarrollo de software.

---

## 2. Vista general del sistema

El sistema ayuda a controlar riesgos de lavado de activos y financiamiento del terrorismo mediante registro, revisión, seguimiento, evidencias, reportes y trazabilidad de acciones.

```mermaid
flowchart TD
    A[Usuario ingresa al sistema] --> B[El sistema valida su acceso]
    B --> C[Usuario selecciona un módulo]
    C --> D[Consulta o registra información]
    D --> E[El sistema revisa y guarda la información]
    E --> F[La información queda disponible para seguimiento]
    F --> G[Se pueden adjuntar evidencias]
    G --> H[Se generan reportes o consultas]
    H --> I[Todo queda registrado para auditoría]
```

### Explicación para cliente

1. El usuario entra al sistema.
2. El sistema confirma que el usuario tiene permiso.
3. El usuario trabaja en el módulo que le corresponde.
4. La información se registra, consulta o actualiza.
5. El sistema conserva evidencia y seguimiento.
6. Todo queda trazable para revisión posterior.

---

## 3. Diagrama simple del acceso al sistema

```mermaid
flowchart TD
    A[Usuario escribe sus credenciales] --> B[El sistema verifica si son correctas]
    B --> C{¿Acceso válido?}
    C -- No --> D[El sistema muestra mensaje de error]
    D --> E[Usuario intenta nuevamente]
    C -- Sí --> F[El sistema permite el ingreso]
    F --> G[Se muestra el menú según permisos]
    G --> H[El usuario trabaja solo en los módulos autorizados]
```

### Qué entiende el cliente

El sistema no deja entrar a cualquier persona. Cada usuario ve únicamente lo que tiene autorizado según su rol y permisos.

---

## 4. Registro de una persona o entidad en seguimiento

Este es el proceso cuando se registra una persona, empresa, empleado, patrono o tercero que requiere control dentro del sistema.

```mermaid
flowchart TD
    A[Usuario encuentra una persona o entidad a revisar] --> B[Abre el formulario de registro]
    B --> C[Completa los datos básicos]
    C --> D[Agrega el motivo del registro]
    D --> E[El sistema revisa si ya existe]
    E --> F{¿Ya existe registro activo?}
    F -- Sí --> G[El sistema actualiza la información existente]
    F -- No --> H[El sistema crea un nuevo registro]
    G --> I[El registro queda disponible para seguimiento]
    H --> I
    I --> J[El usuario puede agregar comentarios y evidencias]
    J --> K[El sistema guarda historial de lo realizado]
```

### Explicación para cliente

Cuando se registra una persona o entidad, el sistema primero revisa si ya existe. Si ya existe, no duplica el caso; actualiza el registro. Si no existe, crea uno nuevo. Después queda listo para seguimiento, evidencias y consultas.

---

## 5. Proceso de revisión de coincidencias

```mermaid
flowchart TD
    A[El usuario consulta coincidencias] --> B[El sistema muestra posibles coincidencias]
    B --> C[Usuario revisa el detalle]
    C --> D{¿La coincidencia requiere seguimiento?}
    D -- No --> E[Se descarta o se marca según criterio]
    D -- Sí --> F[Se registra motivo de seguimiento]
    F --> G[Se documenta el caso]
    G --> H[Se puede adjuntar evidencia]
    H --> I[El caso queda disponible para futuras revisiones]
```

### Explicación para cliente

El sistema muestra coincidencias que deben revisarse. El usuario decide, según el análisis, si la coincidencia requiere seguimiento. Si requiere seguimiento, se documenta el motivo y se conserva evidencia.

---

## 6. Proceso de seguimiento de un caso

```mermaid
flowchart TD
    A[Usuario abre un caso registrado] --> B[Revisa información existente]
    B --> C[Agrega comentario de seguimiento]
    C --> D{¿Tiene documentos de respaldo?}
    D -- Sí --> E[Adjunta evidencia]
    D -- No --> F[Guarda solo el comentario]
    E --> G[El sistema valida el documento]
    G --> H{¿Documento válido?}
    H -- No --> I[El sistema solicita corregir el archivo]
    H -- Sí --> J[Se guarda la evidencia]
    F --> K[El seguimiento queda registrado]
    J --> K
    K --> L[El historial del caso queda actualizado]
```

### Explicación para cliente

Cada caso puede tener comentarios y documentos. El sistema valida que los documentos sean aceptables y deja constancia de todo lo agregado.

---

## 7. Proceso de manejo de evidencias

```mermaid
flowchart TD
    A[Usuario adjunta evidencia] --> B[El sistema revisa el archivo]
    B --> C{¿Archivo permitido?}
    C -- No --> D[El sistema rechaza el archivo]
    C -- Sí --> E[El sistema guarda la evidencia]
    E --> F[La evidencia queda asociada al caso]
    F --> G[Otro usuario autorizado puede consultarla]
    G --> H[Cada consulta queda registrada]
    H --> I[Si se elimina, se pide motivo]
    I --> J[La evidencia no se borra sin dejar rastro]
```

### Explicación para cliente

Las evidencias quedan controladas. Si alguien consulta o elimina una evidencia, el sistema deja historial. Esto ayuda a mantener transparencia y trazabilidad.

---

## 8. Proceso de carga de listas de control

```mermaid
flowchart TD
    A[Usuario selecciona archivo de lista] --> B[El sistema revisa el archivo]
    B --> C{¿El archivo tiene formato correcto?}
    C -- No --> D[El sistema muestra error]
    C -- Sí --> E[El sistema procesa la lista]
    E --> F[La información queda disponible para revisión]
    F --> G[Se pueden identificar coincidencias]
    G --> H[Las coincidencias pasan a revisión del usuario]
```

### Explicación para cliente

El usuario puede cargar listas de control. El sistema revisa si el archivo es válido y, si todo está correcto, lo procesa para que las coincidencias puedan ser revisadas.

---

## 9. Proceso de reportes y exportaciones

```mermaid
flowchart TD
    A[Usuario necesita un reporte] --> B[Selecciona filtros o información]
    B --> C[El sistema genera resultados]
    C --> D{¿Desea exportar?}
    D -- No --> E[Consulta la información en pantalla]
    D -- Sí --> F[Exporta el reporte]
    F --> G[El sistema registra la exportación]
    E --> H[Información disponible para análisis]
    G --> H
```

### Explicación para cliente

El sistema permite consultar información y exportarla cuando sea necesario. Si se exporta, queda registrado quién lo hizo y cuándo.

---

## 10. Proceso de auditoría

```mermaid
flowchart TD
    A[Usuario realiza una acción importante] --> B[El sistema guarda la acción]
    B --> C[Se registra quién la hizo]
    C --> D[Se registra cuándo la hizo]
    D --> E[Se registra qué información afectó]
    E --> F[Auditoría puede revisar el historial]
```

### Explicación para cliente

Cada acción importante queda registrada. Esto permite revisar posteriormente qué usuario realizó cambios, consultas, exportaciones o eliminaciones.

---

## 11. Ciclo completo de un caso

```mermaid
flowchart TD
    A[Se detecta una coincidencia o alerta] --> B[Usuario revisa el caso]
    B --> C{¿Requiere seguimiento?}
    C -- No --> D[Se deja constancia de revisión]
    C -- Sí --> E[Se registra el caso]
    E --> F[Se agrega motivo]
    F --> G[Se documenta seguimiento]
    G --> H[Se adjuntan evidencias]
    H --> I[Se consulta o actualiza cuando sea necesario]
    I --> J[Se genera reporte]
    J --> K[Todo queda en auditoría]
```

### Explicación para cliente

Un caso puede iniciar por una coincidencia o alerta. Luego se revisa, se documenta, se le da seguimiento, se agregan evidencias y puede generar reportes. Todo queda registrado.

---

## 12. Vista por módulos para cliente

```mermaid
flowchart TD
    A[Sistema de Gestión de Riesgos LA/FT] --> B[Seguridad]
    A --> C[Monitoreo de listas]
    A --> D[Seguimiento de casos]
    A --> E[Evidencias]
    A --> F[Reportes]
    A --> G[Auditoría]
    A --> H[Configuración]
    B --> I[Controla quién entra]
    C --> J[Detecta y muestra coincidencias]
    D --> K[Permite documentar casos]
    E --> L[Guarda soportes]
    F --> M[Permite consultar y exportar]
    G --> N[Registra acciones]
    H --> O[Configura datos generales]
```

---

## 13. Qué gana el cliente con este sistema

| Beneficio | Explicación sencilla |
|---|---|
| Control de acceso | Solo usuarios autorizados pueden ingresar |
| Menos duplicidad | El sistema revisa si un registro ya existe |
| Seguimiento ordenado | Cada caso puede tener comentarios y evidencias |
| Trazabilidad | Se sabe quién hizo cada acción |
| Mejor documentación | Los casos quedan respaldados |
| Reportes | La información puede consultarse y exportarse |
| Control LA/FT | Ayuda a gestionar riesgos y coincidencias |

---

## 14. Diferencia entre este documento y la documentación técnica

| Documento | Para quién es | Qué contiene |
|---|---|---|
| Documento funcional para cliente | Cliente, usuarios, jefaturas, cumplimiento | Procesos explicados de forma simple |
| Documento técnico | Desarrolladores, Codex, equipo técnico | Código, rutas, componentes, base de datos y lógica interna |

---

## 15. Recomendación de uso

Para reuniones con cliente se recomienda usar este documento y no la documentación técnica.

La documentación técnica debe quedar como respaldo interno para desarrollo, mantenimiento, Codex y control del proyecto.
