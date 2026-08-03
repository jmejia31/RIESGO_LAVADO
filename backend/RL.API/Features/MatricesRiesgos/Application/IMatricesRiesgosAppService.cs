using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IMatricesRiesgosAppService
{
    // ============================================================
    // 1. GESTIÓN DEL CICLO DE VIDA DEL FORMULARIO Y VERSIONES
    // ============================================================
    Task<ServiceResult<VersionFormularioDto>> ObtenerVersionVigenteFormularioAsync(string familiaCodigo);
    Task<ServiceResult<VersionFormularioDto>> ObtenerVersionFormularioAsync(long versionId);
    Task<ServiceResult<long>> CrearBorradorFormularioAsync(long familiaId, string codigoFormulario, string jsonConfig, long usuarioId);
    Task<ServiceResult<long>> ClonarVersionFormularioAsync(long versionOrigenId, long usuarioId);
    Task<ServiceResult> ActualizarBorradorFormularioAsync(long versionId, string jsonConfig, long usuarioId);
    Task<ServiceResult> PublicarVersionFormularioAsync(long versionId, long usuarioId);
    Task<ServiceResult> CambiarEstadoVigenciaFormularioAsync(long versionId, bool vigente, long usuarioId);
    Task<ServiceResult<List<VersionFormularioDto>>> ListarHistorialVersionesFormularioAsync(string familiaCodigo);

    // ============================================================
    // 2. GESTIÓN DE EVALUACIONES E HISTORIAL DE CAMBIOS
    // ============================================================
    Task<ServiceResult<EvaluacionRiesgoDto>> ObtenerEvaluacionAsync(long evaId);
    Task<ServiceResult<List<EvaluacionRiesgoDto>>> ListarEvaluacionesPaginadasAsync(ConsultaEvaluacionPaginadaDto filtro);
    Task<ServiceResult<long>> CrearEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip);
    Task<ServiceResult> ActualizarEvaluacionAsync(EvaluacionRiesgoDto dto, long usuarioId, string? ip);
    Task<ServiceResult> TransicionarEstadoEvaluacionAsync(long evaId, string nuevoEstado, string? motivo, long usuarioId, string? ip);
    Task<ServiceResult<List<RevisionEvaluacionDto>>> ObtenerRevisionesEvaluacionAsync(long evaId);

    // ============================================================
    // 3. ARCHIVO FÍSICO CENTRAL DE EVIDENCIAS Y SUS VINCULACIONES
    // ============================================================
    Task<ServiceResult<EvidenciaDto>> CargarArchivoEvidenciaFisicaAsync(IFormFile archivo, long usuarioId);
    Task<ServiceResult<EvidenciaDto>> ObtenerEvidenciaFisicaAsync(long evidenciaId);

    Task<ServiceResult> VincularEvidenciaRiesgoAsync(AsociarEvidenciaRiesgoDto dto, long usuarioId, string? ip);
    Task<ServiceResult> VincularEvidenciaEvaluacionAsync(AsociarEvidenciaEvaluacionDto dto, long usuarioId, string? ip);
    Task<ServiceResult> VincularEvidenciaControlAsync(AsociarEvidenciaControlDto dto, long usuarioId, string? ip);
    Task<ServiceResult> VincularEvidenciaPlanAsync(AsociarEvidenciaPlanDto dto, long usuarioId, string? ip);
    Task<ServiceResult> VincularEvidenciaActividadAsync(AsociarEvidenciaActividadDto dto, long usuarioId, string? ip);
    Task<ServiceResult> VincularEvidenciaAlertaAsync(AsociarEvidenciaAlertaDto dto, long usuarioId, string? ip);
    Task<ServiceResult> VincularEvidenciaAutomonitoreoAsync(AsociarEvidenciaAutomonitoreoDto dto, long usuarioId, string? ip);
    Task<ServiceResult> VincularEvidenciaRevisionAsync(AsociarEvidenciaRevisionDto dto, long usuarioId, string? ip);
    Task<ServiceResult> VincularEvidenciaAprobacionAsync(AsociarEvidenciaAprobacionDto dto, long usuarioId, string? ip);
    Task<ServiceResult> EliminarEvidenciaAsync(long evidenciaId, long usuarioId, string? ip);

    // ============================================================
    // 4. MIGRACIÓN CONTROLADA DE REPORTES A DTOs TIPADOS
    // ============================================================
    [Obsolete("Contrato transitorio de Fase 1.3. Migrar consumidores a ObtenerConsolidadoTipadoAsync y eliminar antes del cierre.")]
    Task<ServiceResult<List<Dictionary<string, object>>>> ObtenerConsolidadoMatricesAsync();

    async Task<ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>> ObtenerConsolidadoTipadoAsync()
    {
#pragma warning disable CS0618
        ServiceResult<List<Dictionary<string, object>>> legado = await ObtenerConsolidadoMatricesAsync();
#pragma warning restore CS0618
        if (!legado.Success || legado.Data is null)
        {
            return new ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>(
                false,
                null,
                legado.Message,
                legado.StatusCode);
        }

        var resultado = new List<RiesgoReporteFilaDto>(legado.Data.Count);
        foreach (Dictionary<string, object> fila in legado.Data)
        {
            resultado.Add(new RiesgoReporteFilaDto
            {
                EvaluacionId = Convert.ToInt64(fila["EvaluacionId"]),
                CodigoRiesgo = Convert.ToString(fila["CodigoRiesgo"]) ?? string.Empty,
                EstadoEvaluacion = Convert.ToString(fila["Estado"]) ?? string.Empty,
                Vri = Convert.ToInt32(fila["Vri"]),
                Vrr = Convert.ToInt32(fila["Vrr"]),
                NivelInherente = Convert.ToString(fila["NivelInherente"]) ?? string.Empty,
                NivelResidual = Convert.ToString(fila["NivelResidual"]) ?? string.Empty,
                RespuestaRiesgo = Convert.ToString(fila["RespuestaRiesgo"]) ?? string.Empty,
                AreaPrincipal = Convert.ToString(fila["Area"]) ?? string.Empty,
                DuenoRiesgo = Convert.ToString(fila["Dueno"]) ?? string.Empty,
                FechaEvaluacion = Convert.ToDateTime(fila["Fecha"])
            });
        }

        return ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>>.Ok(resultado);
    }

    // ============================================================
    // 5. MIGRACIÓN CONTROLADA DE METODOLOGÍA A CONTRATO DINÁMICO
    // ============================================================
    [Obsolete("Contrato transitorio de Fase 1.3. Migrar consumidores a ObtenerMetodologiaDinamicaVigenteAsync y eliminar antes del cierre.")]
    Task<ServiceResult<MetodologiaMatricesDto>> ObtenerMetodologiaVigenteAsync();

    async Task<ServiceResult<MetodologiaFormularioDto>> ObtenerMetodologiaDinamicaVigenteAsync()
    {
#pragma warning disable CS0618
        ServiceResult<MetodologiaMatricesDto> legado = await ObtenerMetodologiaVigenteAsync();
#pragma warning restore CS0618
        if (!legado.Success || legado.Data is null)
        {
            return new ServiceResult<MetodologiaFormularioDto>(
                false,
                null,
                legado.Message,
                legado.StatusCode);
        }

        return ServiceResult<MetodologiaFormularioDto>.Ok(new MetodologiaFormularioDto
        {
            Codigo = legado.Data.Version,
            Version = 0,
            Secciones = Array.Empty<SeccionFormularioDto>(),
            Catalogos = Array.Empty<CatalogoMatricesDto>(),
            Reglas = Array.Empty<ReglaCalculoMatricesDto>()
        });
    }
}
