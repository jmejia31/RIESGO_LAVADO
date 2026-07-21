from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8-sig")


def write(path: str, content: str) -> None:
    (ROOT / path).write_text(content, encoding="utf-8")


def replace_once(content: str, old: str, new: str, label: str) -> str:
    if old in content:
        return content.replace(old, new, 1)
    if new in content:
        return content
    raise RuntimeError(f"No se encontró el bloque requerido: {label}")


def replace_regex(content: str, pattern: str, replacement: str, label: str) -> str:
    updated, count = re.subn(pattern, replacement, content, count=1, flags=re.S)
    if count == 1:
        return updated
    if replacement.strip() and replacement.strip() in content:
        return content
    raise RuntimeError(f"No se encontró el patrón requerido: {label}")


repo_interface_path = "backend/RL.API/Features/MatricesRiesgos/Persistence/IMatricesRiesgosRepository.cs"
app_interface_path = "backend/RL.API/Features/MatricesRiesgos/Application/IMatricesRiesgosAppService.cs"
app_service_path = "backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosAppService.cs"
repository_path = "backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs"
controller_path = "backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs"
frontend_service_path = "frontend/rl-app/src/app/features/admin/matrices-riesgos/data-access/matrices-riesgos.service.ts"
component_path = "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts"
template_path = "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html"
backend_tests_path = "backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosApplicationTests.cs"
service_tests_path = "frontend/rl-app/src/app/features/admin/matrices-riesgos/data-access/matrices-riesgos.service.spec.ts"
component_tests_path = "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.spec.ts"

# Interfaces backend
content = read(repo_interface_path)
content = replace_once(
    content,
    "    Task<bool> InactivarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip);\n    Task<bool> EliminarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip);",
    "    Task<bool> InactivarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip);\n    Task<bool> ReactivarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip);\n    Task<bool> CriterioTieneUsoHistoricoAsync(long criterioId);\n    Task<bool> EliminarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip);",
    "contratos de repositorio para reactivación e histórico",
)
write(repo_interface_path, content)

content = read(app_interface_path)
content = replace_once(
    content,
    "    Task<ServiceResult> InactivarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);\n    Task<ServiceResult> EliminarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);",
    "    Task<ServiceResult> InactivarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);\n    Task<ServiceResult> ReactivarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);\n    Task<ServiceResult> EliminarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip);",
    "contrato de aplicación para reactivación",
)
write(app_interface_path, content)

# AppService: reactivación y bloqueo preventivo de eliminación
content = read(app_service_path)
reactivar_app = '''
    public async Task<ServiceResult> ReactivarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        if (criterioId <= 0)
            return ServiceResult.BadRequest("El identificador del criterio es obligatorio.");

        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))
            return ServiceResult.BadRequest("El motivo de reactivación del criterio es obligatorio.");

        try
        {
            var ok = await _repo.ReactivarCriterioAsync(criterioId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);
            return ok
                ? ServiceResult.Ok("Criterio activado correctamente.")
                : ServiceResult.NotFound("No se encontró el criterio inactivo.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.BadRequest(ex.Message);
        }
    }
'''
content = replace_once(
    content,
    "    public async Task<ServiceResult> EliminarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)\n    {",
    reactivar_app + "\n    public async Task<ServiceResult> EliminarCriterioAsync(long criterioId, MatrizRiesgoInactivarRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)\n    {",
    "método de reactivación en aplicación",
)
content = replace_once(
    content,
    "        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))\n            return ServiceResult.BadRequest(\"El motivo de eliminación del criterio es obligatorio.\");\n\n        try\n        {\n            var ok = await _repo.EliminarCriterioAsync(criterioId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);",
    "        if (dto == null || string.IsNullOrWhiteSpace(dto.Motivo))\n            return ServiceResult.BadRequest(\"El motivo de eliminación del criterio es obligatorio.\");\n\n        if (await _repo.CriterioTieneUsoHistoricoAsync(criterioId))\n            return ServiceResult.BadRequest(\"El criterio está relacionado con evaluaciones históricas y no puede eliminarse físicamente. Desactívelo para conservar la trazabilidad.\");\n\n        try\n        {\n            var ok = await _repo.EliminarCriterioAsync(criterioId, dto.Motivo.Trim(), usuarioId, usuarioEmail, ip);",
    "bloqueo preventivo por uso histórico",
)
write(app_service_path, content)

