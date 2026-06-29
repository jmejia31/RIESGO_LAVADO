# DIAGRAMAS PARA CLIENTE - VERSIÓN COMPATIBLE CON GITHUB

**Proyecto:** Sistema de Gestión de Riesgos LA/FT  
**Versión:** 1.0  
**Enfoque:** Cliente no técnico  
**Formato:** Compatible con vista normal de GitHub, sin Mermaid  
**Fecha:** 2026-06-29

---

## 1. Propósito

Este documento explica los procesos principales del sistema con diagramas sencillos hechos con flechas y pasos.  
No usa lenguaje técnico, programación, tablas de base de datos, endpoints ni nombres internos del sistema.

Esta versión se creó porque algunos visores de GitHub no muestran correctamente los diagramas enriquecidos.

---

# 2. Vista general del sistema

```text
[Usuario ingresa al sistema]
          ↓
[El sistema valida su acceso]
          ↓
[Usuario selecciona un módulo]
          ↓
[Consulta o registra información]
          ↓
[El sistema revisa y guarda la información]
          ↓
[La información queda disponible para seguimiento]
          ↓
[Se adjuntan evidencias si aplica]
          ↓
[Se generan consultas o reportes]
          ↓
[Todo queda registrado para auditoría]
```

## Explicación para cliente

El sistema permite que un usuario autorizado ingrese, trabaje en los módulos permitidos, registre información, adjunte evidencias, consulte reportes y deje trazabilidad de todo lo realizado.

---

# 3. Acceso al sistema

```text
[Usuario escribe sus credenciales]
          ↓
[El sistema verifica si son correctas]
          ↓
     ¿Acceso válido?
       /        \
     No          Sí
     ↓           ↓
[Mensaje de   [Ingreso permitido]
 error]             ↓
     ↓        [Se muestra el menú]
[Intentar           ↓
 de nuevo]    [Usuario trabaja solo
               en módulos autorizados]
```

## Explicación para cliente

El sistema controla que solamente ingresen usuarios autorizados. Además, cada usuario solo puede ver y usar las opciones que le corresponden.

---

# 4. Registro de una persona o entidad

```text
[Usuario encuentra una persona o entidad a revisar]
          ↓
[Abre el formulario de registro]
          ↓
[Completa los datos básicos]
          ↓
[Agrega el motivo del registro]
          ↓
[El sistema revisa si ya existe]
          ↓
      ¿Ya existe?
       /       \
     Sí         No
     ↓          ↓
[Actualiza   [Crea nuevo
 registro]    registro]
     \          /
      \        /
       ↓      ↓
[El registro queda disponible para seguimiento]
          ↓
[Se pueden agregar comentarios y evidencias]
          ↓
[El sistema guarda historial de lo realizado]
```

## Explicación para cliente

Cuando se registra una persona, empresa, empleado, patrono o tercero, el sistema revisa si ya existe. Si ya existe, actualiza el registro; si no existe, crea uno nuevo. Así se evita duplicar información.

---

# 5. Revisión de coincidencias

```text
[Usuario consulta coincidencias]
          ↓
[El sistema muestra posibles coincidencias]
          ↓
[Usuario revisa el detalle]
          ↓
 ¿Requiere seguimiento?
       /        \
     No          Sí
     ↓           ↓
[Se deja     [Se registra motivo]
 constancia]        ↓
               [Se documenta el caso]
                    ↓
               [Se adjunta evidencia si aplica]
                    ↓
               [El caso queda disponible
                para futuras revisiones]
```

## Explicación para cliente

El sistema muestra coincidencias que deben ser revisadas. El usuario analiza si requieren seguimiento y documenta la decisión.

---

# 6. Seguimiento de un caso

```text
[Usuario abre un caso registrado]
          ↓
[Revisa la información existente]
          ↓
[Agrega comentario de seguimiento]
          ↓
 ¿Tiene documentos de respaldo?
       /        \
     No          Sí
     ↓           ↓
[Guarda       [Adjunta evidencia]
 comentario]        ↓
     ↓         [El sistema revisa el archivo]
     ↓               ↓
     ↓          ¿Archivo válido?
     ↓            /      \
     ↓          No        Sí
     ↓          ↓         ↓
     ↓     [Solicita  [Guarda evidencia]
     ↓      corregir]       ↓
      \                  /
       \                /
        ↓              ↓
[El seguimiento queda registrado]
          ↓
[El historial del caso queda actualizado]
```

## Explicación para cliente

Cada caso puede tener comentarios, documentos y evidencias. El sistema mantiene el historial completo de lo agregado.

---

