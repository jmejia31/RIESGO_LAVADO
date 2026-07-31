# Plan de Implementación Técnica Consolidado — Ajustes de Diseño, Seguridad y Reportes en Oracle (Aprobado para Ejecución)

Este documento define el Plan de Implementación definitivo para el módulo de Matrices de Riesgos, integrando el Dictamen Consolidado y las correcciones de diseño y arquitectura de los socios.

---

# 1. Veredicto Ejecutivo y Fases de Trabajo

El desarrollo de los ajustes operativos y de reportería queda autorizado exclusivamente en la rama `desarrollo`. Queda prohibido mantener una estructura híbrida con las tablas antiguas (`RL_MR_MODELOS`, `RL_MR_FACTORES`, `RL_MR_VARIABLES`, `RL_MR_ESCALAS`, `RL_MR_CRITERIOS`).

---

# 2. Ajustes en Frontend Angular (`frontend/rl-app`)

### 2.1 Reducción y Compactación Accesible del Mapa de Calor
Se optimizarán las dimensiones del mapa en [matrices-riesgos.component.html](file:///c:/RIESGO_LAVADO/frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html):
- **Celdas del Mapa**: Altura mínima de `min-h-11` (equivalente a **44px**) y ancho de columnas mínimo de **44px** (`minmax(44px, 1fr)`) para garantizar accesibilidad táctil.
- **Eje Y de Etiquetas**: Compactar a `80px`.
- **Textos e Indicadores**: Número total en `text-sm`/`text-base` y promedio secundario en `text-[10px]` o `text-[11px]` (nunca menor a `text-[10px]` para preservar la legibilidad). Se complementará con tooltip `title` y `aria-label`.
- **Dimensiones del Contenedor**: Ajustado a `min-w-[380px] max-w-xl`.

### 2.2 Remoción de la opción "Archivo adjunto" y Compatibilidad Histórica
- **Diseño**: Se eliminará la opción `Archivo adjunto` del maquetador visual. El backend rechazará semánticamente plantillas nuevas que contengan este tipo.
- **Historial**: Las versiones históricas con el tipo `archivo` se abrirán sin error en modo solo lectura, mostrando un enlace al flujo de evidencias relacionales correspondientes. No se intentará recuperar ni almacenar el binario/Base64 desde el JSON `EVA_DATA_JSON`.

### 2.3 Eliminación visual de la palabra "JSON"
- Se reemplazará el término en toda la interfaz funcional (por ejemplo, cambiando `Editar JSON` por `Diseñar plantilla` y `Editor de Esquema JSON` por `Diseñador de Plantilla`).
- Internamente, se conservará la terminología en los contratos, base de datos (`VER_JSON`, `EVA_DATA_JSON`), validaciones y bitácoras técnicas.

---

# 3. Ajustes de Seguridad y Autorización en Backend (`backend/RL.API`)

### 3.1 Centralización de Roles de Administración
- Se verificará previamente el claim del rol en el JWT. Confirmado que el valor real es `"ADMINISTRADOR"`, se centralizarán en la constante:
  ```csharp
  public static class MatricesRiesgosRoles
  {
      public const string AdministracionFormularios = "ADMINISTRADOR,ADMIN,DBA,RIESGOS_ADMIN";
  }
  ```
- Se aplicará `[Authorize(Roles = MatricesRiesgosRoles.AdministracionFormularios)]` a los endpoints de plantillas.
- Se mantendrá la validación de `[ModuloAuthorize(10)]` a nivel de clase controladora para verificar el acceso al Módulo 10 a partir de los claims del JWT.

### 3.2 Pruebas de Autorización Separadas
- Las pruebas de ruteo HTTP, JWT y validaciones de código (401 y 403) se implementarán en una clase de integración separada:
  ```text
  MatricesRiesgosAuthorizationIntegrationTests.cs
  ```
  Esto evitará mezclar la lógica de autorización del pipeline HTTP con las pruebas unitarias directas del controlador en `MatricesRiesgosControllerTests.cs`.

---

# 4. Implementación Relacional y Parametrizada en Oracle 11g

### 4.1 Remoción absoluta de `EVA_ESTADO` y `EvaEstado`
- Se realizará una búsqueda y eliminación exhaustiva de `EVA_ESTADO` y `EvaEstado` en todo el proyecto. Ninguna operación leerá o insertará estado en `RL_MR_EVALUACIONES_RIESGO`.
- El estado actual se obtendrá uniendo la tabla con `RL_MR_FLUJOS_EVALUACION`:
```sql
WITH ULTIMO_FLUJO AS (
    SELECT
        F.FLU_EVALUACION_ID,
        F.FLU_ESTADO,
        F.FLU_FECHA,
        ROW_NUMBER() OVER (
            PARTITION BY F.FLU_EVALUACION_ID
            ORDER BY F.FLU_FECHA DESC, F.FLU_ID DESC
        ) AS RN
    FROM RL_MR_FLUJOS_EVALUACION F
)
SELECT
    E.*,
    UF.FLU_ESTADO AS ESTADO_ACTUAL
FROM RL_MR_EVALUACIONES_RIESGO E
JOIN ULTIMO_FLUJO UF
    ON UF.FLU_EVALUACION_ID = E.EVA_ID
   AND UF.RN = 1
WHERE E.EVA_ACTIVO = 1;
```

### 4.2 Definición de Dos Consultas de Evaluación Actual
1. **Evaluación Oficial Vigente (`ObtenerEvaluacionOficialVigente`)**:
   - Retorna la última evaluación activa en estado `APROBADA` (`EVA_ACTIVO = 1`). Utilizada para dashboard ejecutivo, reportes, Excel y PDF oficiales.
   - **Riesgos sin evaluación aprobada**: No se utilizarán borradores o revisiones para el cómputo oficial; se contabilizarán como "Sin evaluación oficial".
2. **Evaluación Operativa Actual (`ObtenerEvaluacionOperativaActual`)**:
   - Retorna la última evaluación operativa activa (`EVA_ACTIVO = 1`) ordenada por fecha e ID, descartando estados `RECHAZADA` o `CERRADA`. Utilizada para bandejas de edición.

### 4.3 Restricción de Unicidad Física en Oracle
- Se creará un nuevo script de migración idempotente y seguro:
  `database/19_matrices_riesgos/instalacion/05_ajustes_dashboard_seguridad_reportes.sql`
- Validará si existe la restricción, buscará y reportará duplicados existentes de `PROY_EVALUACION_ID` y añadirá la restricción `UQ_RL_MR_PROY_EVA` de forma segura bajo el argumento `EJECUTAR`.
- El guardado de la evaluación y su proyección asociada se realizarán atómicamente dentro de la misma transacción en el backend.

### 4.4 Rediseño completo de la Metodología Dinámica
- `ObtenerMetodologiaVigenteAsync` se re-escribirá completamente: vigencia mediante `VER_VIGENTE = 1`, estructura del formulario desde `RL_MR_VERSIONES_FORMULARIO`, campos canónicos desde `RL_MR_CAMPOS_FORMULARIO`, catálogos desde `RL_MR_CATALOGOS` y `RL_MR_ELEMENTOS_CATALOGO`, y reglas desde `RL_MR_REGLAS_CALCULO`.

### 4.5 Sustitución de Contratos Heredados
- Se retirará la alusión a `ModeloId`, `ModeloVersion`, `SujetoTipo`, `SujetoIdExt`, `Documento`, `NombreSujeto`, `Factores`, `Variables`, `PorFactor` en todas las capas del sistema, alineando los DTOs y renderizadores PDF/Excel al nuevo dominio dinámico plano.

### 4.6 Concreción de la Auditoría y Límites de Exportación
- **Auditoría**: Se validará el servicio de auditoría transversal del sistema. Si no admite la estructura requerida (filtros JSON, formato, registros, correlación), se creará la tabla `RL_MR_EXPORTACIONES`. Queda prohibido asociar exportaciones a un `EVA_ID` ficticio.
- **Límites**: Se limitará la consulta de exportación a un máximo controlado de registros (ej. 10,000), con control de tiempo de respuesta en base de datos, uso obligatorio de `CancellationToken` y respuestas estructuradas ante límites excedidos.

---

# 5. Endpoints de API a Implementar

Se expondrán los siguientes endpoints:
1. `GET /api/matrices-riesgos/evaluaciones`: Listado operativo de evaluaciones paginado.
2. `GET /api/matrices-riesgos/dashboard`: Estadísticas del panel y mapa de transición por cuadrantes (`nivelInherente` y `nivelResidual`).
3. `GET /api/matrices-riesgos/reportes`: Datos consolidados para reporte.
4. `GET /api/matrices-riesgos/reportes/exportar`: Descarga física en Excel o PDF.
5. `GET /api/matrices-riesgos/evaluaciones/{evaluacionId}/reportes/ficha`: Ficha individual en PDF.
- *Alias*: `GET /api/matrices-riesgos` se mantendrá únicamente como alias del listado para evitar rupturas y se retirará posteriormente.

---

# 6. Criterios de Aceptación y Verificación

1. No existen referencias a las tablas antiguas ni a `EVA_ESTADO` en todo el proyecto.
2. Cada cambio de estado de evaluación se registra como una fila en `RL_MR_FLUJOS_EVALUACION`.
3. Restricción física de unicidad `UQ_RL_MR_PROY_EVA` aplicada de forma segura.
4. El dashboard utiliza exclusivamente evaluaciones oficiales, y la bandeja las operativas. Los riesgos sin evaluación aprobada se listan como "Sin evaluación oficial".
5. Los cinco endpoints responden correctamente sin errores 404.
6. Se conserva paridad funcional al 100% entre las exportaciones PDF y Excel.
7. Se prueban unitariamente las autorizaciones HTTP con JWT simulados en un TestServer integrado.
8. La documentación técnica, la bitácora y el estado de la colaboración están actualizados en GitHub.