# Repository: mensajes, solapamiento, reactivación, histórico y variables obligatorias
content = read(repository_path)
content = content.replace(
    "Ya existe un criterio activo con la misma variable, escala y rango.",
    "Ya existe un criterio activo cuyo rango se superpone para la misma variable.",
)

reactivar_repo = '''

    public async Task<bool> ReactivarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var anterior = await ObtenerCriterioAuditoriaAsync(conn, tx, criterioId);
            if (anterior == null)
                return false;

            if (await ExisteSolapamientoParaReactivacionAsync(conn, tx, criterioId))
                throw new InvalidOperationException("El criterio no puede activarse porque su rango se superpone con otro criterio activo de la misma variable.");

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE RL_MR_CRITERIOS
                       SET MRC_ESTADO_REGISTRO = 1,
                           MRC_MOTIVO_INACTIVO = NULL
                     WHERE MRC_ID = :criterioId
                       AND MRC_ESTADO_REGISTRO = 0";
                cmd.Parameters.Add(Param("criterioId", criterioId));
                if (await cmd.ExecuteNonQueryAsync() == 0)
                    return false;
            }

            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_CRITERIOS", criterioId.ToString(), "UPDATE", anterior,
                JsonConvert.SerializeObject(new { Motivo = motivo.Trim(), EstadoRegistro = 1 }), usuarioId, usuarioEmail, ip);
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> CriterioTieneUsoHistoricoAsync(long criterioId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        return await CriterioTieneUsoHistoricoAsync(conn, null, criterioId);
    }
'''
content = replace_once(
    content,
    "\n    public async Task<bool> EliminarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip)\n    {",
    reactivar_repo + "\n    public async Task<bool> EliminarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip)\n    {",
    "repositorio de reactivación e histórico",
)
content = replace_once(
    content,
    "            var anterior = await ObtenerCriterioAuditoriaAsync(conn, tx, criterioId);\n            if (anterior == null)\n                return false;\n\n            await RegistrarAuditoriaAsync(\n                conn,\n                tx,\n                \"RL_MR_CRITERIOS\",",
    "            var anterior = await ObtenerCriterioAuditoriaAsync(conn, tx, criterioId);\n            if (anterior == null)\n                return false;\n\n            if (await CriterioTieneUsoHistoricoAsync(conn, tx, criterioId))\n                throw new InvalidOperationException(\"El criterio está relacionado con evaluaciones históricas y no puede eliminarse físicamente.\");\n\n            await RegistrarAuditoriaAsync(\n                conn,\n                tx,\n                \"RL_MR_CRITERIOS\",",
    "protección histórica transaccional",
)

old_duplicate_query = '''            SELECT COUNT(*)
              FROM RL_MR_CRITERIOS c
              JOIN RL_MR_VARIABLES v ON v.MRV_ID = c.MRC_VARIABLE_ID
              JOIN RL_MR_FACTORES f ON f.MRF_ID = v.MRV_FACTOR_ID
             WHERE f.MRF_MODELO_ID = :modeloId
               AND c.MRC_ESTADO_REGISTRO = 1
               AND c.MRC_VARIABLE_ID = :variableId
               AND NVL(c.MRC_ESCALA_ID, -1) = NVL(:escalaId, -1)
               AND NVL(c.MRC_VALOR_DESDE, -999999999) = NVL(:valorDesde, -999999999)
               AND NVL(c.MRC_VALOR_HASTA, 999999999) = NVL(:valorHasta, 999999999)
               AND (:criterioIdExcluir IS NULL OR c.MRC_ID <> :criterioIdExcluir)'''
new_overlap_query = '''            SELECT COUNT(*)
              FROM RL_MR_CRITERIOS c
              JOIN RL_MR_VARIABLES v ON v.MRV_ID = c.MRC_VARIABLE_ID
              JOIN RL_MR_FACTORES f ON f.MRF_ID = v.MRV_FACTOR_ID
             WHERE f.MRF_MODELO_ID = :modeloId
               AND c.MRC_ESTADO_REGISTRO = 1
               AND c.MRC_VARIABLE_ID = :variableId
               AND NVL(c.MRC_VALOR_DESDE, -999999999) <= NVL(:valorHasta, 999999999)
               AND NVL(c.MRC_VALOR_HASTA, 999999999) >= NVL(:valorDesde, -999999999)
               AND (:criterioIdExcluir IS NULL OR c.MRC_ID <> :criterioIdExcluir)'''