# 7. Manejo de evidencias

```text
[Usuario adjunta evidencia]
          ↓
[El sistema revisa el archivo]
          ↓
 ¿Archivo permitido?
       /        \
     No          Sí
     ↓           ↓
[Rechaza     [Guarda evidencia]
 archivo]          ↓
              [La evidencia queda asociada al caso]
                    ↓
              [Usuarios autorizados pueden consultarla]
                    ↓
              [Cada consulta queda registrada]
                    ↓
              [Si se elimina, se solicita motivo]
                    ↓
              [No se pierde la trazabilidad]
```

## Explicación para cliente

Las evidencias se controlan. El sistema registra si se consultan o eliminan, y no permite eliminar sin justificación.

---

# 8. Carga de listas de control

```text
[Usuario selecciona archivo de lista]
          ↓
[El sistema revisa el archivo]
          ↓
 ¿Formato correcto?
       /        \
     No          Sí
     ↓           ↓
[Muestra     [Procesa la lista]
 error]            ↓
              [La información queda disponible]
                    ↓
              [El sistema identifica coincidencias]
                    ↓
              [Las coincidencias pasan a revisión]
```

## Explicación para cliente

El sistema permite cargar listas de control. Si el archivo es correcto, lo procesa y deja disponibles las coincidencias para revisión.

---

# 9. Reportes y exportaciones

```text
[Usuario necesita un reporte]
          ↓
[Selecciona filtros o información]
          ↓
[El sistema genera resultados]
          ↓
 ¿Desea exportar?
       /        \
     No          Sí
     ↓           ↓
[Consulta     [Exporta reporte]
 en pantalla]       ↓
     ↓        [El sistema registra
     ↓         la exportación]
      \          /
       \        /
        ↓      ↓
[Información disponible para análisis]
```

## Explicación para cliente

El usuario puede consultar información en pantalla o exportarla. Si la exporta, el sistema registra quién lo hizo.

---

# 10. Auditoría del sistema

```text
[Usuario realiza una acción importante]
          ↓
[El sistema guarda la acción]
          ↓
[Se registra quién la hizo]
          ↓
[Se registra cuándo la hizo]
          ↓
[Se registra qué información afectó]
          ↓
[Auditoría puede revisar el historial]
```

## Explicación para cliente

El sistema permite revisar posteriormente las acciones importantes realizadas por los usuarios.

---

# 11. Ciclo completo de un caso

```text
[Se detecta coincidencia o alerta]
          ↓
[Usuario revisa el caso]
          ↓
 ¿Requiere seguimiento?
       /        \
     No          Sí
     ↓           ↓
[Se deja     [Se registra el caso]
 constancia]        ↓
               [Se agrega motivo]
                    ↓
               [Se documenta seguimiento]
                    ↓
               [Se adjuntan evidencias]
                    ↓
               [Se consulta o actualiza]
                    ↓
               [Se genera reporte si aplica]
                    ↓
               [Todo queda en auditoría]
```

## Explicación para cliente

Un caso puede iniciar por una coincidencia o alerta. Luego se revisa, se documenta, se le da seguimiento, se agregan evidencias y se genera auditoría.

---

# 12. Vista por módulos

```text
[Sistema de Gestión de Riesgos LA/FT]
          |
          |-- [Seguridad]
          |       └─ Controla quién entra
          |
          |-- [Monitoreo de listas]
          |       └─ Detecta y muestra coincidencias
          |
          |-- [Seguimiento de casos]
          |       └─ Permite documentar casos
          |
          |-- [Evidencias]
          |       └─ Guarda soportes y documentos
          |
          |-- [Reportes]
          |       └─ Permite consultar y exportar información
          |
          |-- [Auditoría]
          |       └─ Registra acciones importantes
          |
          |-- [Configuración]
                  └─ Ajusta datos generales del sistema
```

---

# 13. Beneficios para el cliente

| Beneficio | Explicación sencilla |
|---|---|
| Control de acceso | Solo ingresan usuarios autorizados |
| Menos duplicidad | El sistema revisa si la información ya existe |
| Seguimiento ordenado | Cada caso puede documentarse con comentarios |
| Evidencias controladas | Los documentos quedan asociados al caso |
| Trazabilidad | Se sabe quién hizo cada acción |
| Reportes | La información puede consultarse y exportarse |
| Control LA/FT | Ayuda a gestionar coincidencias y riesgos |

---

# 14. Recomendación de uso

Este documento es el recomendado para mostrar al cliente.

La documentación técnica debe usarse únicamente para desarrollo interno, revisión de código, mantenimiento y continuidad técnica del sistema.