content = replace_once(content, old_duplicate_query, new_overlap_query, "consulta de rangos solapados")

helpers = '''

    private async Task<bool> ExisteSolapamientoParaReactivacionAsync(OracleConnection conn, OracleTransaction tx, long criterioId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT COUNT(*)
              FROM RL_MR_CRITERIOS objetivo
              JOIN RL_MR_CRITERIOS activo
                ON activo.MRC_VARIABLE_ID = objetivo.MRC_VARIABLE_ID
               AND activo.MRC_ESTADO_REGISTRO = 1
               AND activo.MRC_ID <> objetivo.MRC_ID
               AND NVL(activo.MRC_VALOR_DESDE, -999999999) <= NVL(objetivo.MRC_VALOR_HASTA, 999999999)
               AND NVL(activo.MRC_VALOR_HASTA, 999999999) >= NVL(objetivo.MRC_VALOR_DESDE, -999999999)
             WHERE objetivo.MRC_ID = :criterioId
               AND objetivo.MRC_ESTADO_REGISTRO = 0";
        cmd.Parameters.Add(Param("criterioId", criterioId));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }

    private async Task<bool> CriterioTieneUsoHistoricoAsync(OracleConnection conn, OracleTransaction? tx, long criterioId)
    {
        var token = $"\\\"CriterioId\\\":{criterioId}";
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT COUNT(*)
              FROM (
                    SELECT 1
                      FROM RL_MR_MATRICES
                     WHERE MRMAT_SNAPSHOT_METODO IS NOT NULL
                       AND DBMS_LOB.INSTR(MRMAT_SNAPSHOT_METODO, :token) > 0
                    UNION ALL
                    SELECT 1
                      FROM RL_MR_RESULTADOS
                     WHERE MRR_SNAPSHOT_CALCULO IS NOT NULL
                       AND DBMS_LOB.INSTR(MRR_SNAPSHOT_CALCULO, :token) > 0
                   )
             WHERE ROWNUM = 1";
        cmd.Parameters.Add(Param("token", token));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }
'''
content = replace_once(
    content,
    "\n    private async Task<bool> ExisteMatrizDuplicadaAsync(OracleConnection conn, OracleTransaction tx, MatrizRiesgoCrearRequestDto dto, long? matrizIdExcluir)\n    {",
    helpers + "\n    private async Task<bool> ExisteMatrizDuplicadaAsync(OracleConnection conn, OracleTransaction tx, MatrizRiesgoCrearRequestDto dto, long? matrizIdExcluir)\n    {",
    "auxiliares de reactivación e histórico",
)

mandatory_method = '''    private async Task ValidarVariablesPorTipoSujetoAsync(OracleConnection conn, OracleTransaction tx, long modeloId, MatrizRiesgoCrearRequestDto dto)
    {
        var detalles = dto.Detalles ?? new List<MatrizRiesgoDetalleRequestDto>();
        var variablesEnviadas = detalles.Select(d => d.VariableId).ToList();
        if (variablesEnviadas.Count != variablesEnviadas.Distinct().Count())
            throw new InvalidOperationException("No se permite registrar la misma variable más de una vez en una matriz.");

        var factorPermitido = FactorCodigoPorTipoSujeto(dto.SujetoTipo);
        var esInstitucional = dto.SujetoTipo.Equals("INSTITUCIONAL", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(factorPermitido) && !esInstitucional)
            return;

        foreach (var variableId in variablesEnviadas)
        {
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;
            cmd.Transaction = tx;
            cmd.CommandText = @"
                SELECT f.MRF_CODIGO
                  FROM RL_MR_VARIABLES v
                  JOIN RL_MR_FACTORES f ON f.MRF_ID = v.MRV_FACTOR_ID
                 WHERE v.MRV_ID = :variableId
                   AND f.MRF_MODELO_ID = :modeloId
                   AND v.MRV_ESTADO_REGISTRO = 1
                   AND f.MRF_ESTADO_REGISTRO = 1";
            cmd.Parameters.Add(Param("variableId", variableId));
            cmd.Parameters.Add(Param("modeloId", modeloId));
            var codigo = (await cmd.ExecuteScalarAsync())?.ToString();
            if (string.IsNullOrWhiteSpace(codigo))
                throw new InvalidOperationException($"La variable {variableId} no pertenece a la metodología vigente.");
            if (!esInstitucional && !string.Equals(codigo, factorPermitido, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"La variable {variableId} no corresponde al tipo de sujeto {dto.SujetoTipo}.");
        }

        var obligatorias = new List<long>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.BindByName = true;
            cmd.Transaction = tx;
            cmd.CommandText = @"
                SELECT v.MRV_ID
                  FROM RL_MR_VARIABLES v
                  JOIN RL_MR_FACTORES f ON f.MRF_ID = v.MRV_FACTOR_ID
                 WHERE f.MRF_MODELO_ID = :modeloId
                   AND f.MRF_ESTADO_REGISTRO = 1
                   AND v.MRV_ESTADO_REGISTRO = 1
                   AND v.MRV_OBLIGATORIA = 1
                   AND (:factorCodigo IS NULL OR f.MRF_CODIGO = :factorCodigo)
                 ORDER BY v.MRV_ID";
            cmd.Parameters.Add(Param("modeloId", modeloId));
            cmd.Parameters.Add(Param("factorCodigo", esInstitucional ? null : factorPermitido));
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                obligatorias.Add(ToLong(reader["MRV_ID"]));
        }

        var faltantes = obligatorias.Except(variablesEnviadas).ToList();
        if (faltantes.Count > 0)
            throw new InvalidOperationException($"Faltan variables obligatorias para {dto.SujetoTipo}: {string.Join(", ", faltantes)}.");
    }
'''
content = replace_regex(
    content,
    r"    private async Task ValidarVariablesPorTipoSujetoAsync\(OracleConnection conn, OracleTransaction tx, long modeloId, MatrizRiesgoCrearRequestDto dto\)\n    \{.*?\n    \}\n\n    private static string\? FactorCodigoPorTipoSujeto",
    mandatory_method + "\n    private static string? FactorCodigoPorTipoSujeto",
    "validación completa de variables obligatorias",
)
write(repository_path, content)

# Controller
content = read(controller_path)
reactivar_controller = '''

    [HttpPut("criterios/{criterioId:long}/reactivar")]
    [AuditRequired("Reactivacion de criterio de matriz de riesgos")]
    public async Task<IActionResult> ReactivarCriterio(long criterioId, [FromBody] MatrizRiesgoInactivarRequestDto dto)
    {
        try
        {
            var result = await _service.ReactivarCriterioAsync(criterioId, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar criterio de Matrices de Riesgos {CriterioId}", criterioId);
            return Error500(ex);
        }
    }
'''
content = replace_once(
    content,
    "\n    [HttpPut(\"criterios/{criterioId:long}/eliminar\")]",
    reactivar_controller + "\n    [HttpPut(\"criterios/{criterioId:long}/eliminar\")]",
    "endpoint de reactivación",
)
write(controller_path, content)

# Frontend service
content = read(frontend_service_path)
content = replace_once(
    content,
    "  eliminarCriterio(id: number, motivo: string): Observable<ApiMessage> {\n    return this.http.put<ApiMessage>(`${this.apiUrl}/criterios/${id}/eliminar`, { motivo }, this.confirmado);\n  }",
    "  reactivarCriterio(id: number, motivo: string): Observable<ApiMessage> {\n    return this.http.put<ApiMessage>(`${this.apiUrl}/criterios/${id}/reactivar`, { motivo }, this.confirmado);\n  }\n\n  eliminarCriterio(id: number, motivo: string): Observable<ApiMessage> {\n    return this.http.put<ApiMessage>(`${this.apiUrl}/criterios/${id}/eliminar`, { motivo }, this.confirmado);\n  }",
    "servicio frontend de reactivación",
)
write(frontend_service_path, content)

# Component TS
content = read(component_path)
content = replace_once(
    content,
    "type ModalTipo = 'estado' | 'eliminarMatriz' | 'inactivarCriterio' | 'eliminarCriterio' | 'estadoPlan' | 'inactivarPlan' | 'reactivarPlan' | 'inactivarEvidencia';",
    "type ModalTipo = 'estado' | 'eliminarMatriz' | 'inactivarCriterio' | 'reactivarCriterio' | 'eliminarCriterio' | 'estadoPlan' | 'inactivarPlan' | 'reactivarPlan' | 'inactivarEvidencia';",
    "modal de reactivación de criterio",
)
content = replace_once(
    content,
    "        const verInactivos = this.incluirCriteriosInactivos();\n        this.criterios.set(verInactivos ? datos.filter(c => !c.activo) : datos.filter(c => c.activo));",
    "        const verInactivos = this.incluirCriteriosInactivos();\n        this.criterios.set(verInactivos ? datos : datos.filter(c => c.activo));",
    "listado que incluye inactivos",
)
content = replace_once(
    content,
    "      case 'inactivarCriterio':\n        this.ejecutarInactivacionCriterio(operacion.criterio!, motivo);\n        break;\n      case 'eliminarCriterio':",
    "      case 'inactivarCriterio':\n        this.ejecutarInactivacionCriterio(operacion.criterio!, motivo);\n        break;\n      case 'reactivarCriterio':\n        this.ejecutarReactivacionCriterio(operacion.criterio!, motivo);\n        break;\n      case 'eliminarCriterio':",
    "switch de reactivación",
)
content = replace_once(
    content,
    "    this.guardando.set(true);\n    const dto: MatrizRiesgoCriterioRequest = {",
    "    const solapamiento = this.validarSolapamientoCriterio();\n    if (solapamiento) {\n      this.error.set(solapamiento);\n      return;\n    }\n\n    this.guardando.set(true);\n    const dto: MatrizRiesgoCriterioRequest = {",
    "validación preventiva de solapamiento",
)
reactivar_component = '''

  reactivarCriterio(criterio: MatrizRiesgoCriterio): void {
    this.abrirModal({
      tipo: 'reactivarCriterio',
      titulo: 'Activar criterio',
      descripcion: `Ingrese el motivo obligatorio para activar el criterio ${criterio.criterioId}. Se validará que su rango no se superponga con criterios activos.`,
      textoConfirmar: 'Activar',
      requiereMotivo: true,
      criterio,
      tono: 'normal'
    });
  }
'''
content = replace_once(
    content,
    "\n  eliminarCriterio(criterio: MatrizRiesgoCriterio): void {",
    reactivar_component + "\n  eliminarCriterio(criterio: MatrizRiesgoCriterio): void {",
    "acción de reactivación frontend",
)
executor = '''

  private ejecutarReactivacionCriterio(criterio: MatrizRiesgoCriterio, motivo: string): void {
    this.guardando.set(true);
    this.service.reactivarCriterio(criterio.criterioId, motivo).subscribe({
      next: () => {
        this.mensaje.set('Criterio activado correctamente.');
        this.cargarCriterios();
        this.guardando.set(false);
        this.cerrarModal();
      },
      error: err => this.finalizarAccionConError(err, 'No se pudo activar el criterio.')
    });
  }
'''
content = replace_once(
    content,
    "\n  private ejecutarEliminacionCriterio(criterio: MatrizRiesgoCriterio, motivo: string): void {",
    executor + "\n  private ejecutarEliminacionCriterio(criterio: MatrizRiesgoCriterio, motivo: string): void {",
    "ejecutor de reactivación frontend",
)
overlap_front = '''

  private validarSolapamientoCriterio(): string | null {
    const variableId = Number(this.criteriosForm.variableId);
    if (!variableId) return null;

    const desdeNuevo = this.criteriosForm.valorDesde ?? Number.NEGATIVE_INFINITY;
    const hastaNuevo = this.criteriosForm.valorHasta ?? Number.POSITIVE_INFINITY;
    const editandoId = this.criterioEditandoId();
    const conflicto = this.criterios().find(criterio => {
      if (!criterio.activo || criterio.variableId !== variableId || criterio.criterioId === editandoId) return false;
      const desdeExistente = criterio.valorDesde ?? Number.NEGATIVE_INFINITY;
      const hastaExistente = criterio.valorHasta ?? Number.POSITIVE_INFINITY;
      return desdeExistente <= hastaNuevo && hastaExistente >= desdeNuevo;
    });

    return conflicto
      ? `El rango se superpone con el criterio activo ${conflicto.criterioId} (${conflicto.valorDesde ?? '-∞'} a ${conflicto.valorHasta ?? '∞'}).`
      : null;
  }
'''
content = replace_once(
    content,
    "\n  private prepararCapturaVariables(): void {",
    overlap_front + "\n  private prepararCapturaVariables(): void {",
    "validador frontend de solapamiento",
)
write(component_path, content)

# Template criteria actions
content = read(template_path)
content = replace_once(content, "            Ver inactivos", "            Incluir inactivos", "etiqueta de inclusión")
content = replace_once(
    content,
    "                      <button type=\"button\" (click)=\"editarCriterio(criterio)\"\n                        class=\"px-2.5 py-1.5 rounded-lg border border-gray-200 text-xs font-semibold hover:bg-gray-50\">\n                        Editar\n                      </button>\n                      <button type=\"button\" (click)=\"inactivarCriterio(criterio)\" [disabled]=\"!criterio.activo || guardando()\"\n                        class=\"px-2.5 py-1.5 rounded-lg bg-red-600 text-white text-xs font-semibold disabled:opacity-50\">\n                        Desactivar\n                      </button>\n                      <button type=\"button\" (click)=\"eliminarCriterio(criterio)\" [disabled]=\"guardando()\"\n                        class=\"px-2.5 py-1.5 rounded-lg border border-red-200 text-red-700 text-xs font-semibold hover:bg-red-50 disabled:opacity-50\">\n                        Eliminar\n                      </button>",
    "                      <button type=\"button\" (click)=\"editarCriterio(criterio)\" [disabled]=\"!criterio.activo || guardando()\"\n                        title=\"Los criterios inactivos deben activarse antes de editarlos\"\n                        class=\"px-2.5 py-1.5 rounded-lg border border-gray-200 text-xs font-semibold hover:bg-gray-50 disabled:opacity-50\">\n                        Editar\n                      </button>\n                      <button *ngIf=\"criterio.activo\" type=\"button\" (click)=\"inactivarCriterio(criterio)\" [disabled]=\"guardando()\"\n                        class=\"px-2.5 py-1.5 rounded-lg bg-red-600 text-white text-xs font-semibold disabled:opacity-50\">\n                        Desactivar\n                      </button>\n                      <button *ngIf=\"!criterio.activo\" type=\"button\" (click)=\"reactivarCriterio(criterio)\" [disabled]=\"guardando()\"\n                        class=\"px-2.5 py-1.5 rounded-lg bg-emerald-600 text-white text-xs font-semibold disabled:opacity-50\">\n                        Activar\n                      </button>\n                      <button type=\"button\" (click)=\"eliminarCriterio(criterio)\" [disabled]=\"guardando() || criterio.activo\"\n                        title=\"Primero desactive el criterio. La eliminación solo procede si no tiene uso histórico.\"\n                        class=\"px-2.5 py-1.5 rounded-lg border border-red-200 text-red-700 text-xs font-semibold hover:bg-red-50 disabled:opacity-50\">\n                        Eliminar\n                      </button>",
    "acciones administrables de criterios",
)
write(template_path, content)

# Backend tests
content = read(backend_tests_path)
new_backend_tests = '''

    [Fact]
    public async Task ReactivarCriterio_MotivoValido_RecortaYDelega()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.ReactivarCriterioAsync), _ => Task.FromResult(true));

        var result = await service.ReactivarCriterioAsync(35, new MatrizRiesgoInactivarRequestDto
        {
            Motivo = " Reactivación autorizada "
        }, 7, null, null);

        Assert.True(result.Success);
        var call = Assert.Single(repo.CallsTo(nameof(IMatricesRiesgosRepository.ReactivarCriterioAsync)));
        Assert.Equal("Reactivación autorizada", call.Arguments[1]);
    }

    [Fact]
    public async Task EliminarCriterio_ConUsoHistorico_RechazaSinEliminar()
    {
        var service = CrearServicio(out var repo, out _);
        repo.On(nameof(IMatricesRiesgosRepository.CriterioTieneUsoHistoricoAsync), _ => Task.FromResult(true));

        var result = await service.EliminarCriterioAsync(35, new MatrizRiesgoInactivarRequestDto
        {
            Motivo = "Depuración de catálogo"
        }, 7, null, null);

        Assert.False(result.Success);
        Assert.Contains("históric", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repo.CallsTo(nameof(IMatricesRiesgosRepository.EliminarCriterioAsync)));
    }
'''
content = replace_once(
    content,
    "\n    private static MatricesRiesgosAppService CrearServicio(out InterfaceStub repoStub, out InterfaceStub motorStub, IConfiguration? configuration = null)",
    new_backend_tests + "\n    private static MatricesRiesgosAppService CrearServicio(out InterfaceStub repoStub, out InterfaceStub motorStub, IConfiguration? configuration = null)",
    "pruebas backend 12.2",
)
write(backend_tests_path, content)

# Frontend service test
content = read(service_tests_path)
new_service_test = '''

  it('reactiva un criterio con confirmacion y motivo', () => {
    service.reactivarCriterio(9, 'Rango nuevamente vigente').subscribe();

    const request = http.expectOne(`${apiUrl}/criterios/9/reactivar`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ motivo: 'Rango nuevamente vigente' });
    expect(request.request.headers.get(CONFIRMACION_CAMBIOS_HEADER)).toBe('1');
    request.flush({ success: true });
  });
'''
content = replace_once(
    content,
    "\n  it('propaga errores HTTP del listado al coordinador de la pantalla', () => {",
    new_service_test + "\n  it('propaga errores HTTP del listado al coordinador de la pantalla', () => {",
    "prueba de servicio de reactivación",
)
write(service_tests_path, content)

# Frontend component tests
content = read(component_tests_path)
content = replace_once(
    content,
    "      inactivarCriterio: vi.fn(() => of({ success: true })),\n      eliminarCriterio: vi.fn(() => of({ success: true })),",
    "      inactivarCriterio: vi.fn(() => of({ success: true })),\n      reactivarCriterio: vi.fn(() => of({ success: true })),\n      eliminarCriterio: vi.fn(() => of({ success: true })),",
    "mock de reactivación",
)
content = replace_once(
    content,
    "  it('ver inactivos muestra solo criterios desactivados', () => {",
    "  it('incluir inactivos conserva criterios activos e inactivos', () => {",
    "nombre de prueba de listado",
)
content = replace_once(
    content,
    "    expect(component.criterios()).toEqual([{ criterioId: 2, activo: false }]);",
    "    expect(component.criterios()).toEqual(criterios);",
    "expectativa de inclusión de inactivos",
)
new_component_tests = '''

  it('bloquea un criterio cuando el rango se superpone con otro activo', () => {
    component.criterios.set([{ criterioId: 4, variableId: 2, activo: true, valorDesde: 10, valorHasta: 20 }] as never);
    component.criteriosForm = {
      variableId: 2,
      escalaId: null,
      valorDesde: 15,
      valorHasta: 25,
      puntaje: 4,
      descripcion: 'Rango solapado'
    };

    component.guardarCriterio();

    expect(component.error()).toContain('se superpone');
    expect(service['crearCriterio']).not.toHaveBeenCalled();
  });

  it('reactiva un criterio inactivo con motivo', () => {
    const criterio = { criterioId: 9, activo: false } as never;
    component.reactivarCriterio(criterio);
    component.actualizarModalMotivo('Rango nuevamente vigente');

    component.confirmarModal();

    expect(service['reactivarCriterio']).toHaveBeenCalledWith(9, 'Rango nuevamente vigente');
    expect(component.mensaje()).toBe('Criterio activado correctamente.');
    expect(component.modalOperacion()).toBeNull();
  });
'''
content = replace_once(
    content,
    "\n  it('conserva un error controlado y detiene la carga si falla el reporte', () => {",
    new_component_tests + "\n  it('conserva un error controlado y detiene la carga si falla el reporte', () => {",
    "pruebas componente criterios 12.2",
)
write(component_tests_path, content)

# Evidence baseline
base = ROOT / "docs/3. Módulo Matrices de Riesgos/Fase 12 - Mejora ejecutiva UXUI y mapa de calor/Evidencia_Fase_12_2"
base.mkdir(parents=True, exist_ok=True)
(base / "fase12_2_alcance_implementado.json").write_text(
    '''{
  "fase": "12.2 - Captura y criterios",
  "rama": "fase-12-mejora-ejecutiva-matrices",
  "estado": "Implementación preparada para validación automatizada",
  "controles": [
    "Variables obligatorias y no repetidas por tipo de sujeto.",
    "Institucional evalúa variables globales obligatorias.",
    "Rangos activos de criterios no pueden solaparse.",
    "Criterios inactivos pueden reactivarse con motivo y auditoría.",
    "Eliminación física bloqueada cuando existe uso histórico.",
    "Recálculo automático permanece en backend."
  ],
  "cambios_bd": false,
  "restricciones": [
    "Sin DNP.",
    "Sin CONTROL_ALMACEN.PROVEEDOR.",
    "Sin integración con Monitoreo de Listas.",
    "Sin cálculo de riesgo en frontend."
  ]
}
''',
    encoding="utf-8",
)

print("Integración de Fase 12.2 aplicada correctamente.")
