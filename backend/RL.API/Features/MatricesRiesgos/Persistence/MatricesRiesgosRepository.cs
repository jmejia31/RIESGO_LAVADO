using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Infrastructure.Database;
using System.Data;
using System.Globalization;
using System.Text;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public sealed class MatricesRiesgosRepository : IMatricesRiesgosRepository
{
    private const string ModuloAuditoria = "MatricesRiesgos";
    private readonly OracleDbContext _db;

    public MatricesRiesgosRepository(OracleDbContext db)
    {
        _db = db;
    }

    public async Task<MetodologiaCalculoDto?> ObtenerMetodologiaVigenteAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var modelo = await ObtenerModeloVigenteAsync(conn, null);
        if (modelo == null)
            return null;

        return await ConstruirMetodologiaAsync(conn, null, modelo.Value.ModeloId, modelo.Value.Version);
    }

    public async Task<MatrizRiesgoDetalleDto?> ObtenerMatrizAsync(long matrizId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var matriz = await ObtenerMatrizBaseAsync(conn, null, matrizId);
        if (matriz == null)
            return null;

        matriz.Detalles = await ObtenerDetallesMatrizAsync(conn, null, matrizId, matriz.ModeloId);
        matriz.Controles = await ObtenerControlesMatrizAsync(conn, null, matrizId);
        matriz.Resultados = await ObtenerResultadosMatrizAsync(conn, null, matrizId);
        matriz.PlanesAccion = await ObtenerPlanesMatrizAsync(conn, null, matrizId);
        matriz.Evidencias = await ObtenerEvidenciasMatrizAsync(conn, null, matrizId);
        return matriz;
    }

    public async Task<List<MatrizRiesgoResumenDto>> ListarMatricesAsync(MatrizRiesgoFiltroDto filtro)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        var where = new List<string> { "m.MRMAT_ESTADO_REGISTRO = 1" };
        var parameters = new List<OracleParameter>();

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            if (filtro.Estado.Trim().Equals("EN_REVISION", StringComparison.OrdinalIgnoreCase))
            {
                // CALCULADA queda como compatibilidad histórica; para operación diaria se presenta como En Revisión.
                where.Add("m.MRMAT_ESTADO IN ('EN_REVISION', 'CALCULADA')");
            }
            else
            {
                where.Add("m.MRMAT_ESTADO = :estado");
                parameters.Add(new OracleParameter("estado", filtro.Estado.Trim().ToUpperInvariant()));
            }
        }

        if (!string.IsNullOrWhiteSpace(filtro.SujetoTipo))
        {
            where.Add("m.MRMAT_SUJETO_TIPO = :sujetoTipo");
            parameters.Add(new OracleParameter("sujetoTipo", filtro.SujetoTipo.Trim().ToUpperInvariant()));
        }

        if (filtro.FechaInicio.HasValue)
        {
            where.Add("m.MRMAT_FECHA_EVALUACION >= :fechaInicio");
            parameters.Add(new OracleParameter("fechaInicio", filtro.FechaInicio.Value.Date));
        }

        if (filtro.FechaFin.HasValue)
        {
            where.Add("m.MRMAT_FECHA_EVALUACION <= :fechaFin");
            parameters.Add(new OracleParameter("fechaFin", filtro.FechaFin.Value.Date.AddDays(1).AddSeconds(-1)));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            where.Add(@"(
                LOWER(m.MRMAT_NOMBRE_SUJETO) LIKE :buscar
                OR LOWER(NVL(m.MRMAT_DOCUMENTO, '')) LIKE :buscar
                OR LOWER(NVL(m.MRMAT_SUJETO_ID_EXT, '')) LIKE :buscar
                OR LOWER(NVL(m.MRMAT_ESTADO, '')) LIKE :buscar
                OR LOWER(NVL(m.MRMAT_SUJETO_TIPO, '')) LIKE :buscar
                OR LOWER(NVL(mo.MRM_VERSION, '')) LIKE :buscar
                OR LOWER(NVL(r.MRR_NIVEL_INHERENTE, '')) LIKE :buscar
                OR LOWER(NVL(r.MRR_NIVEL_RESIDUAL, '')) LIKE :buscar
            )");
            parameters.Add(new OracleParameter("buscar", $"%{filtro.Buscar.Trim().ToLowerInvariant()}%"));
        }

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT m.MRMAT_ID, m.MRMAT_MODELO_ID, mo.MRM_VERSION, m.MRMAT_SUJETO_TIPO,
                   m.MRMAT_SUJETO_ID_EXT, m.MRMAT_DOCUMENTO, m.MRMAT_NOMBRE_SUJETO,
                   m.MRMAT_ESTADO, m.MRMAT_FECHA_EVALUACION,
                   r.MRR_PUNTAJE_INHERENTE, r.MRR_NIVEL_INHERENTE,
                   r.MRR_PUNTAJE_RESIDUAL, r.MRR_NIVEL_RESIDUAL, r.MRR_REQUIERE_PLAN
              FROM RL_MR_MATRICES m
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              LEFT JOIN RL_MR_RESULTADOS r
                ON r.MRR_MATRIZ_ID = m.MRMAT_ID
               AND r.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
               AND r.MRR_ES_VIGENTE = 1
             WHERE {string.Join(" AND ", where)}
             ORDER BY m.MRMAT_FECHA_EVALUACION DESC, m.MRMAT_ID DESC";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        var result = new List<MatrizRiesgoResumenDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(MapResumen(reader));

        return result;
    }

    public async Task<MatricesRiesgoDashboardDto> ObtenerDashboardAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        var dashboard = new MatricesRiesgoDashboardDto();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT COUNT(DISTINCT m.MRMAT_ID) TOTAL,
                       COUNT(DISTINCT CASE WHEN r.MRR_ID IS NOT NULL THEN m.MRMAT_ID END) CALCULADAS,
                       COUNT(DISTINCT CASE WHEN m.MRMAT_ESTADO = 'CERRADA' THEN m.MRMAT_ID END) CERRADAS
                  FROM RL_MR_MATRICES m
                  LEFT JOIN RL_MR_RESULTADOS r
                    ON r.MRR_MATRIZ_ID = m.MRMAT_ID
                   AND r.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
                   AND r.MRR_ES_VIGENTE = 1
                 WHERE m.MRMAT_ESTADO_REGISTRO = 1";

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                dashboard.TotalMatrices = ToInt(reader["TOTAL"]);
                dashboard.TotalCalculadas = ToInt(reader["CALCULADAS"]);
                dashboard.TotalCerradas = ToInt(reader["CERRADAS"]);
            }
        }

        dashboard.PorEstado = await ObtenerConteosAsync(conn, "SELECT CASE WHEN MRMAT_ESTADO = 'CALCULADA' THEN 'EN_REVISION' ELSE MRMAT_ESTADO END NOMBRE, COUNT(*) TOTAL FROM RL_MR_MATRICES WHERE MRMAT_ESTADO_REGISTRO = 1 GROUP BY CASE WHEN MRMAT_ESTADO = 'CALCULADA' THEN 'EN_REVISION' ELSE MRMAT_ESTADO END ORDER BY NOMBRE");
        dashboard.PorNivelResidual = await ObtenerConteosAsync(conn, @"
            SELECT NVL(MRR_NIVEL_RESIDUAL, 'SIN_CALCULO') NOMBRE, COUNT(*) TOTAL
              FROM RL_MR_MATRICES m
              LEFT JOIN RL_MR_RESULTADOS r
                ON r.MRR_MATRIZ_ID = m.MRMAT_ID
               AND r.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
               AND r.MRR_ES_VIGENTE = 1
             WHERE m.MRMAT_ESTADO_REGISTRO = 1
             GROUP BY NVL(MRR_NIVEL_RESIDUAL, 'SIN_CALCULO')
             ORDER BY NOMBRE");
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT COUNT(*)
                  FROM RL_MR_MATRICES m
                  JOIN RL_MR_RESULTADOS r
                    ON r.MRR_MATRIZ_ID = m.MRMAT_ID
                   AND r.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
                   AND r.MRR_ES_VIGENTE = 1
                 WHERE m.MRMAT_ESTADO_REGISTRO = 1
                   AND r.MRR_REQUIERE_PLAN = 1";
            dashboard.TotalConPlanAccion = ToInt(await cmd.ExecuteScalarAsync());
        }

        return dashboard;
    }

    public async Task<MatricesRiesgoReporteDto> ObtenerReporteAsync(MatrizRiesgoReporteFiltroDto filtro)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        filtro ??= new MatrizRiesgoReporteFiltroDto();
        var reporte = new MatricesRiesgoReporteDto
        {
            FechaGeneracion = DateTime.Now,
            Filtro = filtro
        };

        reporte.Totales = await ObtenerTotalesReporteAsync(conn, filtro);
        reporte.PorEstado = await ObtenerConteosReporteAsync(conn, filtro, "CASE WHEN m.MRMAT_ESTADO = 'CALCULADA' THEN 'EN_REVISION' ELSE m.MRMAT_ESTADO END");
        reporte.PorNivelResidual = await ObtenerConteosReporteAsync(conn, filtro, "NVL(ri.MRR_NIVEL_RESIDUAL, 'SIN_CALCULO')");
        reporte.PorSujetoTipo = await ObtenerConteosReporteAsync(conn, filtro, "m.MRMAT_SUJETO_TIPO");
        reporte.PorFactor = await ObtenerFactoresReporteAsync(conn, filtro);
        reporte.MapaInherente = await ObtenerMapaNivelReporteAsync(conn, filtro, "INHERENTE");
        reporte.MapaResidual = await ObtenerMapaNivelReporteAsync(conn, filtro, "RESIDUAL");
        reporte.MatricesFiltradas = await ObtenerMatricesFiltradasReporteAsync(conn, filtro);
        reporte.MatricesCriticas = await ObtenerMatricesCriticasReporteAsync(conn, filtro);
        reporte.PlanesAccion = await ObtenerPlanesAccionReporteAsync(conn, filtro);

        return reporte;
    }

    public async Task RegistrarExportacionReporteAsync(MatrizRiesgoReporteFiltroDto filtro, string formato, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var datos = JsonConvert.SerializeObject(new
            {
                Accion = "EXPORTACION_REPORTE_MATRICES_RIESGOS",
                Formato = formato.Trim().ToUpperInvariant(),
                Fecha = DateTime.Now,
                Filtros = filtro
            });

            await RegistrarHistorialAsync(conn, tx, null, "RL_MR_RESULTADOS", "REPORTE_MATRICES", "EXPORTACION_REPORTE", null, null, $"Exportación {formato} de reportería de matrices de riesgos.", null, datos, usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_RESULTADOS", "REPORTE_MATRICES", "VER", null, datos, usuarioId, usuarioEmail, ip);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<long> CrearMatrizAsync(MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            // La matriz queda amarrada al modelo vigente y conserva snapshot de metodología
            // para que cambios futuros de criterios no alteren evaluaciones históricas.
            var modelo = await ObtenerModeloVigenteAsync(conn, tx) ?? throw new InvalidOperationException("No existe una metodología aprobada vigente para Matrices de Riesgos.");
            var metodologia = await ConstruirMetodologiaAsync(conn, tx, modelo.ModeloId, modelo.Version);
            if (await ExisteMatrizDuplicadaAsync(conn, tx, dto, null))
                throw new InvalidOperationException("Ya existe una matriz activa con igual identificador externo o documento.");

            await ValidarVariablesPorTipoSujetoAsync(conn, tx, modelo.ModeloId, dto);

            var matrizId = await NextValAsync(conn, tx, "SEQ_RL_MR_MATRICES");
            var snapshotMetodo = JsonConvert.SerializeObject(metodologia);

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO RL_MR_MATRICES (
                        MRMAT_ID, MRMAT_MODELO_ID, MRMAT_SUJETO_TIPO, MRMAT_SUJETO_ID_EXT,
                        MRMAT_DOCUMENTO, MRMAT_NOMBRE_SUJETO, MRMAT_ORIGEN_DATOS,
                        MRMAT_ESTADO, MRMAT_FECHA_EVALUACION, MRMAT_SNAPSHOT_METODO,
                        MRMAT_USR_CREACION_ID, MRMAT_FECHA_CREACION, MRMAT_ESTADO_REGISTRO
                    ) VALUES (
                        :id, :modeloId, :sujetoTipo, :sujetoIdExt,
                        :documento, :nombreSujeto, :origenDatos,
                        'EN_REVISION', SYSDATE, :snapshotMetodo,
                        :usuarioId, SYSDATE, 1
                    )";
                cmd.Parameters.Add(Param("id", matrizId));
                cmd.Parameters.Add(Param("modeloId", modelo.ModeloId));
                cmd.Parameters.Add(Param("sujetoTipo", dto.SujetoTipo.Trim().ToUpperInvariant()));
                cmd.Parameters.Add(Param("sujetoIdExt", dto.SujetoIdExt));
                cmd.Parameters.Add(Param("documento", dto.Documento));
                cmd.Parameters.Add(Param("nombreSujeto", dto.NombreSujeto.Trim()));
                cmd.Parameters.Add(Param("origenDatos", string.IsNullOrWhiteSpace(dto.OrigenDatos) ? "CAPTURA" : dto.OrigenDatos.Trim().ToUpperInvariant()));
                cmd.Parameters.Add(ClobParam("snapshotMetodo", snapshotMetodo));
                cmd.Parameters.Add(Param("usuarioId", usuarioId));
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var detalle in dto.Detalles)
                await InsertarDetalleAsync(conn, tx, matrizId, modelo.ModeloId, detalle, usuarioId);

            foreach (var control in dto.Controles)
                await InsertarControlAsync(conn, tx, matrizId, modelo.ModeloId, control, usuarioId);

            await RegistrarHistorialAsync(conn, tx, matrizId, "RL_MR_MATRICES", matrizId.ToString(), "CREACION", null, "EN_REVISION", "Creación de matriz de riesgos.", null, JsonConvert.SerializeObject(dto), usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_MATRICES", matrizId.ToString(), "INSERT", null, JsonConvert.SerializeObject(dto), usuarioId, usuarioEmail, ip);

            tx.Commit();
            return matrizId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> ActualizarMatrizAsync(long matrizId, MatrizRiesgoCrearRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var matriz = await ObtenerMatrizBaseAsync(conn, tx, matrizId);
            if (matriz == null)
                return false;

            // Las matrices cerradas o retiradas se protegen contra cambios retroactivos.
            if (matriz.Estado is "CERRADA" or "INACTIVA")
                throw new InvalidOperationException("La matriz cerrada o inactiva no puede editarse.");

            var modelo = await ObtenerModeloVigenteAsync(conn, tx) ?? throw new InvalidOperationException("No existe una metodología aprobada vigente para Matrices de Riesgos.");

            await ValidarVariablesPorTipoSujetoAsync(conn, tx, modelo.ModeloId, dto);

            var datosAnteriores = JsonConvert.SerializeObject(matriz);
            var estadoNuevo = "EN_REVISION";

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE RL_MR_MATRICES
                       SET MRMAT_SUJETO_TIPO = :sujetoTipo,
                           MRMAT_SUJETO_ID_EXT = :sujetoIdExt,
                           MRMAT_DOCUMENTO = :documento,
                           MRMAT_NOMBRE_SUJETO = :nombreSujeto,
                           MRMAT_ORIGEN_DATOS = :origenDatos,
                           MRMAT_ESTADO = :estadoNuevo,
                           MRMAT_MOTIVO_ESTADO = :motivo,
                           MRMAT_USR_MODIF_ID = :usuarioId,
                           MRMAT_FECHA_MODIF = SYSDATE
                     WHERE MRMAT_ID = :matrizId";
                cmd.Parameters.Add(Param("sujetoTipo", dto.SujetoTipo.Trim().ToUpperInvariant()));
                cmd.Parameters.Add(Param("sujetoIdExt", dto.SujetoIdExt));
                cmd.Parameters.Add(Param("documento", dto.Documento));
                cmd.Parameters.Add(Param("nombreSujeto", dto.NombreSujeto.Trim()));
                cmd.Parameters.Add(Param("origenDatos", string.IsNullOrWhiteSpace(dto.OrigenDatos) ? "CAPTURA" : dto.OrigenDatos.Trim().ToUpperInvariant()));
                cmd.Parameters.Add(Param("estadoNuevo", estadoNuevo));
                cmd.Parameters.Add(Param("motivo", "Edición de datos de matriz; queda en revisión con cálculo actualizado."));
                cmd.Parameters.Add(Param("usuarioId", usuarioId));
                cmd.Parameters.Add(Param("matrizId", matrizId));
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var detalle in dto.Detalles)
                await GuardarDetalleAsync(conn, tx, matrizId, modelo.ModeloId, detalle, usuarioId);

            await InactivarResultadosVigentesAsync(conn, tx, matrizId);

            var datosNuevos = JsonConvert.SerializeObject(dto);
            await RegistrarHistorialAsync(conn, tx, matrizId, "RL_MR_MATRICES", matrizId.ToString(), "EDICION", matriz.Estado, estadoNuevo, "Edición de datos de matriz; el sistema recalcula y deja la matriz en revisión.", datosAnteriores, datosNuevos, usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_MATRICES", matrizId.ToString(), "UPDATE", datosAnteriores, datosNuevos, usuarioId, usuarioEmail, ip);

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<MatrizCalculoRequestDto?> PrepararSolicitudCalculoAsync(long matrizId, string tipoCalculo, string? motivoCalculo, bool esRecalculo)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var matriz = await ObtenerMatrizBaseAsync(conn, null, matrizId);
        if (matriz == null)
            return null;

        if (matriz.Estado is "CERRADA" or "INACTIVA")
            throw new InvalidOperationException("La matriz está cerrada o inactiva y no puede recalcularse.");

        var metodologia = await ConstruirMetodologiaAsync(conn, null, matriz.ModeloId, matriz.ModeloVersion);
        var detalles = await ObtenerDetallesMatrizAsync(conn, null, matrizId, matriz.ModeloId);
        var controles = await ObtenerControlesMatrizAsync(conn, null, matrizId);

        // Se transforma la persistencia Oracle al contrato del motor de cálculo.
        // Los controles solo viajan por factor y el motor decide su mitigación real.
        return new MatrizCalculoRequestDto
        {
            TipoCalculo = string.IsNullOrWhiteSpace(tipoCalculo) ? "GLOBAL" : tipoCalculo.Trim().ToUpperInvariant(),
            MotivoCalculo = motivoCalculo?.Trim(),
            EsRecalculo = esRecalculo,
            Metodologia = metodologia,
            Factores = detalles
                .Where(d => d.Puntaje.HasValue)
                .GroupBy(d => new { d.FactorId, d.FactorCodigo, d.FactorNombre, d.FactorPesoInstitucional })
                .OrderBy(g => g.Key.FactorCodigo)
                .Select(g => new FactorCalculoDto
                {
                    Codigo = g.Key.FactorCodigo,
                    Nombre = g.Key.FactorNombre,
                    PesoInstitucional = g.Key.FactorPesoInstitucional,
                    Variables = g.OrderBy(v => v.VariableCodigo).Select(v => new VariableCalculoDto
                    {
                        Codigo = v.VariableCodigo,
                        Nombre = v.VariableNombre,
                        PesoInterno = v.VariablePesoInterno,
                        Puntaje = v.Puntaje,
                        Obligatoria = v.Obligatoria,
                        TieneValor = v.Puntaje.HasValue
                    }).ToList(),
                    Controles = controles
                        .Where(c => c.FactorId == g.Key.FactorId)
                        .Select(c => new ControlCalculoDto
                        {
                            Codigo = c.ControlId.ToString(),
                            Nombre = c.Nombre,
                            MitigacionPct = c.EfectividadPct,
                            Activo = c.Estado.Equals("ACTIVO", StringComparison.OrdinalIgnoreCase),
                            TieneEvidencia = c.TieneEvidencia
                        }).ToList()
                })
                .ToList()
        };
    }

    public async Task PersistirResultadoCalculoAsync(long matrizId, MatrizCalculoResultadoDto resultado, string? motivoCalculo, bool esRecalculo, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var matriz = await ObtenerMatrizBaseAsync(conn, tx, matrizId) ?? throw new InvalidOperationException("No se encontró la matriz de riesgos.");
            if (matriz.Estado is "CERRADA" or "INACTIVA")
                throw new InvalidOperationException("La matriz está cerrada o inactiva y no puede recalcularse.");

            // Cada cálculo deja sin vigencia los resultados anteriores y crea nuevos
            // registros para sostener trazabilidad completa de recálculos.
            await InactivarResultadosVigentesAsync(conn, tx, matrizId);
            var snapshot = JsonConvert.SerializeObject(resultado);
            var resultadoInstitucionalId = await InsertarResultadoAsync(conn, tx, matrizId, null, "INSTITUCIONAL", resultado.VersionCalculo, resultado.PuntajeInherente, resultado.NivelInherente, resultado.MitigacionPct, resultado.PuntajeResidual, resultado.NivelResidual, resultado.RequierePlanAccion, motivoCalculo, null, snapshot, usuarioId);

            foreach (var factor in resultado.Factores)
            {
                var factorId = await ObtenerFactorIdPorCodigoAsync(conn, tx, matriz.ModeloId, factor.Codigo);
                await InsertarResultadoAsync(conn, tx, matrizId, factorId, "FACTOR", resultado.VersionCalculo, factor.PuntajeInherente, factor.NivelInherente, factor.MitigacionPct, factor.PuntajeResidual, factor.NivelResidual, factor.RequierePlanAccion, motivoCalculo, null, JsonConvert.SerializeObject(factor), usuarioId);
            }

            var estadoResultado = "EN_REVISION";

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE RL_MR_MATRICES
                       SET MRMAT_ESTADO = :estadoResultado,
                           MRMAT_MOTIVO_ESTADO = :motivo,
                           MRMAT_USR_MODIF_ID = :usuarioId,
                           MRMAT_FECHA_MODIF = SYSDATE
                     WHERE MRMAT_ID = :matrizId";
                cmd.Parameters.Add(Param("estadoResultado", estadoResultado));
                cmd.Parameters.Add(Param("motivo", string.IsNullOrWhiteSpace(motivoCalculo) ? "Cálculo de matriz de riesgos." : motivoCalculo));
                cmd.Parameters.Add(Param("usuarioId", usuarioId));
                cmd.Parameters.Add(Param("matrizId", matrizId));
                await cmd.ExecuteNonQueryAsync();
            }

            var accion = esRecalculo ? "RECALCULO" : "CALCULO";
            await RegistrarHistorialAsync(conn, tx, matrizId, "RL_MR_RESULTADOS", resultadoInstitucionalId.ToString(), accion, matriz.Estado, estadoResultado, motivoCalculo, null, snapshot, usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_RESULTADOS", resultadoInstitucionalId.ToString(), "INSERT", null, snapshot, usuarioId, usuarioEmail, ip);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> CambiarEstadoAsync(long matrizId, string estado, string motivo, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var matriz = await ObtenerMatrizBaseAsync(conn, tx, matrizId);
            if (matriz == null)
                return false;

            var estadoNuevo = estado.Trim().ToUpperInvariant();
            var motivoNormalizado = motivo.Trim();
            var motivoActual = matriz.MotivoEstado?.Trim() ?? string.Empty;
            if (matriz.Estado.Equals(estadoNuevo, StringComparison.OrdinalIgnoreCase)
                && motivoActual.Equals(motivoNormalizado, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La matriz ya tiene ese estado registrado con el mismo motivo. No se generó un nuevo historial.");
            }

            if (await ExisteMotivoCambioEstadoAsync(conn, tx, matrizId, motivoNormalizado))
                throw new InvalidOperationException("El motivo indicado ya fue utilizado en un cambio de estado de esta matriz.");

            if (estadoNuevo == "CERRADA" && matriz.RequierePlanAccion && !await TienePlanTratadoParaCierreAsync(conn, tx, matrizId))
                throw new InvalidOperationException("No se puede cerrar la matriz porque requiere plan de acción y no tiene un plan cerrado o una justificación aprobada.");

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE RL_MR_MATRICES
                       SET MRMAT_ESTADO = :estado,
                           MRMAT_MOTIVO_ESTADO = :motivo,
                           MRMAT_CERRADO_POR = CASE WHEN :estadoCierre = 'CERRADA' THEN :usuarioId ELSE MRMAT_CERRADO_POR END,
                           MRMAT_FECHA_CIERRE = CASE WHEN :estadoCierre = 'CERRADA' THEN SYSDATE ELSE MRMAT_FECHA_CIERRE END,
                           MRMAT_USR_MODIF_ID = :usuarioId,
                           MRMAT_FECHA_MODIF = SYSDATE
                     WHERE MRMAT_ID = :matrizId";
                cmd.Parameters.Add(Param("estado", estadoNuevo));
                cmd.Parameters.Add(Param("motivo", motivoNormalizado));
                cmd.Parameters.Add(Param("estadoCierre", estadoNuevo));
                cmd.Parameters.Add(Param("usuarioId", usuarioId));
                cmd.Parameters.Add(Param("matrizId", matrizId));
                await cmd.ExecuteNonQueryAsync();
            }

            var datos = JsonConvert.SerializeObject(new { EstadoAnterior = matriz.Estado, EstadoNuevo = estadoNuevo, Motivo = motivoNormalizado });
            await RegistrarHistorialAsync(conn, tx, matrizId, "RL_MR_MATRICES", matrizId.ToString(), "CAMBIO_ESTADO", matriz.Estado, estadoNuevo, motivoNormalizado, null, datos, usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_MATRICES", matrizId.ToString(), "UPDATE", JsonConvert.SerializeObject(new { matriz.Estado }), datos, usuarioId, usuarioEmail, ip);

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> EliminarMatrizAsync(long matrizId, string motivo, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var matriz = await ObtenerMatrizBaseAsync(conn, tx, matrizId);
            if (matriz == null)
                return false;

            var motivoNormalizado = motivo.Trim();
            var datosAnteriores = JsonConvert.SerializeObject(matriz);
            var datosNuevos = JsonConvert.SerializeObject(new
            {
                EstadoAnterior = matriz.Estado,
                EstadoNuevo = "INACTIVA",
                EstadoRegistro = 0,
                Motivo = motivoNormalizado
            });

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE RL_MR_MATRICES
                       SET MRMAT_ESTADO = 'INACTIVA',
                           MRMAT_ESTADO_REGISTRO = 0,
                           MRMAT_MOTIVO_ESTADO = :motivo,
                           MRMAT_USR_MODIF_ID = :usuarioId,
                           MRMAT_FECHA_MODIF = SYSDATE
                     WHERE MRMAT_ID = :matrizId
                       AND MRMAT_ESTADO_REGISTRO = 1";
                cmd.Parameters.Add(Param("motivo", motivoNormalizado));
                cmd.Parameters.Add(Param("usuarioId", usuarioId));
                cmd.Parameters.Add(Param("matrizId", matrizId));
                if (await cmd.ExecuteNonQueryAsync() == 0)
                    return false;
            }

            await InactivarResultadosVigentesAsync(conn, tx, matrizId);
            await RegistrarHistorialAsync(conn, tx, matrizId, "RL_MR_MATRICES", matrizId.ToString(), "ELIMINACION_LOGICA", matriz.Estado, "INACTIVA", motivoNormalizado, datosAnteriores, datosNuevos, usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_MATRICES", matrizId.ToString(), "DELETE", datosAnteriores, datosNuevos, usuarioId, usuarioEmail, ip);

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<List<MatrizRiesgoHistorialDto>> ObtenerHistorialAsync(long matrizId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var result = new List<MatrizRiesgoHistorialDto>();
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = @"
            SELECT MRH_ID, MRH_MATRIZ_ID, MRH_TABLA, MRH_REGISTRO_ID, MRH_ACCION,
                   MRH_ESTADO_ANTERIOR, MRH_ESTADO_NUEVO, MRH_MOTIVO, MRH_USR_ID,
                   MRH_USR_EMAIL, MRH_IP, MRH_FECHA
              FROM RL_MR_HISTORIAL
             WHERE MRH_MATRIZ_ID = :matrizId
             ORDER BY MRH_FECHA DESC, MRH_ID DESC";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoHistorialDto
            {
                HistorialId = ToLong(reader["MRH_ID"]),
                MatrizId = reader["MRH_MATRIZ_ID"] == DBNull.Value ? null : ToLong(reader["MRH_MATRIZ_ID"]),
                Tabla = reader["MRH_TABLA"].ToString() ?? string.Empty,
                RegistroId = reader["MRH_REGISTRO_ID"].ToString() ?? string.Empty,
                Accion = reader["MRH_ACCION"].ToString() ?? string.Empty,
                EstadoAnterior = ToNullableString(reader["MRH_ESTADO_ANTERIOR"]),
                EstadoNuevo = ToNullableString(reader["MRH_ESTADO_NUEVO"]),
                Motivo = ToNullableString(reader["MRH_MOTIVO"]),
                UsuarioId = reader["MRH_USR_ID"] == DBNull.Value ? null : ToLong(reader["MRH_USR_ID"]),
                UsuarioEmail = ToNullableString(reader["MRH_USR_EMAIL"]),
                Ip = ToNullableString(reader["MRH_IP"]),
                Fecha = ToDate(reader["MRH_FECHA"])
            });
        }
        return result;
    }

    private async Task<bool> ExisteMotivoCambioEstadoAsync(OracleConnection conn, OracleTransaction tx, long matrizId, string motivo)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT COUNT(*)
              FROM RL_MR_HISTORIAL
             WHERE MRH_MATRIZ_ID = :matrizId
               AND MRH_ACCION = 'CAMBIO_ESTADO'
               AND UPPER(TRIM(MRH_MOTIVO)) = UPPER(TRIM(:motivo))";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        cmd.Parameters.Add(Param("motivo", motivo));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task<List<MatrizRiesgoPlanAccionDto>> ListarPlanesAsync(long matrizId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        return await ObtenerPlanesMatrizAsync(conn, null, matrizId);
    }

    public async Task<long> CrearPlanAsync(long matrizId, MatrizRiesgoPlanAccionRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var matriz = await ObtenerMatrizBaseAsync(conn, tx, matrizId) ?? throw new InvalidOperationException("No se encontró la matriz de riesgos.");
            if (matriz.Estado is "CERRADA" or "INACTIVA")
                throw new InvalidOperationException("No se pueden crear planes en una matriz cerrada o inactiva.");
            if (dto.ResultadoId.HasValue && !await ExisteResultadoMatrizAsync(conn, tx, matrizId, dto.ResultadoId.Value))
                throw new InvalidOperationException("El resultado seleccionado no pertenece a la matriz.");
            if (await ExistePlanDuplicadoAsync(conn, tx, matrizId, dto.Actividad, dto.Responsable, null))
                throw new InvalidOperationException("Ya existe un plan activo con la misma actividad y responsable para esta matriz.");

            var planId = await NextValAsync(conn, tx, "SEQ_RL_MR_PLANES_ACCION");
            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO RL_MR_PLANES_ACCION (
                        MRPA_ID, MRPA_MATRIZ_ID, MRPA_RESULTADO_ID, MRPA_ACTIVIDAD,
                        MRPA_RESPONSABLE, MRPA_PERIODICIDAD, MRPA_FECHA_INICIO,
                        MRPA_FECHA_FIN, MRPA_MEDIO_PRUEBA, MRPA_OBSERVACIONES,
                        MRPA_ESTADO, MRPA_USR_CREACION_ID, MRPA_FECHA_CREACION
                    ) VALUES (
                        :id, :matrizId, :resultadoId, :actividad,
                        :responsable, :periodicidad, :fechaInicio,
                        :fechaFin, :medioPrueba, :observaciones,
                        'PENDIENTE', :usuarioId, SYSDATE
                    )";
                cmd.Parameters.Add(Param("id", planId));
                cmd.Parameters.Add(Param("matrizId", matrizId));
                cmd.Parameters.Add(Param("resultadoId", dto.ResultadoId));
                cmd.Parameters.Add(Param("actividad", dto.Actividad));
                cmd.Parameters.Add(Param("responsable", dto.Responsable));
                cmd.Parameters.Add(Param("periodicidad", dto.Periodicidad));
                cmd.Parameters.Add(Param("fechaInicio", dto.FechaInicio));
                cmd.Parameters.Add(Param("fechaFin", dto.FechaFin));
                cmd.Parameters.Add(Param("medioPrueba", dto.MedioPrueba));
                cmd.Parameters.Add(Param("observaciones", dto.Observaciones));
                cmd.Parameters.Add(Param("usuarioId", usuarioId));
                await cmd.ExecuteNonQueryAsync();
            }

            var datos = JsonConvert.SerializeObject(dto);
            await RegistrarHistorialAsync(conn, tx, matrizId, "RL_MR_PLANES_ACCION", planId.ToString(), "CREACION_PLAN", null, "PENDIENTE", "Registro de plan de acción.", null, datos, usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_PLANES_ACCION", planId.ToString(), "INSERT", null, datos, usuarioId, usuarioEmail, ip);
            tx.Commit();
            return planId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> ActualizarPlanAsync(long matrizId, long planId, MatrizRiesgoPlanAccionRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var anterior = await ObtenerPlanAuditoriaAsync(conn, tx, matrizId, planId);
            if (anterior == null)
                return false;

            if (await MatrizEstaCerradaOInactivaAsync(conn, tx, matrizId))
                throw new InvalidOperationException("No se pueden editar planes en una matriz cerrada o inactiva.");
            if (dto.ResultadoId.HasValue && !await ExisteResultadoMatrizAsync(conn, tx, matrizId, dto.ResultadoId.Value))
                throw new InvalidOperationException("El resultado seleccionado no pertenece a la matriz.");
            if (await ExistePlanDuplicadoAsync(conn, tx, matrizId, dto.Actividad, dto.Responsable, planId))
                throw new InvalidOperationException("Ya existe otro plan activo con la misma actividad y responsable para esta matriz.");

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE RL_MR_PLANES_ACCION
                       SET MRPA_RESULTADO_ID = :resultadoId,
                           MRPA_ACTIVIDAD = :actividad,
                           MRPA_RESPONSABLE = :responsable,
                           MRPA_PERIODICIDAD = :periodicidad,
                           MRPA_FECHA_INICIO = :fechaInicio,
                           MRPA_FECHA_FIN = :fechaFin,
                           MRPA_MEDIO_PRUEBA = :medioPrueba,
                           MRPA_OBSERVACIONES = :observaciones
                     WHERE MRPA_ID = :planId
                       AND MRPA_MATRIZ_ID = :matrizId
                       AND MRPA_ESTADO <> 'INACTIVO'";
                cmd.Parameters.Add(Param("resultadoId", dto.ResultadoId));
                cmd.Parameters.Add(Param("actividad", dto.Actividad));
                cmd.Parameters.Add(Param("responsable", dto.Responsable));
                cmd.Parameters.Add(Param("periodicidad", dto.Periodicidad));
                cmd.Parameters.Add(Param("fechaInicio", dto.FechaInicio));
                cmd.Parameters.Add(Param("fechaFin", dto.FechaFin));
                cmd.Parameters.Add(Param("medioPrueba", dto.MedioPrueba));
                cmd.Parameters.Add(Param("observaciones", dto.Observaciones));
                cmd.Parameters.Add(Param("planId", planId));
                cmd.Parameters.Add(Param("matrizId", matrizId));
                if (await cmd.ExecuteNonQueryAsync() == 0)
                    return false;
            }

            var nuevo = JsonConvert.SerializeObject(dto);
            await RegistrarHistorialAsync(conn, tx, matrizId, "RL_MR_PLANES_ACCION", planId.ToString(), "EDICION_PLAN", null, null, "Actualización de plan de acción.", anterior, nuevo, usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_PLANES_ACCION", planId.ToString(), "UPDATE", anterior, nuevo, usuarioId, usuarioEmail, ip);
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> CambiarEstadoPlanAsync(long matrizId, long planId, string estado, string motivo, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var plan = await ObtenerPlanAsync(conn, tx, matrizId, planId);
            if (plan == null)
                return false;

            var estadoNuevo = estado.Trim().ToUpperInvariant();
            var datosAnt = JsonConvert.SerializeObject(plan);
            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE RL_MR_PLANES_ACCION
                       SET MRPA_ESTADO = :estado,
                           MRPA_MOTIVO_CIERRE = CASE WHEN :estadoCierre IN ('CERRADO','VENCIDO') THEN :motivo ELSE MRPA_MOTIVO_CIERRE END,
                           MRPA_USR_CIERRE_ID = CASE WHEN :estadoCierre = 'CERRADO' THEN :usuarioId ELSE MRPA_USR_CIERRE_ID END,
                           MRPA_FECHA_CIERRE = CASE WHEN :estadoCierre = 'CERRADO' THEN SYSDATE ELSE MRPA_FECHA_CIERRE END
                     WHERE MRPA_ID = :planId
                       AND MRPA_MATRIZ_ID = :matrizId
                       AND MRPA_ESTADO <> 'INACTIVO'";
                cmd.Parameters.Add(Param("estado", estadoNuevo));
                cmd.Parameters.Add(Param("estadoCierre", estadoNuevo));
                cmd.Parameters.Add(Param("motivo", motivo));
                cmd.Parameters.Add(Param("usuarioId", usuarioId));
                cmd.Parameters.Add(Param("planId", planId));
                cmd.Parameters.Add(Param("matrizId", matrizId));
                if (await cmd.ExecuteNonQueryAsync() == 0)
                    return false;
            }

            var datosNvo = JsonConvert.SerializeObject(new { EstadoAnterior = plan.Estado, EstadoNuevo = estadoNuevo, Motivo = motivo });
            await RegistrarHistorialAsync(conn, tx, matrizId, "RL_MR_PLANES_ACCION", planId.ToString(), "CAMBIO_ESTADO_PLAN", plan.Estado, estadoNuevo, motivo, datosAnt, datosNvo, usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_PLANES_ACCION", planId.ToString(), "UPDATE", datosAnt, datosNvo, usuarioId, usuarioEmail, ip);
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> InactivarPlanAsync(long matrizId, long planId, string motivo, long usuarioId, string? usuarioEmail, string? ip)
    {
        return await CambiarEstadoPlanAsync(matrizId, planId, "INACTIVO", motivo, usuarioId, usuarioEmail, ip);
    }

    public async Task<bool> ReactivarPlanAsync(long matrizId, long planId, string motivo, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            if (await MatrizEstaCerradaOInactivaAsync(conn, tx, matrizId))
                throw new InvalidOperationException("No se pueden reactivar planes en una matriz cerrada o inactiva.");

            var plan = await ObtenerPlanAsync(conn, tx, matrizId, planId);
            if (plan == null || plan.Estado != "INACTIVO")
                return false;

            var datosAnt = JsonConvert.SerializeObject(plan);
            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE RL_MR_PLANES_ACCION
                       SET MRPA_ESTADO = 'PENDIENTE',
                           MRPA_MOTIVO_CIERRE = NULL,
                           MRPA_USR_CIERRE_ID = NULL,
                           MRPA_FECHA_CIERRE = NULL
                     WHERE MRPA_ID = :planId
                       AND MRPA_MATRIZ_ID = :matrizId
                       AND MRPA_ESTADO = 'INACTIVO'";
                cmd.Parameters.Add(Param("planId", planId));
                cmd.Parameters.Add(Param("matrizId", matrizId));
                if (await cmd.ExecuteNonQueryAsync() == 0)
                    return false;
            }

            var datosNvo = JsonConvert.SerializeObject(new { EstadoAnterior = plan.Estado, EstadoNuevo = "PENDIENTE", Motivo = motivo });
            await RegistrarHistorialAsync(conn, tx, matrizId, "RL_MR_PLANES_ACCION", planId.ToString(), "REACTIVACION_PLAN", plan.Estado, "PENDIENTE", motivo, datosAnt, datosNvo, usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_PLANES_ACCION", planId.ToString(), "UPDATE", datosAnt, datosNvo, usuarioId, usuarioEmail, ip);
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> TienePlanTratadoParaCierreAsync(long matrizId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        return await TienePlanTratadoParaCierreAsync(conn, null, matrizId);
    }

    public async Task<List<MatrizRiesgoEvidenciaDto>> ListarEvidenciasAsync(long matrizId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        return await ObtenerEvidenciasMatrizAsync(conn, null, matrizId);
    }

    public async Task<long> RegistrarEvidenciaAsync(MatrizRiesgoEvidenciaRegistroDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var matriz = await ObtenerMatrizBaseAsync(conn, tx, dto.MatrizId) ?? throw new InvalidOperationException("No se encontró la matriz de riesgos.");
            if (matriz.Estado is "CERRADA" or "INACTIVA")
                throw new InvalidOperationException("No se pueden cargar evidencias en una matriz cerrada o inactiva.");
            if (dto.ControlId.HasValue && !await ExisteControlMatrizAsync(conn, tx, dto.MatrizId, dto.ControlId.Value))
                throw new InvalidOperationException("El control seleccionado no pertenece a la matriz.");
            if (dto.PlanId.HasValue && !await ExistePlanActivoMatrizAsync(conn, tx, dto.MatrizId, dto.PlanId.Value))
                throw new InvalidOperationException("El plan seleccionado no pertenece a la matriz o está inactivo.");

            var evidenciaId = await NextValAsync(conn, tx, "SEQ_RL_MR_EVIDENCIAS");
            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO RL_MR_EVIDENCIAS (
                        MREV_ID, MREV_MATRIZ_ID, MREV_CONTROL_ID, MREV_PLAN_ID,
                        MREV_NOMBRE_ORIGINAL, MREV_NOMBRE_FISICO, MREV_TIPO_MIME,
                        MREV_EXTENSION, MREV_TAMANO_BYTES, MREV_RUTA_FISICA,
                        MREV_HASH_SHA256, MREV_ESTADO_REGISTRO, MREV_USR_CREACION_ID,
                        MREV_FECHA_CREACION
                    ) VALUES (
                        :id, :matrizId, :controlId, :planId,
                        :nombreOriginal, :nombreFisico, :tipoMime,
                        :extension, :tamanoBytes, :rutaFisica,
                        :hashSha256, 1, :usuarioId, SYSDATE
                    )";
                cmd.Parameters.Add(Param("id", evidenciaId));
                cmd.Parameters.Add(Param("matrizId", dto.MatrizId));
                cmd.Parameters.Add(Param("controlId", dto.ControlId));
                cmd.Parameters.Add(Param("planId", dto.PlanId));
                cmd.Parameters.Add(Param("nombreOriginal", dto.NombreOriginal));
                cmd.Parameters.Add(Param("nombreFisico", dto.NombreFisico));
                cmd.Parameters.Add(Param("tipoMime", dto.TipoMime));
                cmd.Parameters.Add(Param("extension", dto.Extension));
                cmd.Parameters.Add(Param("tamanoBytes", dto.TamanoBytes));
                cmd.Parameters.Add(Param("rutaFisica", dto.RutaFisica));
                cmd.Parameters.Add(Param("hashSha256", dto.HashSha256));
                cmd.Parameters.Add(Param("usuarioId", usuarioId));
                await cmd.ExecuteNonQueryAsync();
            }

            var datos = JsonConvert.SerializeObject(dto);
            await RegistrarHistorialAsync(conn, tx, dto.MatrizId, "RL_MR_EVIDENCIAS", evidenciaId.ToString(), "CARGA_EVIDENCIA", null, "ACTIVA", "Carga de evidencia documental.", null, datos, usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_EVIDENCIAS", evidenciaId.ToString(), "INSERT", null, datos, usuarioId, usuarioEmail, ip);
            tx.Commit();
            return evidenciaId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<MatrizRiesgoEvidenciaDto?> ObtenerEvidenciaAsync(long matrizId, long evidenciaId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        return await ObtenerEvidenciaAsync(conn, null, matrizId, evidenciaId);
    }

    public async Task RegistrarDescargaEvidenciaAsync(long matrizId, long evidenciaId, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        var datos = JsonConvert.SerializeObject(new { EvidenciaId = evidenciaId, MatrizId = matrizId, Accion = "DESCARGA_EVIDENCIA" });
        await RegistrarHistorialAsync(conn, tx, matrizId, "RL_MR_EVIDENCIAS", evidenciaId.ToString(), "DESCARGA_EVIDENCIA", null, null, "Descarga o visualización de evidencia.", null, datos, usuarioId, usuarioEmail, ip);
        await RegistrarAuditoriaAsync(conn, tx, "RL_MR_EVIDENCIAS", evidenciaId.ToString(), "VER", null, datos, usuarioId, usuarioEmail, ip);
        tx.Commit();
    }

    public async Task<bool> InactivarEvidenciaAsync(long matrizId, long evidenciaId, string motivo, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var evidencia = await ObtenerEvidenciaAsync(conn, tx, matrizId, evidenciaId);
            if (evidencia == null || !evidencia.Activa)
                return false;

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE RL_MR_EVIDENCIAS
                       SET MREV_ESTADO_REGISTRO = 0,
                           MREV_MOTIVO_INACTIVO = :motivo,
                           MREV_USR_INACTIVO_ID = :usuarioId,
                           MREV_FECHA_INACTIVO = SYSDATE
                     WHERE MREV_ID = :evidenciaId
                       AND MREV_MATRIZ_ID = :matrizId
                       AND MREV_ESTADO_REGISTRO = 1";
                cmd.Parameters.Add(Param("motivo", motivo));
                cmd.Parameters.Add(Param("usuarioId", usuarioId));
                cmd.Parameters.Add(Param("evidenciaId", evidenciaId));
                cmd.Parameters.Add(Param("matrizId", matrizId));
                if (await cmd.ExecuteNonQueryAsync() == 0)
                    return false;
            }

            var datosAnt = JsonConvert.SerializeObject(evidencia);
            var datosNvo = JsonConvert.SerializeObject(new { Motivo = motivo, EstadoRegistro = 0 });
            await RegistrarHistorialAsync(conn, tx, matrizId, "RL_MR_EVIDENCIAS", evidenciaId.ToString(), "ELIMINACION_LOGICA_EVIDENCIA", "ACTIVA", "INACTIVA", motivo, datosAnt, datosNvo, usuarioId, usuarioEmail, ip);
            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_EVIDENCIAS", evidenciaId.ToString(), "DELETE", datosAnt, datosNvo, usuarioId, usuarioEmail, ip);
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<List<MatrizRiesgoCriterioDto>> ListarCriteriosAsync(bool incluirInactivos)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        var modelo = await ObtenerModeloVigenteAsync(conn, null) ?? throw new InvalidOperationException("No existe una metodología aprobada vigente para Matrices de Riesgos.");
        return await ObtenerCriteriosAdministrablesAsync(conn, null, modelo.ModeloId, incluirInactivos);
    }

    public async Task<long> CrearCriterioAsync(MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var modelo = await ObtenerModeloVigenteAsync(conn, tx) ?? throw new InvalidOperationException("No existe una metodología aprobada vigente para Matrices de Riesgos.");
            await ValidarVariableYEscalaCriterioAsync(conn, tx, modelo.ModeloId, dto.VariableId, dto.EscalaId);
            if (await ExisteCriterioDuplicadoAsync(conn, tx, modelo.ModeloId, dto, null))
                throw new InvalidOperationException("Ya existe un criterio activo con la misma variable, escala y rango.");

            var criterioId = await NextValAsync(conn, tx, "SEQ_RL_MR_CRITERIOS");
            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO RL_MR_CRITERIOS (
                        MRC_ID, MRC_VARIABLE_ID, MRC_ESCALA_ID, MRC_VALOR_DESDE,
                        MRC_VALOR_HASTA, MRC_PUNTAJE, MRC_DESCRIPCION,
                        MRC_ESTADO_REGISTRO
                    ) VALUES (
                        :id, :variableId, :escalaId, :valorDesde,
                        :valorHasta, :puntaje, :descripcion,
                        1
                    )";
                cmd.Parameters.Add(Param("id", criterioId));
                cmd.Parameters.Add(Param("variableId", dto.VariableId));
                cmd.Parameters.Add(Param("escalaId", dto.EscalaId));
                cmd.Parameters.Add(Param("valorDesde", dto.ValorDesde));
                cmd.Parameters.Add(Param("valorHasta", dto.ValorHasta));
                cmd.Parameters.Add(Param("puntaje", dto.Puntaje));
                cmd.Parameters.Add(Param("descripcion", dto.Descripcion.Trim()));
                await cmd.ExecuteNonQueryAsync();
            }

            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_CRITERIOS", criterioId.ToString(), "INSERT", null, JsonConvert.SerializeObject(dto), usuarioId, usuarioEmail, ip);
            tx.Commit();
            return criterioId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> ActualizarCriterioAsync(long criterioId, MatrizRiesgoCriterioRequestDto dto, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var modelo = await ObtenerModeloVigenteAsync(conn, tx) ?? throw new InvalidOperationException("No existe una metodología aprobada vigente para Matrices de Riesgos.");
            await ValidarVariableYEscalaCriterioAsync(conn, tx, modelo.ModeloId, dto.VariableId, dto.EscalaId);
            var anterior = await ObtenerCriterioAuditoriaAsync(conn, tx, criterioId);
            if (anterior == null)
                return false;

            if (await ExisteCriterioDuplicadoAsync(conn, tx, modelo.ModeloId, dto, criterioId))
                throw new InvalidOperationException("Ya existe un criterio activo con la misma variable, escala y rango.");

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE RL_MR_CRITERIOS
                       SET MRC_VARIABLE_ID = :variableId,
                           MRC_ESCALA_ID = :escalaId,
                           MRC_VALOR_DESDE = :valorDesde,
                           MRC_VALOR_HASTA = :valorHasta,
                           MRC_PUNTAJE = :puntaje,
                           MRC_DESCRIPCION = :descripcion
                     WHERE MRC_ID = :criterioId
                       AND MRC_ESTADO_REGISTRO = 1";
                cmd.Parameters.Add(Param("variableId", dto.VariableId));
                cmd.Parameters.Add(Param("escalaId", dto.EscalaId));
                cmd.Parameters.Add(Param("valorDesde", dto.ValorDesde));
                cmd.Parameters.Add(Param("valorHasta", dto.ValorHasta));
                cmd.Parameters.Add(Param("puntaje", dto.Puntaje));
                cmd.Parameters.Add(Param("descripcion", dto.Descripcion.Trim()));
                cmd.Parameters.Add(Param("criterioId", criterioId));
                var affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                    return false;
            }

            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_CRITERIOS", criterioId.ToString(), "UPDATE", anterior, JsonConvert.SerializeObject(dto), usuarioId, usuarioEmail, ip);
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> InactivarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var anterior = await ObtenerCriterioAuditoriaAsync(conn, tx, criterioId);
            if (anterior == null)
                return false;

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE RL_MR_CRITERIOS
                       SET MRC_ESTADO_REGISTRO = 0,
                           MRC_MOTIVO_INACTIVO = :motivo
                     WHERE MRC_ID = :criterioId
                       AND MRC_ESTADO_REGISTRO = 1";
                cmd.Parameters.Add(Param("motivo", motivo.Trim()));
                cmd.Parameters.Add(Param("criterioId", criterioId));
                var affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                    return false;
            }

            await RegistrarAuditoriaAsync(conn, tx, "RL_MR_CRITERIOS", criterioId.ToString(), "UPDATE", anterior, JsonConvert.SerializeObject(new { Motivo = motivo, EstadoRegistro = 0 }), usuarioId, usuarioEmail, ip);
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> EliminarCriterioAsync(long criterioId, string motivo, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var anterior = await ObtenerCriterioAuditoriaAsync(conn, tx, criterioId);
            if (anterior == null)
                return false;

            await RegistrarAuditoriaAsync(
                conn,
                tx,
                "RL_MR_CRITERIOS",
                criterioId.ToString(),
                "DELETE",
                anterior,
                JsonConvert.SerializeObject(new { Motivo = motivo.Trim(), EliminacionFisica = true }),
                usuarioId,
                usuarioEmail,
                ip);

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM RL_MR_CRITERIOS WHERE MRC_ID = :criterioId";
                cmd.Parameters.Add(Param("criterioId", criterioId));
                var affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                    return false;
            }

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private async Task<(long ModeloId, string Version)?> ObtenerModeloVigenteAsync(OracleConnection conn, OracleTransaction? tx)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT MRM_ID, MRM_VERSION
              FROM (
                    SELECT MRM_ID, MRM_VERSION
                      FROM RL_MR_MODELOS
                     WHERE MRM_ESTADO = 'APROBADO'
                       AND MRM_ESTADO_REGISTRO = 1
                     ORDER BY NVL(MRM_FECHA_VIGENCIA, MRM_FECHA_CREACION) DESC, MRM_ID DESC
                   )
             WHERE ROWNUM = 1";
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return (ToLong(reader["MRM_ID"]), reader["MRM_VERSION"].ToString() ?? string.Empty);
    }

    private async Task<MetodologiaCalculoDto> ConstruirMetodologiaAsync(OracleConnection conn, OracleTransaction? tx, long modeloId, string version)
    {
        var factores = await ObtenerFactoresMetodologiaAsync(conn, tx, modeloId);
        var variables = await ObtenerVariablesMetodologiaAsync(conn, tx, modeloId);
        var escalas = await ObtenerEscalasMetodologiaAsync(conn, tx, modeloId);
        var criterios = await ObtenerCriteriosMetodologiaAsync(conn, tx, modeloId);
        var mitigaciones = escalas
            .Where(e => e.Tipo.Equals("CONTROL", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.ValorMinimo)
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        foreach (var escala in escalas)
        {
            escala.RequierePlanAccion =
                escala.Tipo.Equals("RESIDUAL", StringComparison.OrdinalIgnoreCase) &&
                NivelResidualRequierePlanAccion(escala.Nivel);
        }

        var escalasRiesgo = escalas
            .Where(e => e.Tipo.Equals("RESIDUAL", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (escalasRiesgo.Count == 0)
            escalasRiesgo = escalas.Where(e => e.Tipo.Equals("INHERENTE", StringComparison.OrdinalIgnoreCase)).ToList();

        if (escalasRiesgo.Count == 0)
            escalasRiesgo = escalas.Where(e => e.Tipo.Equals("VARIABLE", StringComparison.OrdinalIgnoreCase)).ToList();

        return new MetodologiaCalculoDto
        {
            Version = version,
            PesoTotalEsperado = 100m,
            PuntajeMinimo = escalasRiesgo.Count == 0 ? 1m : escalasRiesgo.Min(e => e.ValorMinimo),
            PuntajeMaximo = escalasRiesgo.Count == 0 ? 5m : escalasRiesgo.Max(e => e.ValorMaximo),
            MitigacionMaximaPct = mitigaciones.Count == 0 ? 55m : mitigaciones.Max(),
            DecimalesCalculo = 4,
            DecimalesVisualizacion = 2,
            FactoresInstitucionales = factores.Select(f => new FactorInstitucionalCalculoDto
            {
                Codigo = f.Codigo,
                Nombre = f.Nombre,
                PesoInstitucional = f.PesoInstitucional,
                ObligatorioGlobal = true
            }).ToList(),
            Variables = variables,
            EscalasRiesgo = escalasRiesgo.Select(e => new EscalaRiesgoCalculoDto
            {
                EscalaId = e.EscalaId,
                Tipo = e.Tipo,
                Nivel = e.Nivel,
                Color = e.Color,
                ValorMinimo = e.ValorMinimo,
                ValorMaximo = e.ValorMaximo,
                RequierePlanAccion = e.RequierePlanAccion
            }).ToList(),
            EscalasCatalogo = escalas,
            Criterios = criterios,
            MitigacionesPermitidas = mitigaciones
        };
    }

    private async Task<List<FactorMetodologiaRow>> ObtenerFactoresMetodologiaAsync(OracleConnection conn, OracleTransaction? tx, long modeloId)
    {
        var result = new List<FactorMetodologiaRow>();
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT MRF_ID, MRF_CODIGO, MRF_NOMBRE, MRF_PESO_INSTITUCIONAL
              FROM RL_MR_FACTORES
             WHERE MRF_MODELO_ID = :modeloId
               AND MRF_ESTADO_REGISTRO = 1
             ORDER BY MRF_ORDEN, MRF_ID";
        cmd.Parameters.Add(Param("modeloId", modeloId));
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new FactorMetodologiaRow(ToLong(reader["MRF_ID"]), reader["MRF_CODIGO"].ToString() ?? string.Empty, reader["MRF_NOMBRE"].ToString() ?? string.Empty, ToDecimal(reader["MRF_PESO_INSTITUCIONAL"])));
        }
        return result;
    }

    private async Task<List<VariableMetodologiaDto>> ObtenerVariablesMetodologiaAsync(OracleConnection conn, OracleTransaction? tx, long modeloId)
    {
        var result = new List<VariableMetodologiaDto>();
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT v.MRV_ID, v.MRV_CODIGO, v.MRV_NOMBRE, v.MRV_PESO_INTERNO,
                   v.MRV_OBLIGATORIA, f.MRF_ID, f.MRF_CODIGO, f.MRF_NOMBRE
              FROM RL_MR_VARIABLES v
              JOIN RL_MR_FACTORES f ON f.MRF_ID = v.MRV_FACTOR_ID
             WHERE f.MRF_MODELO_ID = :modeloId
               AND f.MRF_ESTADO_REGISTRO = 1
               AND v.MRV_ESTADO_REGISTRO = 1
             ORDER BY f.MRF_ORDEN, v.MRV_ORDEN, v.MRV_ID";
        cmd.Parameters.Add(Param("modeloId", modeloId));
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new VariableMetodologiaDto
            {
                VariableId = ToLong(reader["MRV_ID"]),
                FactorId = ToLong(reader["MRF_ID"]),
                FactorCodigo = reader["MRF_CODIGO"].ToString() ?? string.Empty,
                FactorNombre = reader["MRF_NOMBRE"].ToString() ?? string.Empty,
                Codigo = reader["MRV_CODIGO"].ToString() ?? string.Empty,
                Nombre = reader["MRV_NOMBRE"].ToString() ?? string.Empty,
                PesoInterno = ToDecimal(reader["MRV_PESO_INTERNO"]),
                Obligatoria = ToInt(reader["MRV_OBLIGATORIA"]) == 1
            });
        }
        return result;
    }

    private async Task<List<EscalaRiesgoCalculoDto>> ObtenerEscalasMetodologiaAsync(OracleConnection conn, OracleTransaction? tx, long modeloId)
    {
        var result = new List<EscalaRiesgoCalculoDto>();
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT MRE_ID, MRE_TIPO, MRE_VALOR_MIN, MRE_VALOR_MAX, MRE_NIVEL, MRE_COLOR_HEX
              FROM RL_MR_ESCALAS
             WHERE MRE_MODELO_ID = :modeloId
               AND MRE_ESTADO_REGISTRO = 1
             ORDER BY MRE_TIPO, MRE_ORDEN, MRE_VALOR_MIN";
        cmd.Parameters.Add(Param("modeloId", modeloId));
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new EscalaRiesgoCalculoDto
            {
                EscalaId = ToLong(reader["MRE_ID"]),
                Tipo = reader["MRE_TIPO"].ToString() ?? string.Empty,
                ValorMinimo = ToDecimal(reader["MRE_VALOR_MIN"]),
                ValorMaximo = ToDecimal(reader["MRE_VALOR_MAX"]),
                Nivel = reader["MRE_NIVEL"].ToString() ?? string.Empty,
                Color = ToNullableString(reader["MRE_COLOR_HEX"]) ?? string.Empty
            });
        }
        return result;
    }

    private async Task<List<CriterioCalculoDto>> ObtenerCriteriosMetodologiaAsync(OracleConnection conn, OracleTransaction? tx, long modeloId)
    {
        var result = new List<CriterioCalculoDto>();
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT c.MRC_ID, f.MRF_ID, f.MRF_CODIGO, f.MRF_NOMBRE,
                   c.MRC_VARIABLE_ID, v.MRV_CODIGO, v.MRV_NOMBRE, c.MRC_ESCALA_ID,
                   c.MRC_VALOR_DESDE, c.MRC_VALOR_HASTA, c.MRC_PUNTAJE,
                   c.MRC_DESCRIPCION
              FROM RL_MR_CRITERIOS c
              JOIN RL_MR_VARIABLES v ON v.MRV_ID = c.MRC_VARIABLE_ID
              JOIN RL_MR_FACTORES f ON f.MRF_ID = v.MRV_FACTOR_ID
             WHERE f.MRF_MODELO_ID = :modeloId
               AND f.MRF_ESTADO_REGISTRO = 1
               AND v.MRV_ESTADO_REGISTRO = 1
               AND c.MRC_ESTADO_REGISTRO = 1
             ORDER BY f.MRF_ORDEN, v.MRV_ORDEN, c.MRC_VALOR_DESDE, c.MRC_ID";
        cmd.Parameters.Add(Param("modeloId", modeloId));
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new CriterioCalculoDto
            {
                CriterioId = ToLong(reader["MRC_ID"]),
                FactorId = ToLong(reader["MRF_ID"]),
                FactorCodigo = reader["MRF_CODIGO"].ToString() ?? string.Empty,
                FactorNombre = reader["MRF_NOMBRE"].ToString() ?? string.Empty,
                VariableId = ToLong(reader["MRC_VARIABLE_ID"]),
                VariableCodigo = reader["MRV_CODIGO"].ToString() ?? string.Empty,
                VariableNombre = reader["MRV_NOMBRE"].ToString() ?? string.Empty,
                EscalaId = reader["MRC_ESCALA_ID"] == DBNull.Value ? null : ToLong(reader["MRC_ESCALA_ID"]),
                ValorDesde = ToNullableDecimal(reader["MRC_VALOR_DESDE"]),
                ValorHasta = ToNullableDecimal(reader["MRC_VALOR_HASTA"]),
                Puntaje = ToDecimal(reader["MRC_PUNTAJE"]),
                Descripcion = ToNullableString(reader["MRC_DESCRIPCION"]) ?? string.Empty
            });
        }
        return result;
    }

    private async Task<List<MatrizRiesgoCriterioDto>> ObtenerCriteriosAdministrablesAsync(OracleConnection conn, OracleTransaction? tx, long modeloId, bool incluirInactivos)
    {
        var result = new List<MatrizRiesgoCriterioDto>();
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT c.MRC_ID, f.MRF_ID, f.MRF_CODIGO, f.MRF_NOMBRE,
                   c.MRC_VARIABLE_ID, v.MRV_CODIGO, v.MRV_NOMBRE,
                   c.MRC_ESCALA_ID, e.MRE_TIPO, e.MRE_NIVEL,
                   c.MRC_VALOR_DESDE, c.MRC_VALOR_HASTA, c.MRC_PUNTAJE,
                   c.MRC_DESCRIPCION, c.MRC_ESTADO_REGISTRO, c.MRC_MOTIVO_INACTIVO
              FROM RL_MR_CRITERIOS c
              JOIN RL_MR_VARIABLES v ON v.MRV_ID = c.MRC_VARIABLE_ID
              JOIN RL_MR_FACTORES f ON f.MRF_ID = v.MRV_FACTOR_ID
              LEFT JOIN RL_MR_ESCALAS e ON e.MRE_ID = c.MRC_ESCALA_ID
             WHERE f.MRF_MODELO_ID = :modeloId
               AND f.MRF_ESTADO_REGISTRO = 1
               AND v.MRV_ESTADO_REGISTRO = 1
               AND (:incluirInactivos = 1 OR c.MRC_ESTADO_REGISTRO = 1)
             ORDER BY f.MRF_ORDEN, v.MRV_ORDEN, c.MRC_ESTADO_REGISTRO DESC,
                      c.MRC_VALOR_DESDE, c.MRC_ID";
        cmd.Parameters.Add(Param("modeloId", modeloId));
        cmd.Parameters.Add(Param("incluirInactivos", incluirInactivos ? 1 : 0));

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoCriterioDto
            {
                CriterioId = ToLong(reader["MRC_ID"]),
                FactorId = ToLong(reader["MRF_ID"]),
                FactorCodigo = reader["MRF_CODIGO"].ToString() ?? string.Empty,
                FactorNombre = reader["MRF_NOMBRE"].ToString() ?? string.Empty,
                VariableId = ToLong(reader["MRC_VARIABLE_ID"]),
                VariableCodigo = reader["MRV_CODIGO"].ToString() ?? string.Empty,
                VariableNombre = reader["MRV_NOMBRE"].ToString() ?? string.Empty,
                EscalaId = reader["MRC_ESCALA_ID"] == DBNull.Value ? null : ToLong(reader["MRC_ESCALA_ID"]),
                EscalaTipo = ToNullableString(reader["MRE_TIPO"]),
                EscalaNivel = ToNullableString(reader["MRE_NIVEL"]),
                ValorDesde = ToNullableDecimal(reader["MRC_VALOR_DESDE"]),
                ValorHasta = ToNullableDecimal(reader["MRC_VALOR_HASTA"]),
                Puntaje = ToDecimal(reader["MRC_PUNTAJE"]),
                Descripcion = ToNullableString(reader["MRC_DESCRIPCION"]) ?? string.Empty,
                Activo = ToInt(reader["MRC_ESTADO_REGISTRO"]) == 1,
                MotivoInactivo = ToNullableString(reader["MRC_MOTIVO_INACTIVO"])
            });
        }

        return result;
    }

    private async Task ValidarVariableYEscalaCriterioAsync(OracleConnection conn, OracleTransaction tx, long modeloId, long variableId, long? escalaId)
    {
        await using (var cmd = conn.CreateCommand())
        {
            cmd.BindByName = true;
            cmd.Transaction = tx;
            cmd.CommandText = @"
                SELECT COUNT(*)
                  FROM RL_MR_VARIABLES v
                  JOIN RL_MR_FACTORES f ON f.MRF_ID = v.MRV_FACTOR_ID
                 WHERE v.MRV_ID = :variableId
                   AND f.MRF_MODELO_ID = :modeloId
                   AND v.MRV_ESTADO_REGISTRO = 1
                   AND f.MRF_ESTADO_REGISTRO = 1";
            cmd.Parameters.Add(Param("variableId", variableId));
            cmd.Parameters.Add(Param("modeloId", modeloId));
            if (ToInt(await cmd.ExecuteScalarAsync()) == 0)
                throw new InvalidOperationException("La variable seleccionada no pertenece a la metodologia vigente.");
        }

        if (!escalaId.HasValue)
            return;

        await using (var cmd = conn.CreateCommand())
        {
            cmd.BindByName = true;
            cmd.Transaction = tx;
            cmd.CommandText = @"
                SELECT COUNT(*)
                  FROM RL_MR_ESCALAS
                 WHERE MRE_ID = :escalaId
                   AND MRE_MODELO_ID = :modeloId
                   AND MRE_ESTADO_REGISTRO = 1";
            cmd.Parameters.Add(Param("escalaId", escalaId.Value));
            cmd.Parameters.Add(Param("modeloId", modeloId));
            if (ToInt(await cmd.ExecuteScalarAsync()) == 0)
                throw new InvalidOperationException("La escala seleccionada no pertenece a la metodologia vigente.");
        }
    }

    private async Task<string?> ObtenerCriterioAuditoriaAsync(OracleConnection conn, OracleTransaction tx, long criterioId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT c.MRC_ID, c.MRC_VARIABLE_ID, c.MRC_ESCALA_ID,
                   c.MRC_VALOR_DESDE, c.MRC_VALOR_HASTA, c.MRC_PUNTAJE,
                   c.MRC_DESCRIPCION, c.MRC_ESTADO_REGISTRO, c.MRC_MOTIVO_INACTIVO
              FROM RL_MR_CRITERIOS c
             WHERE c.MRC_ID = :criterioId";
        cmd.Parameters.Add(Param("criterioId", criterioId));
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return JsonConvert.SerializeObject(new
        {
            CriterioId = ToLong(reader["MRC_ID"]),
            VariableId = ToLong(reader["MRC_VARIABLE_ID"]),
            EscalaId = reader["MRC_ESCALA_ID"] == DBNull.Value ? (long?)null : ToLong(reader["MRC_ESCALA_ID"]),
            ValorDesde = ToNullableDecimal(reader["MRC_VALOR_DESDE"]),
            ValorHasta = ToNullableDecimal(reader["MRC_VALOR_HASTA"]),
            Puntaje = ToDecimal(reader["MRC_PUNTAJE"]),
            Descripcion = ToNullableString(reader["MRC_DESCRIPCION"]),
            EstadoRegistro = ToInt(reader["MRC_ESTADO_REGISTRO"]),
            MotivoInactivo = ToNullableString(reader["MRC_MOTIVO_INACTIVO"])
        });
    }

    private async Task<bool> ExisteCriterioDuplicadoAsync(OracleConnection conn, OracleTransaction tx, long modeloId, MatrizRiesgoCriterioRequestDto dto, long? criterioIdExcluir)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT COUNT(*)
              FROM RL_MR_CRITERIOS c
              JOIN RL_MR_VARIABLES v ON v.MRV_ID = c.MRC_VARIABLE_ID
              JOIN RL_MR_FACTORES f ON f.MRF_ID = v.MRV_FACTOR_ID
             WHERE f.MRF_MODELO_ID = :modeloId
               AND c.MRC_ESTADO_REGISTRO = 1
               AND c.MRC_VARIABLE_ID = :variableId
               AND NVL(c.MRC_ESCALA_ID, -1) = NVL(:escalaId, -1)
               AND NVL(c.MRC_VALOR_DESDE, -999999999) = NVL(:valorDesde, -999999999)
               AND NVL(c.MRC_VALOR_HASTA, 999999999) = NVL(:valorHasta, 999999999)
               AND (:criterioIdExcluir IS NULL OR c.MRC_ID <> :criterioIdExcluir)";
        cmd.Parameters.Add(Param("modeloId", modeloId));
        cmd.Parameters.Add(Param("variableId", dto.VariableId));
        cmd.Parameters.Add(Param("escalaId", dto.EscalaId));
        cmd.Parameters.Add(Param("valorDesde", dto.ValorDesde));
        cmd.Parameters.Add(Param("valorHasta", dto.ValorHasta));
        cmd.Parameters.Add(Param("criterioIdExcluir", criterioIdExcluir));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }

    private async Task<bool> ExisteMatrizDuplicadaAsync(OracleConnection conn, OracleTransaction tx, MatrizRiesgoCrearRequestDto dto, long? matrizIdExcluir)
    {
        var documento = dto.Documento?.Trim();
        var sujetoIdExt = dto.SujetoIdExt?.Trim();

        if (string.IsNullOrWhiteSpace(documento) && string.IsNullOrWhiteSpace(sujetoIdExt))
        {
            return false;
        }

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT COUNT(*)
              FROM RL_MR_MATRICES m
             WHERE m.MRMAT_ESTADO_REGISTRO = 1
               AND (:matrizIdExcluir IS NULL OR m.MRMAT_ID <> :matrizIdExcluir)
               AND (
                    (:documento IS NOT NULL AND UPPER(TRIM(NVL(m.MRMAT_DOCUMENTO, ''))) = UPPER(TRIM(:documento)))
                    OR (:sujetoIdExt IS NOT NULL AND UPPER(TRIM(NVL(m.MRMAT_SUJETO_ID_EXT, ''))) = UPPER(TRIM(:sujetoIdExt)))
               )";
        cmd.Parameters.Add(Param("matrizIdExcluir", matrizIdExcluir));
        cmd.Parameters.Add(Param("documento", string.IsNullOrWhiteSpace(documento) ? null : documento));
        cmd.Parameters.Add(Param("sujetoIdExt", string.IsNullOrWhiteSpace(sujetoIdExt) ? null : sujetoIdExt));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }

    private async Task ValidarVariablesPorTipoSujetoAsync(OracleConnection conn, OracleTransaction tx, long modeloId, MatrizRiesgoCrearRequestDto dto)
    {
        var factorPermitido = FactorCodigoPorTipoSujeto(dto.SujetoTipo);
        if (string.IsNullOrWhiteSpace(factorPermitido))
            return;

        var variables = dto.Detalles.Select(d => d.VariableId).Distinct().ToList();
        if (variables.Count == 0)
            throw new InvalidOperationException("Debe registrar variables para evaluar la matriz.");

        foreach (var variableId in variables)
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
            if (!string.Equals(codigo, factorPermitido, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"La variable {variableId} no corresponde al tipo de sujeto {dto.SujetoTipo}.");
        }
    }

    private static string? FactorCodigoPorTipoSujeto(string? sujetoTipo)
    {
        return sujetoTipo?.Trim().ToUpperInvariant() switch
        {
            "PROVEEDOR" => "PROVEEDORES",
            "CLIENTE_PATRONO" => "CLIENTES_PATRONOS",
            "EMPLEADO" => "EMPLEADOS",
            _ => null
        };
    }

    private async Task<MatrizRiesgoDetalleDto?> ObtenerMatrizBaseAsync(OracleConnection conn, OracleTransaction? tx, long matrizId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT m.MRMAT_ID, m.MRMAT_MODELO_ID, mo.MRM_VERSION, m.MRMAT_SUJETO_TIPO,
                   m.MRMAT_SUJETO_ID_EXT, m.MRMAT_DOCUMENTO, m.MRMAT_NOMBRE_SUJETO,
                   m.MRMAT_ORIGEN_DATOS, m.MRMAT_ESTADO, m.MRMAT_FECHA_EVALUACION,
                   m.MRMAT_MOTIVO_ESTADO, m.MRMAT_SNAPSHOT_METODO,
                   r.MRR_PUNTAJE_INHERENTE, r.MRR_NIVEL_INHERENTE,
                   r.MRR_PUNTAJE_RESIDUAL, r.MRR_NIVEL_RESIDUAL, r.MRR_REQUIERE_PLAN
              FROM RL_MR_MATRICES m
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              LEFT JOIN RL_MR_RESULTADOS r
                ON r.MRR_MATRIZ_ID = m.MRMAT_ID
               AND r.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
               AND r.MRR_ES_VIGENTE = 1
             WHERE m.MRMAT_ID = :matrizId
               AND m.MRMAT_ESTADO_REGISTRO = 1";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var matriz = new MatrizRiesgoDetalleDto
        {
            MatrizId = ToLong(reader["MRMAT_ID"]),
            ModeloId = ToLong(reader["MRMAT_MODELO_ID"]),
            ModeloVersion = reader["MRM_VERSION"].ToString() ?? string.Empty,
            SujetoTipo = reader["MRMAT_SUJETO_TIPO"].ToString() ?? string.Empty,
            SujetoIdExt = ToNullableString(reader["MRMAT_SUJETO_ID_EXT"]),
            Documento = ToNullableString(reader["MRMAT_DOCUMENTO"]),
            NombreSujeto = reader["MRMAT_NOMBRE_SUJETO"].ToString() ?? string.Empty,
            OrigenDatos = reader["MRMAT_ORIGEN_DATOS"].ToString() ?? string.Empty,
            Estado = NormalizarEstadoFuncional(reader["MRMAT_ESTADO"].ToString()),
            FechaEvaluacion = ToDate(reader["MRMAT_FECHA_EVALUACION"]),
            MotivoEstado = ToNullableString(reader["MRMAT_MOTIVO_ESTADO"]),
            SnapshotMetodo = ToNullableString(reader["MRMAT_SNAPSHOT_METODO"]),
            PuntajeInherente = ToNullableDecimal(reader["MRR_PUNTAJE_INHERENTE"]),
            NivelInherente = ToNullableString(reader["MRR_NIVEL_INHERENTE"]),
            PuntajeResidual = ToNullableDecimal(reader["MRR_PUNTAJE_RESIDUAL"]),
            NivelResidual = ToNullableString(reader["MRR_NIVEL_RESIDUAL"]),
            RequierePlanAccion = ToInt(reader["MRR_REQUIERE_PLAN"]) == 1
        };
        return matriz;
    }

    private async Task<List<MatrizRiesgoVariableDetalleDto>> ObtenerDetallesMatrizAsync(OracleConnection conn, OracleTransaction? tx, long matrizId, long modeloId)
    {
        var result = new List<MatrizRiesgoVariableDetalleDto>();
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT NVL(d.MRD_ID, 0) MRD_ID, v.MRV_ID, f.MRF_ID, f.MRF_CODIGO, f.MRF_NOMBRE,
                   f.MRF_PESO_INSTITUCIONAL, v.MRV_CODIGO, v.MRV_NOMBRE, v.MRV_PESO_INTERNO,
                   v.MRV_OBLIGATORIA, d.MRD_VALOR_CAPTURADO, d.MRD_PUNTAJE,
                   d.MRD_PUNTAJE_PONDERADO, d.MRD_JUSTIFICACION, d.MRD_FUENTE_DATO
              FROM RL_MR_FACTORES f
              JOIN RL_MR_VARIABLES v ON v.MRV_FACTOR_ID = f.MRF_ID
              LEFT JOIN RL_MR_DETALLE d
                ON d.MRD_VARIABLE_ID = v.MRV_ID
               AND d.MRD_MATRIZ_ID = :matrizId
             WHERE f.MRF_MODELO_ID = :modeloId
               AND f.MRF_ESTADO_REGISTRO = 1
               AND v.MRV_ESTADO_REGISTRO = 1
             ORDER BY f.MRF_ORDEN, v.MRV_ORDEN, v.MRV_ID";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        cmd.Parameters.Add(Param("modeloId", modeloId));
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoVariableDetalleDto
            {
                DetalleId = ToLong(reader["MRD_ID"]),
                VariableId = ToLong(reader["MRV_ID"]),
                FactorId = ToLong(reader["MRF_ID"]),
                FactorCodigo = reader["MRF_CODIGO"].ToString() ?? string.Empty,
                FactorNombre = reader["MRF_NOMBRE"].ToString() ?? string.Empty,
                FactorPesoInstitucional = ToDecimal(reader["MRF_PESO_INSTITUCIONAL"]),
                VariableCodigo = reader["MRV_CODIGO"].ToString() ?? string.Empty,
                VariableNombre = reader["MRV_NOMBRE"].ToString() ?? string.Empty,
                VariablePesoInterno = ToDecimal(reader["MRV_PESO_INTERNO"]),
                Obligatoria = ToInt(reader["MRV_OBLIGATORIA"]) == 1,
                ValorCapturado = ToNullableString(reader["MRD_VALOR_CAPTURADO"]),
                Puntaje = ToNullableDecimal(reader["MRD_PUNTAJE"]),
                PuntajePonderado = ToNullableDecimal(reader["MRD_PUNTAJE_PONDERADO"]),
                Justificacion = ToNullableString(reader["MRD_JUSTIFICACION"]),
                FuenteDato = ToNullableString(reader["MRD_FUENTE_DATO"])
            });
        }
        return result;
    }

    private async Task<List<MatrizRiesgoPlanAccionDto>> ObtenerPlanesMatrizAsync(OracleConnection conn, OracleTransaction? tx, long matrizId)
    {
        var result = new List<MatrizRiesgoPlanAccionDto>();
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT MRPA_ID, MRPA_MATRIZ_ID, MRPA_RESULTADO_ID, MRPA_ACTIVIDAD,
                   MRPA_RESPONSABLE, MRPA_PERIODICIDAD, MRPA_FECHA_INICIO,
                   MRPA_FECHA_FIN, MRPA_MEDIO_PRUEBA, MRPA_OBSERVACIONES,
                   MRPA_ESTADO, MRPA_MOTIVO_CIERRE, MRPA_FECHA_CREACION,
                   MRPA_FECHA_CIERRE
              FROM RL_MR_PLANES_ACCION
             WHERE MRPA_MATRIZ_ID = :matrizId
             ORDER BY CASE MRPA_ESTADO WHEN 'PENDIENTE' THEN 1 WHEN 'EN_PROCESO' THEN 2 WHEN 'VENCIDO' THEN 3 WHEN 'CERRADO' THEN 4 ELSE 5 END,
                      MRPA_FECHA_CREACION DESC, MRPA_ID DESC";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var fechaFin = reader["MRPA_FECHA_FIN"] == DBNull.Value ? (DateTime?)null : ToDate(reader["MRPA_FECHA_FIN"]);
            var estado = reader["MRPA_ESTADO"].ToString() ?? string.Empty;
            result.Add(new MatrizRiesgoPlanAccionDto
            {
                PlanId = ToLong(reader["MRPA_ID"]),
                MatrizId = ToLong(reader["MRPA_MATRIZ_ID"]),
                ResultadoId = reader["MRPA_RESULTADO_ID"] == DBNull.Value ? null : ToLong(reader["MRPA_RESULTADO_ID"]),
                Actividad = reader["MRPA_ACTIVIDAD"].ToString() ?? string.Empty,
                Responsable = reader["MRPA_RESPONSABLE"].ToString() ?? string.Empty,
                Periodicidad = ToNullableString(reader["MRPA_PERIODICIDAD"]),
                FechaInicio = reader["MRPA_FECHA_INICIO"] == DBNull.Value ? null : ToDate(reader["MRPA_FECHA_INICIO"]),
                FechaFin = fechaFin,
                MedioPrueba = ToNullableString(reader["MRPA_MEDIO_PRUEBA"]),
                Observaciones = ToNullableString(reader["MRPA_OBSERVACIONES"]),
                Estado = estado,
                MotivoCierre = ToNullableString(reader["MRPA_MOTIVO_CIERRE"]),
                FechaCreacion = ToDate(reader["MRPA_FECHA_CREACION"]),
                FechaCierre = reader["MRPA_FECHA_CIERRE"] == DBNull.Value ? null : ToDate(reader["MRPA_FECHA_CIERRE"]),
                Vencido = fechaFin.HasValue && fechaFin.Value.Date < DateTime.Today && estado is not ("CERRADO" or "INACTIVO")
            });
        }
        return result;
    }

    private async Task<List<MatrizRiesgoEvidenciaDto>> ObtenerEvidenciasMatrizAsync(OracleConnection conn, OracleTransaction? tx, long matrizId)
    {
        var result = new List<MatrizRiesgoEvidenciaDto>();
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT MREV_ID, MREV_MATRIZ_ID, MREV_CONTROL_ID, MREV_PLAN_ID,
                   MREV_NOMBRE_ORIGINAL, MREV_NOMBRE_FISICO, MREV_TIPO_MIME,
                   MREV_EXTENSION, MREV_TAMANO_BYTES, MREV_RUTA_FISICA,
                   MREV_HASH_SHA256, MREV_ESTADO_REGISTRO, MREV_MOTIVO_INACTIVO,
                   MREV_FECHA_CREACION
              FROM RL_MR_EVIDENCIAS
             WHERE MREV_MATRIZ_ID = :matrizId
             ORDER BY MREV_ESTADO_REGISTRO DESC, MREV_FECHA_CREACION DESC, MREV_ID DESC";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(MapEvidencia(reader));
        return result;
    }

    private async Task<MatrizRiesgoPlanAccionDto?> ObtenerPlanAsync(OracleConnection conn, OracleTransaction? tx, long matrizId, long planId)
    {
        return (await ObtenerPlanesMatrizAsync(conn, tx, matrizId)).FirstOrDefault(x => x.PlanId == planId);
    }

    private async Task<bool> ExistePlanActivoMatrizAsync(OracleConnection conn, OracleTransaction tx, long matrizId, long planId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT COUNT(*)
              FROM RL_MR_PLANES_ACCION
             WHERE MRPA_ID = :planId
               AND MRPA_MATRIZ_ID = :matrizId
               AND MRPA_ESTADO <> 'INACTIVO'";
        cmd.Parameters.Add(Param("planId", planId));
        cmd.Parameters.Add(Param("matrizId", matrizId));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }

    private async Task<bool> ExistePlanDuplicadoAsync(OracleConnection conn, OracleTransaction tx, long matrizId, string actividad, string responsable, long? excluirPlanId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT COUNT(*)
              FROM RL_MR_PLANES_ACCION
             WHERE MRPA_MATRIZ_ID = :matrizId
               AND MRPA_ESTADO <> 'INACTIVO'
               AND UPPER(TRIM(MRPA_ACTIVIDAD)) = UPPER(TRIM(:actividad))
               AND UPPER(TRIM(MRPA_RESPONSABLE)) = UPPER(TRIM(:responsable))
               AND (:excluirPlanId IS NULL OR MRPA_ID <> :excluirPlanId)";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        cmd.Parameters.Add(Param("actividad", actividad));
        cmd.Parameters.Add(Param("responsable", responsable));
        cmd.Parameters.Add(Param("excluirPlanId", excluirPlanId));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }

    private async Task<bool> ExisteResultadoMatrizAsync(OracleConnection conn, OracleTransaction tx, long matrizId, long resultadoId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT COUNT(*)
              FROM RL_MR_RESULTADOS
             WHERE MRR_ID = :resultadoId
               AND MRR_MATRIZ_ID = :matrizId";
        cmd.Parameters.Add(Param("resultadoId", resultadoId));
        cmd.Parameters.Add(Param("matrizId", matrizId));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }

    private async Task<bool> ExisteControlMatrizAsync(OracleConnection conn, OracleTransaction tx, long matrizId, long controlId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT COUNT(*)
              FROM RL_MR_CONTROLES
             WHERE MRCTRL_ID = :controlId
               AND MRCTRL_MATRIZ_ID = :matrizId";
        cmd.Parameters.Add(Param("controlId", controlId));
        cmd.Parameters.Add(Param("matrizId", matrizId));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }

    private async Task<string?> ObtenerPlanAuditoriaAsync(OracleConnection conn, OracleTransaction tx, long matrizId, long planId)
    {
        var plan = await ObtenerPlanAsync(conn, tx, matrizId, planId);
        return plan == null ? null : JsonConvert.SerializeObject(plan);
    }

    private async Task<MatrizRiesgoEvidenciaDto?> ObtenerEvidenciaAsync(OracleConnection conn, OracleTransaction? tx, long matrizId, long evidenciaId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT MREV_ID, MREV_MATRIZ_ID, MREV_CONTROL_ID, MREV_PLAN_ID,
                   MREV_NOMBRE_ORIGINAL, MREV_NOMBRE_FISICO, MREV_TIPO_MIME,
                   MREV_EXTENSION, MREV_TAMANO_BYTES, MREV_RUTA_FISICA,
                   MREV_HASH_SHA256, MREV_ESTADO_REGISTRO, MREV_MOTIVO_INACTIVO,
                   MREV_FECHA_CREACION
              FROM RL_MR_EVIDENCIAS
             WHERE MREV_ID = :evidenciaId
               AND MREV_MATRIZ_ID = :matrizId";
        cmd.Parameters.Add(Param("evidenciaId", evidenciaId));
        cmd.Parameters.Add(Param("matrizId", matrizId));
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapEvidencia(reader) : null;
    }

    private static MatrizRiesgoEvidenciaDto MapEvidencia(OracleDataReader reader)
    {
        return new MatrizRiesgoEvidenciaDto
        {
            EvidenciaId = ToLong(reader["MREV_ID"]),
            MatrizId = ToLong(reader["MREV_MATRIZ_ID"]),
            ControlId = reader["MREV_CONTROL_ID"] == DBNull.Value ? null : ToLong(reader["MREV_CONTROL_ID"]),
            PlanId = reader["MREV_PLAN_ID"] == DBNull.Value ? null : ToLong(reader["MREV_PLAN_ID"]),
            NombreOriginal = reader["MREV_NOMBRE_ORIGINAL"].ToString() ?? string.Empty,
            NombreFisico = reader["MREV_NOMBRE_FISICO"].ToString() ?? string.Empty,
            TipoMime = ToNullableString(reader["MREV_TIPO_MIME"]),
            Extension = ToNullableString(reader["MREV_EXTENSION"]),
            TamanoBytes = ToLong(reader["MREV_TAMANO_BYTES"]),
            RutaFisica = reader["MREV_RUTA_FISICA"].ToString() ?? string.Empty,
            HashSha256 = ToNullableString(reader["MREV_HASH_SHA256"]),
            Activa = ToInt(reader["MREV_ESTADO_REGISTRO"]) == 1,
            MotivoInactivo = ToNullableString(reader["MREV_MOTIVO_INACTIVO"]),
            FechaCreacion = ToDate(reader["MREV_FECHA_CREACION"])
        };
    }

    private async Task<List<MatrizRiesgoControlDto>> ObtenerControlesMatrizAsync(OracleConnection conn, OracleTransaction? tx, long matrizId)
    {
        var result = new List<MatrizRiesgoControlDto>();
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT c.MRCTRL_ID, c.MRCTRL_FACTOR_ID, f.MRF_CODIGO, c.MRCTRL_NOMBRE, c.MRCTRL_DESCRIPCION,
                   c.MRCTRL_PERIODICIDAD, c.MRCTRL_OPORTUNIDAD, c.MRCTRL_AUTOMATIZACION,
                   c.MRCTRL_PROCEDIMIENTOS, c.MRCTRL_CALIDAD, c.MRCTRL_EFECTIVIDAD_PCT,
                   c.MRCTRL_RESPONSABLE, c.MRCTRL_ESTADO, c.MRCTRL_EVIDENCIA_OBL,
                   CASE WHEN EXISTS (
                        SELECT 1 FROM RL_MR_EVIDENCIAS e
                         WHERE e.MREV_CONTROL_ID = c.MRCTRL_ID
                           AND e.MREV_ESTADO_REGISTRO = 1
                   ) THEN 1 ELSE 0 END TIENE_EVIDENCIA
              FROM RL_MR_CONTROLES c
              LEFT JOIN RL_MR_FACTORES f ON f.MRF_ID = c.MRCTRL_FACTOR_ID
             WHERE c.MRCTRL_MATRIZ_ID = :matrizId
             ORDER BY c.MRCTRL_ID";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoControlDto
            {
                ControlId = ToLong(reader["MRCTRL_ID"]),
                FactorId = reader["MRCTRL_FACTOR_ID"] == DBNull.Value ? null : ToLong(reader["MRCTRL_FACTOR_ID"]),
                FactorCodigo = ToNullableString(reader["MRF_CODIGO"]),
                Nombre = reader["MRCTRL_NOMBRE"].ToString() ?? string.Empty,
                Descripcion = ToNullableString(reader["MRCTRL_DESCRIPCION"]),
                Periodicidad = ToNullableString(reader["MRCTRL_PERIODICIDAD"]),
                Oportunidad = ToNullableString(reader["MRCTRL_OPORTUNIDAD"]),
                Automatizacion = ToNullableString(reader["MRCTRL_AUTOMATIZACION"]),
                Procedimientos = ToNullableString(reader["MRCTRL_PROCEDIMIENTOS"]),
                Calidad = ToNullableString(reader["MRCTRL_CALIDAD"]),
                EfectividadPct = ToDecimal(reader["MRCTRL_EFECTIVIDAD_PCT"]),
                Responsable = ToNullableString(reader["MRCTRL_RESPONSABLE"]),
                Estado = reader["MRCTRL_ESTADO"].ToString() ?? string.Empty,
                EvidenciaObligatoria = ToInt(reader["MRCTRL_EVIDENCIA_OBL"]) == 1,
                TieneEvidencia = ToInt(reader["TIENE_EVIDENCIA"]) == 1
            });
        }
        return result;
    }

    private async Task<List<MatrizRiesgoResultadoPersistidoDto>> ObtenerResultadosMatrizAsync(OracleConnection conn, OracleTransaction? tx, long matrizId)
    {
        var result = new List<MatrizRiesgoResultadoPersistidoDto>();
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT MRR_ID, MRR_FACTOR_ID, MRR_TIPO_RESULTADO, MRR_VERSION_CALCULO,
                   MRR_ES_VIGENTE, MRR_PUNTAJE_INHERENTE, MRR_NIVEL_INHERENTE,
                   MRR_MITIGACION_PCT, MRR_PUNTAJE_RESIDUAL, MRR_NIVEL_RESIDUAL,
                   MRR_REQUIERE_PLAN, MRR_MOTIVO_RECALCULO, MRR_FECHA_CALCULO
              FROM RL_MR_RESULTADOS
             WHERE MRR_MATRIZ_ID = :matrizId
             ORDER BY MRR_ES_VIGENTE DESC, MRR_FECHA_CALCULO DESC, MRR_ID DESC";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoResultadoPersistidoDto
            {
                ResultadoId = ToLong(reader["MRR_ID"]),
                FactorId = reader["MRR_FACTOR_ID"] == DBNull.Value ? null : ToLong(reader["MRR_FACTOR_ID"]),
                TipoResultado = reader["MRR_TIPO_RESULTADO"].ToString() ?? string.Empty,
                VersionCalculo = reader["MRR_VERSION_CALCULO"].ToString() ?? string.Empty,
                EsVigente = ToInt(reader["MRR_ES_VIGENTE"]) == 1,
                PuntajeInherente = ToDecimal(reader["MRR_PUNTAJE_INHERENTE"]),
                NivelInherente = reader["MRR_NIVEL_INHERENTE"].ToString() ?? string.Empty,
                MitigacionPct = ToDecimal(reader["MRR_MITIGACION_PCT"]),
                PuntajeResidual = ToDecimal(reader["MRR_PUNTAJE_RESIDUAL"]),
                NivelResidual = reader["MRR_NIVEL_RESIDUAL"].ToString() ?? string.Empty,
                RequierePlanAccion = ToInt(reader["MRR_REQUIERE_PLAN"]) == 1,
                MotivoRecalculo = ToNullableString(reader["MRR_MOTIVO_RECALCULO"]),
                FechaCalculo = ToDate(reader["MRR_FECHA_CALCULO"])
            });
        }
        return result;
    }

    private async Task InsertarDetalleAsync(OracleConnection conn, OracleTransaction tx, long matrizId, long modeloId, MatrizRiesgoDetalleRequestDto detalle, long usuarioId)
    {
        var variable = await ObtenerVariableSnapshotAsync(conn, tx, modeloId, detalle.VariableId)
            ?? throw new InvalidOperationException($"La variable {detalle.VariableId} no pertenece a la metodología vigente.");

        var detalleId = await NextValAsync(conn, tx, "SEQ_RL_MR_DETALLE");
        var ponderado = Math.Round(detalle.Puntaje * variable.PesoInterno / 100m, 4, MidpointRounding.AwayFromZero);
        var snapshot = JsonConvert.SerializeObject(variable);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO RL_MR_DETALLE (
                MRD_ID, MRD_MATRIZ_ID, MRD_VARIABLE_ID, MRD_VALOR_CAPTURADO,
                MRD_PUNTAJE, MRD_PESO_SNAPSHOT, MRD_PUNTAJE_PONDERADO,
                MRD_JUSTIFICACION, MRD_FUENTE_DATO, MRD_SNAPSHOT_VARIABLE,
                MRD_USR_CREACION_ID, MRD_FECHA_CREACION
            ) VALUES (
                :id, :matrizId, :variableId, :valorCapturado,
                :puntaje, :pesoSnapshot, :puntajePonderado,
                :justificacion, :fuenteDato, :snapshotVariable,
                :usuarioId, SYSDATE
            )";
        cmd.Parameters.Add(Param("id", detalleId));
        cmd.Parameters.Add(Param("matrizId", matrizId));
        cmd.Parameters.Add(Param("variableId", detalle.VariableId));
        cmd.Parameters.Add(Param("valorCapturado", detalle.ValorCapturado));
        cmd.Parameters.Add(Param("puntaje", detalle.Puntaje));
        cmd.Parameters.Add(Param("pesoSnapshot", variable.PesoInterno));
        cmd.Parameters.Add(Param("puntajePonderado", ponderado));
        cmd.Parameters.Add(Param("justificacion", detalle.Justificacion));
        cmd.Parameters.Add(Param("fuenteDato", string.IsNullOrWhiteSpace(detalle.FuenteDato) ? "CAPTURA" : detalle.FuenteDato.Trim().ToUpperInvariant()));
        cmd.Parameters.Add(ClobParam("snapshotVariable", snapshot));
        cmd.Parameters.Add(Param("usuarioId", usuarioId));
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task GuardarDetalleAsync(OracleConnection conn, OracleTransaction tx, long matrizId, long modeloId, MatrizRiesgoDetalleRequestDto detalle, long usuarioId)
    {
        var variable = await ObtenerVariableSnapshotAsync(conn, tx, modeloId, detalle.VariableId)
            ?? throw new InvalidOperationException($"La variable {detalle.VariableId} no pertenece a la metodología vigente.");

        var ponderado = Math.Round(detalle.Puntaje * variable.PesoInterno / 100m, 4, MidpointRounding.AwayFromZero);
        var snapshot = JsonConvert.SerializeObject(variable);

        await using (var update = conn.CreateCommand())
        {
            update.BindByName = true;
            update.Transaction = tx;
            update.CommandText = @"
                UPDATE RL_MR_DETALLE
                   SET MRD_VALOR_CAPTURADO = :valorCapturado,
                       MRD_PUNTAJE = :puntaje,
                       MRD_PESO_SNAPSHOT = :pesoSnapshot,
                       MRD_PUNTAJE_PONDERADO = :puntajePonderado,
                       MRD_JUSTIFICACION = :justificacion,
                       MRD_FUENTE_DATO = :fuenteDato,
                       MRD_SNAPSHOT_VARIABLE = :snapshotVariable
                 WHERE MRD_MATRIZ_ID = :matrizId
                   AND MRD_VARIABLE_ID = :variableId";
            update.Parameters.Add(Param("valorCapturado", detalle.ValorCapturado));
            update.Parameters.Add(Param("puntaje", detalle.Puntaje));
            update.Parameters.Add(Param("pesoSnapshot", variable.PesoInterno));
            update.Parameters.Add(Param("puntajePonderado", ponderado));
            update.Parameters.Add(Param("justificacion", detalle.Justificacion));
            update.Parameters.Add(Param("fuenteDato", string.IsNullOrWhiteSpace(detalle.FuenteDato) ? "CAPTURA" : detalle.FuenteDato.Trim().ToUpperInvariant()));
            update.Parameters.Add(ClobParam("snapshotVariable", snapshot));
            update.Parameters.Add(Param("matrizId", matrizId));
            update.Parameters.Add(Param("variableId", detalle.VariableId));

            if (await update.ExecuteNonQueryAsync() > 0)
                return;
        }

        await InsertarDetalleAsync(conn, tx, matrizId, modeloId, detalle, usuarioId);
    }

    private async Task InsertarControlAsync(OracleConnection conn, OracleTransaction tx, long matrizId, long modeloId, MatrizRiesgoControlRequestDto control, long usuarioId)
    {
        if (control.FactorId.HasValue && !await FactorPerteneceModeloAsync(conn, tx, modeloId, control.FactorId.Value))
            throw new InvalidOperationException($"El factor {control.FactorId.Value} no pertenece a la metodología vigente.");

        var controlId = await NextValAsync(conn, tx, "SEQ_RL_MR_CONTROLES");
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO RL_MR_CONTROLES (
                MRCTRL_ID, MRCTRL_MATRIZ_ID, MRCTRL_FACTOR_ID, MRCTRL_NOMBRE,
                MRCTRL_DESCRIPCION, MRCTRL_PERIODICIDAD, MRCTRL_OPORTUNIDAD,
                MRCTRL_AUTOMATIZACION, MRCTRL_PROCEDIMIENTOS, MRCTRL_CALIDAD,
                MRCTRL_EFECTIVIDAD_PCT, MRCTRL_RESPONSABLE, MRCTRL_ESTADO,
                MRCTRL_EVIDENCIA_OBL, MRCTRL_USR_CREACION_ID, MRCTRL_FECHA_CREACION
            ) VALUES (
                :id, :matrizId, :factorId, :nombre,
                :descripcion, :periodicidad, :oportunidad,
                :automatizacion, :procedimientos, :calidad,
                :efectividadPct, :responsable, 'ACTIVO',
                :evidenciaObl, :usuarioId, SYSDATE
            )";
        cmd.Parameters.Add(Param("id", controlId));
        cmd.Parameters.Add(Param("matrizId", matrizId));
        cmd.Parameters.Add(Param("factorId", control.FactorId));
        cmd.Parameters.Add(Param("nombre", control.Nombre.Trim()));
        cmd.Parameters.Add(Param("descripcion", control.Descripcion));
        cmd.Parameters.Add(Param("periodicidad", control.Periodicidad));
        cmd.Parameters.Add(Param("oportunidad", control.Oportunidad));
        cmd.Parameters.Add(Param("automatizacion", control.Automatizacion));
        cmd.Parameters.Add(Param("procedimientos", control.Procedimientos));
        cmd.Parameters.Add(Param("calidad", control.Calidad));
        cmd.Parameters.Add(Param("efectividadPct", control.EfectividadPct));
        cmd.Parameters.Add(Param("responsable", control.Responsable));
        cmd.Parameters.Add(Param("evidenciaObl", control.EvidenciaObligatoria ? 1 : 0));
        cmd.Parameters.Add(Param("usuarioId", usuarioId));
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<VariableSnapshot?> ObtenerVariableSnapshotAsync(OracleConnection conn, OracleTransaction tx, long modeloId, long variableId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT v.MRV_ID, v.MRV_CODIGO, v.MRV_NOMBRE, v.MRV_PESO_INTERNO,
                   f.MRF_ID, f.MRF_CODIGO, f.MRF_NOMBRE
              FROM RL_MR_VARIABLES v
              JOIN RL_MR_FACTORES f ON f.MRF_ID = v.MRV_FACTOR_ID
             WHERE v.MRV_ID = :variableId
               AND f.MRF_MODELO_ID = :modeloId
               AND v.MRV_ESTADO_REGISTRO = 1
               AND f.MRF_ESTADO_REGISTRO = 1";
        cmd.Parameters.Add(Param("variableId", variableId));
        cmd.Parameters.Add(Param("modeloId", modeloId));
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new VariableSnapshot
        {
            VariableId = ToLong(reader["MRV_ID"]),
            Codigo = reader["MRV_CODIGO"].ToString() ?? string.Empty,
            Nombre = reader["MRV_NOMBRE"].ToString() ?? string.Empty,
            PesoInterno = ToDecimal(reader["MRV_PESO_INTERNO"]),
            FactorId = ToLong(reader["MRF_ID"]),
            FactorCodigo = reader["MRF_CODIGO"].ToString() ?? string.Empty,
            FactorNombre = reader["MRF_NOMBRE"].ToString() ?? string.Empty
        };
    }

    private async Task<bool> FactorPerteneceModeloAsync(OracleConnection conn, OracleTransaction tx, long modeloId, long factorId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT COUNT(*) FROM RL_MR_FACTORES WHERE MRF_ID = :factorId AND MRF_MODELO_ID = :modeloId AND MRF_ESTADO_REGISTRO = 1";
        cmd.Parameters.Add(Param("factorId", factorId));
        cmd.Parameters.Add(Param("modeloId", modeloId));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }

    private async Task InactivarResultadosVigentesAsync(OracleConnection conn, OracleTransaction tx, long matrizId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE RL_MR_RESULTADOS SET MRR_ES_VIGENTE = 0 WHERE MRR_MATRIZ_ID = :matrizId AND MRR_ES_VIGENTE = 1";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<long> InsertarResultadoAsync(OracleConnection conn, OracleTransaction tx, long matrizId, long? factorId, string tipoResultado, string versionCalculo, decimal puntajeInherente, string nivelInherente, decimal mitigacionPct, decimal puntajeResidual, string nivelResidual, bool requierePlan, string? motivoRecalculo, long? resultadoAnteriorId, string snapshot, long usuarioId)
    {
        var resultadoId = await NextValAsync(conn, tx, "SEQ_RL_MR_RESULTADOS");
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO RL_MR_RESULTADOS (
                MRR_ID, MRR_MATRIZ_ID, MRR_FACTOR_ID, MRR_TIPO_RESULTADO,
                MRR_VERSION_CALCULO, MRR_ES_VIGENTE, MRR_PUNTAJE_INHERENTE,
                MRR_NIVEL_INHERENTE, MRR_MITIGACION_PCT, MRR_PUNTAJE_RESIDUAL,
                MRR_NIVEL_RESIDUAL, MRR_REQUIERE_PLAN, MRR_MOTIVO_RECALCULO,
                MRR_RESULTADO_ANTERIOR_ID, MRR_SNAPSHOT_CALCULO, MRR_FECHA_CALCULO,
                MRR_USR_CALCULO_ID
            ) VALUES (
                :id, :matrizId, :factorId, :tipoResultado,
                :versionCalculo, 1, :puntajeInherente,
                :nivelInherente, :mitigacionPct, :puntajeResidual,
                :nivelResidual, :requierePlan, :motivoRecalculo,
                :resultadoAnteriorId, :snapshotCalculo, SYSDATE,
                :usuarioId
            )";
        cmd.Parameters.Add(Param("id", resultadoId));
        cmd.Parameters.Add(Param("matrizId", matrizId));
        cmd.Parameters.Add(Param("factorId", factorId));
        cmd.Parameters.Add(Param("tipoResultado", tipoResultado));
        cmd.Parameters.Add(Param("versionCalculo", versionCalculo));
        cmd.Parameters.Add(Param("puntajeInherente", puntajeInherente));
        cmd.Parameters.Add(Param("nivelInherente", nivelInherente));
        cmd.Parameters.Add(Param("mitigacionPct", mitigacionPct));
        cmd.Parameters.Add(Param("puntajeResidual", puntajeResidual));
        cmd.Parameters.Add(Param("nivelResidual", nivelResidual));
        cmd.Parameters.Add(Param("requierePlan", requierePlan ? 1 : 0));
        cmd.Parameters.Add(Param("motivoRecalculo", motivoRecalculo));
        cmd.Parameters.Add(Param("resultadoAnteriorId", resultadoAnteriorId));
        cmd.Parameters.Add(ClobParam("snapshotCalculo", snapshot));
        cmd.Parameters.Add(Param("usuarioId", usuarioId));
        await cmd.ExecuteNonQueryAsync();
        return resultadoId;
    }

    private async Task<long> ObtenerFactorIdPorCodigoAsync(OracleConnection conn, OracleTransaction tx, long modeloId, string codigo)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT MRF_ID FROM RL_MR_FACTORES WHERE MRF_MODELO_ID = :modeloId AND MRF_CODIGO = :codigo AND MRF_ESTADO_REGISTRO = 1";
        cmd.Parameters.Add(Param("modeloId", modeloId));
        cmd.Parameters.Add(Param("codigo", codigo));
        var value = await cmd.ExecuteScalarAsync();
        if (value == null || value == DBNull.Value)
            throw new InvalidOperationException($"No se encontró el factor {codigo} en la metodología vigente.");
        return ToLong(value);
    }

    private async Task<MatricesRiesgoReporteTotalesDto> ObtenerTotalesReporteAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro)
    {
        var where = new List<string> { "m.MRMAT_ESTADO_REGISTRO = 1" };
        var parameters = new List<OracleParameter>();
        AgregarFiltrosReporte(filtro, where, parameters);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT COUNT(*) TOTAL,
                   COUNT(DISTINCT CASE WHEN ri.MRR_ID IS NOT NULL THEN m.MRMAT_ID END) CALCULADAS,
                   COUNT(DISTINCT CASE WHEN m.MRMAT_ESTADO = 'CERRADA' THEN m.MRMAT_ID END) CERRADAS,
                   SUM(CASE WHEN UPPER(NVL(ri.MRR_NIVEL_RESIDUAL, '')) IN ('ALTO','CRITICO','CRÍTICO') THEN 1 ELSE 0 END) ALTO_CRITICO,
                   SUM(CASE WHEN ri.MRR_REQUIERE_PLAN = 1 THEN 1 ELSE 0 END) PLAN_REQUERIDO,
                   0 PLANES_VENCIDOS
              FROM RL_MR_MATRICES m
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              LEFT JOIN RL_MR_RESULTADOS ri
                ON ri.MRR_MATRIZ_ID = m.MRMAT_ID
               AND ri.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
               AND ri.MRR_ES_VIGENTE = 1
             WHERE {string.Join(" AND ", where)}";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return new MatricesRiesgoReporteTotalesDto();

        return new MatricesRiesgoReporteTotalesDto
        {
            TotalMatrices = ToInt(reader["TOTAL"]),
            TotalCalculadas = ToInt(reader["CALCULADAS"]),
            TotalCerradas = ToInt(reader["CERRADAS"]),
            TotalAltoCritico = ToInt(reader["ALTO_CRITICO"]),
            TotalPlanAccionRequerido = ToInt(reader["PLAN_REQUERIDO"]),
            TotalPlanesVencidos = await ObtenerTotalPlanesVencidosReporteAsync(conn, filtro)
        };
    }

    private async Task<int> ObtenerTotalPlanesVencidosReporteAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro)
    {
        var where = new List<string>
        {
            "m.MRMAT_ESTADO_REGISTRO = 1",
            "pa.MRPA_ESTADO <> 'CERRADO'",
            "pa.MRPA_FECHA_FIN < TRUNC(SYSDATE)"
        };
        var parameters = new List<OracleParameter>();
        AgregarFiltrosReporte(filtro, where, parameters);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT COUNT(*)
              FROM RL_MR_PLANES_ACCION pa
              JOIN RL_MR_MATRICES m ON m.MRMAT_ID = pa.MRPA_MATRIZ_ID
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              LEFT JOIN RL_MR_RESULTADOS ri
                ON ri.MRR_MATRIZ_ID = m.MRMAT_ID
               AND ri.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
               AND ri.MRR_ES_VIGENTE = 1
             WHERE {string.Join(" AND ", where)}";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        return ToInt(await cmd.ExecuteScalarAsync());
    }

    private async Task<List<MatrizRiesgoConteoDto>> ObtenerConteosReporteAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro, string expresionNombre)
    {
        var where = new List<string> { "m.MRMAT_ESTADO_REGISTRO = 1" };
        var parameters = new List<OracleParameter>();
        AgregarFiltrosReporte(filtro, where, parameters);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT {expresionNombre} NOMBRE, COUNT(*) TOTAL
              FROM RL_MR_MATRICES m
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              LEFT JOIN RL_MR_RESULTADOS ri
                ON ri.MRR_MATRIZ_ID = m.MRMAT_ID
               AND ri.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
               AND ri.MRR_ES_VIGENTE = 1
             WHERE {string.Join(" AND ", where)}
             GROUP BY {expresionNombre}
             ORDER BY NOMBRE";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        return await LeerConteosAsync(cmd);
    }

    private async Task<List<MatrizRiesgoFactorReporteDto>> ObtenerFactoresReporteAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro)
    {
        var where = new List<string> { "m.MRMAT_ESTADO_REGISTRO = 1", "rf.MRR_ES_VIGENTE = 1", "rf.MRR_TIPO_RESULTADO = 'FACTOR'" };
        var parameters = new List<OracleParameter>();
        AgregarFiltrosReporte(filtro, where, parameters);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT f.MRF_ID, f.MRF_CODIGO, f.MRF_NOMBRE,
                   COUNT(DISTINCT m.MRMAT_ID) TOTAL_MATRICES,
                   ROUND(AVG(NVL(rf.MRR_PUNTAJE_INHERENTE, 0)), 4) PROM_INHERENTE,
                   ROUND(AVG(NVL(rf.MRR_PUNTAJE_RESIDUAL, 0)), 4) PROM_RESIDUAL,
                   SUM(CASE WHEN UPPER(NVL(rf.MRR_NIVEL_RESIDUAL, '')) IN ('ALTO','CRITICO','CRÍTICO') THEN 1 ELSE 0 END) ALTO_CRITICO,
                   SUM(CASE WHEN rf.MRR_REQUIERE_PLAN = 1 THEN 1 ELSE 0 END) PLAN_REQUERIDO
              FROM RL_MR_MATRICES m
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              LEFT JOIN RL_MR_RESULTADOS ri
                ON ri.MRR_MATRIZ_ID = m.MRMAT_ID
               AND ri.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
               AND ri.MRR_ES_VIGENTE = 1
              JOIN RL_MR_RESULTADOS rf
                ON rf.MRR_MATRIZ_ID = m.MRMAT_ID
              JOIN RL_MR_FACTORES f
                ON f.MRF_ID = rf.MRR_FACTOR_ID
             WHERE {string.Join(" AND ", where)}
             GROUP BY f.MRF_ID, f.MRF_CODIGO, f.MRF_NOMBRE
             ORDER BY f.MRF_CODIGO";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        var result = new List<MatrizRiesgoFactorReporteDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoFactorReporteDto
            {
                FactorId = ToLong(reader["MRF_ID"]),
                FactorCodigo = reader["MRF_CODIGO"].ToString() ?? string.Empty,
                FactorNombre = reader["MRF_NOMBRE"].ToString() ?? string.Empty,
                TotalMatrices = ToInt(reader["TOTAL_MATRICES"]),
                PromedioInherente = ToDecimal(reader["PROM_INHERENTE"]),
                PromedioResidual = ToDecimal(reader["PROM_RESIDUAL"]),
                TotalAltoCritico = ToInt(reader["ALTO_CRITICO"]),
                TotalPlanAccionRequerido = ToInt(reader["PLAN_REQUERIDO"])
            });
        }

        return result;
    }

    private async Task<List<MatrizRiesgoMapaNivelDto>> ObtenerMapaNivelReporteAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro, string tipo)
    {
        var nivelCol = tipo.Equals("INHERENTE", StringComparison.OrdinalIgnoreCase) ? "ri.MRR_NIVEL_INHERENTE" : "ri.MRR_NIVEL_RESIDUAL";
        var puntajeCol = tipo.Equals("INHERENTE", StringComparison.OrdinalIgnoreCase) ? "ri.MRR_PUNTAJE_INHERENTE" : "ri.MRR_PUNTAJE_RESIDUAL";
        var where = new List<string> { "m.MRMAT_ESTADO_REGISTRO = 1", "ri.MRR_ES_VIGENTE = 1", "ri.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'" };
        var parameters = new List<OracleParameter>();
        AgregarFiltrosReporte(filtro, where, parameters);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT NVL({nivelCol}, 'SIN_CALCULO') NIVEL,
                   COUNT(*) TOTAL,
                   ROUND(AVG(NVL({puntajeCol}, 0)), 4) PROMEDIO
              FROM RL_MR_MATRICES m
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              JOIN RL_MR_RESULTADOS ri
                ON ri.MRR_MATRIZ_ID = m.MRMAT_ID
             WHERE {string.Join(" AND ", where)}
             GROUP BY NVL({nivelCol}, 'SIN_CALCULO')
             ORDER BY MIN(NVL({puntajeCol}, 0))";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        var result = new List<MatrizRiesgoMapaNivelDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoMapaNivelDto
            {
                Nivel = reader["NIVEL"].ToString() ?? string.Empty,
                Total = ToInt(reader["TOTAL"]),
                Promedio = ToDecimal(reader["PROMEDIO"])
            });
        }

        return result;
    }

    private async Task<List<MatrizRiesgoResumenDto>> ObtenerMatricesCriticasReporteAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro)
    {
        var where = new List<string>
        {
            "m.MRMAT_ESTADO_REGISTRO = 1",
            "UPPER(NVL(ri.MRR_NIVEL_RESIDUAL, '')) IN ('ALTO','CRITICO','CRÍTICO')"
        };
        var parameters = new List<OracleParameter>();
        AgregarFiltrosReporte(filtro, where, parameters);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT *
              FROM (
                    SELECT m.MRMAT_ID, m.MRMAT_MODELO_ID, mo.MRM_VERSION, m.MRMAT_SUJETO_TIPO,
                           m.MRMAT_SUJETO_ID_EXT, m.MRMAT_DOCUMENTO, m.MRMAT_NOMBRE_SUJETO,
                           m.MRMAT_ESTADO, m.MRMAT_FECHA_EVALUACION,
                           ri.MRR_PUNTAJE_INHERENTE, ri.MRR_NIVEL_INHERENTE,
                           ri.MRR_PUNTAJE_RESIDUAL, ri.MRR_NIVEL_RESIDUAL, ri.MRR_REQUIERE_PLAN
                      FROM RL_MR_MATRICES m
                      JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
                      LEFT JOIN RL_MR_RESULTADOS ri
                        ON ri.MRR_MATRIZ_ID = m.MRMAT_ID
                       AND ri.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
                       AND ri.MRR_ES_VIGENTE = 1
                     WHERE {string.Join(" AND ", where)}
                     ORDER BY ri.MRR_PUNTAJE_RESIDUAL DESC, m.MRMAT_FECHA_EVALUACION DESC
                   )
             WHERE ROWNUM <= 25";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        var result = new List<MatrizRiesgoResumenDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(MapResumen(reader));

        return result;
    }

    private async Task<List<MatrizRiesgoResumenDto>> ObtenerMatricesFiltradasReporteAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro)
    {
        var where = new List<string> { "m.MRMAT_ESTADO_REGISTRO = 1" };
        var parameters = new List<OracleParameter>();
        AgregarFiltrosReporte(filtro, where, parameters);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT m.MRMAT_ID, m.MRMAT_MODELO_ID, mo.MRM_VERSION, m.MRMAT_SUJETO_TIPO,
                   m.MRMAT_SUJETO_ID_EXT, m.MRMAT_DOCUMENTO, m.MRMAT_NOMBRE_SUJETO,
                   m.MRMAT_ESTADO, m.MRMAT_FECHA_EVALUACION,
                   ri.MRR_PUNTAJE_INHERENTE, ri.MRR_NIVEL_INHERENTE,
                   ri.MRR_PUNTAJE_RESIDUAL, ri.MRR_NIVEL_RESIDUAL, ri.MRR_REQUIERE_PLAN
              FROM RL_MR_MATRICES m
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              LEFT JOIN RL_MR_RESULTADOS ri
                ON ri.MRR_MATRIZ_ID = m.MRMAT_ID
               AND ri.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
               AND ri.MRR_ES_VIGENTE = 1
             WHERE {string.Join(" AND ", where)}
             ORDER BY m.MRMAT_FECHA_EVALUACION DESC, m.MRMAT_ID DESC";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        var result = new List<MatrizRiesgoResumenDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(MapResumen(reader));

        return result;
    }

    private async Task<List<MatrizRiesgoPlanAccionReporteDto>> ObtenerPlanesAccionReporteAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro)
    {
        var where = new List<string> { "m.MRMAT_ESTADO_REGISTRO = 1" };
        var parameters = new List<OracleParameter>();
        AgregarFiltrosReporte(filtro, where, parameters);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT NVL(pa.MRPA_ESTADO, 'SIN_PLAN') ESTADO,
                   COUNT(pa.MRPA_ID) TOTAL,
                   SUM(CASE WHEN pa.MRPA_ESTADO <> 'CERRADO' AND pa.MRPA_FECHA_FIN < TRUNC(SYSDATE) THEN 1 ELSE 0 END) VENCIDOS
              FROM RL_MR_MATRICES m
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              LEFT JOIN RL_MR_RESULTADOS ri
                ON ri.MRR_MATRIZ_ID = m.MRMAT_ID
               AND ri.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
               AND ri.MRR_ES_VIGENTE = 1
              LEFT JOIN RL_MR_PLANES_ACCION pa
                ON pa.MRPA_MATRIZ_ID = m.MRMAT_ID
             WHERE {string.Join(" AND ", where)}
             GROUP BY NVL(pa.MRPA_ESTADO, 'SIN_PLAN')
             ORDER BY ESTADO";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        var result = new List<MatrizRiesgoPlanAccionReporteDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoPlanAccionReporteDto
            {
                Estado = reader["ESTADO"].ToString() ?? string.Empty,
                Total = ToInt(reader["TOTAL"]),
                Vencidos = ToInt(reader["VENCIDOS"])
            });
        }

        return result;
    }

    private static void AgregarFiltrosReporte(MatrizRiesgoReporteFiltroDto filtro, List<string> where, List<OracleParameter> parameters)
    {
        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            if (filtro.Estado.Trim().Equals("EN_REVISION", StringComparison.OrdinalIgnoreCase))
            {
                // Compatibilidad con matrices calculadas antes de simplificar estados operativos.
                where.Add("m.MRMAT_ESTADO IN ('EN_REVISION', 'CALCULADA')");
            }
            else
            {
                where.Add("m.MRMAT_ESTADO = :repEstado");
                parameters.Add(new OracleParameter("repEstado", filtro.Estado.Trim().ToUpperInvariant()));
            }
        }

        if (!string.IsNullOrWhiteSpace(filtro.SujetoTipo))
        {
            where.Add("m.MRMAT_SUJETO_TIPO = :repSujetoTipo");
            parameters.Add(new OracleParameter("repSujetoTipo", filtro.SujetoTipo.Trim().ToUpperInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(filtro.NivelResidual))
        {
            where.Add("TRANSLATE(UPPER(NVL(ri.MRR_NIVEL_RESIDUAL, '')), 'ÁÉÍÓÚ', 'AEIOU') = :repNivelResidual");
            parameters.Add(new OracleParameter("repNivelResidual", NormalizarTexto(filtro.NivelResidual)));
        }

        if (!string.IsNullOrWhiteSpace(filtro.ModeloVersion))
        {
            where.Add("mo.MRM_VERSION = :repModeloVersion");
            parameters.Add(new OracleParameter("repModeloVersion", filtro.ModeloVersion.Trim()));
        }

        if (filtro.FechaInicio.HasValue)
        {
            where.Add("m.MRMAT_FECHA_EVALUACION >= :repFechaInicio");
            parameters.Add(new OracleParameter("repFechaInicio", filtro.FechaInicio.Value.Date));
        }

        if (filtro.FechaFin.HasValue)
        {
            where.Add("m.MRMAT_FECHA_EVALUACION <= :repFechaFin");
            parameters.Add(new OracleParameter("repFechaFin", filtro.FechaFin.Value.Date.AddDays(1).AddSeconds(-1)));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            where.Add(@"(
                LOWER(m.MRMAT_NOMBRE_SUJETO) LIKE :repBuscar
                OR LOWER(NVL(m.MRMAT_DOCUMENTO, '')) LIKE :repBuscar
                OR LOWER(NVL(m.MRMAT_SUJETO_ID_EXT, '')) LIKE :repBuscar
                OR LOWER(NVL(m.MRMAT_ESTADO, '')) LIKE :repBuscar
                OR LOWER(NVL(m.MRMAT_SUJETO_TIPO, '')) LIKE :repBuscar
                OR LOWER(NVL(mo.MRM_VERSION, '')) LIKE :repBuscar
                OR LOWER(NVL(ri.MRR_NIVEL_INHERENTE, '')) LIKE :repBuscar
                OR LOWER(NVL(ri.MRR_NIVEL_RESIDUAL, '')) LIKE :repBuscar
                OR TO_CHAR(NVL(ri.MRR_PUNTAJE_INHERENTE, 0), 'FM9999990D9999', 'NLS_NUMERIC_CHARACTERS=.,') LIKE :repBuscar
                OR TO_CHAR(NVL(ri.MRR_PUNTAJE_RESIDUAL, 0), 'FM9999990D9999', 'NLS_NUMERIC_CHARACTERS=.,') LIKE :repBuscar
                OR EXISTS (
                    SELECT 1
                      FROM RL_MR_CONTROLES ctrl
                     WHERE ctrl.MRCTRL_MATRIZ_ID = m.MRMAT_ID
                       AND (
                            LOWER(NVL(ctrl.MRCTRL_RESPONSABLE, '')) LIKE :repBuscar
                            OR LOWER(NVL(ctrl.MRCTRL_NOMBRE, '')) LIKE :repBuscar
                            OR LOWER(NVL(ctrl.MRCTRL_DESCRIPCION, '')) LIKE :repBuscar
                       )
                )
                OR EXISTS (
                    SELECT 1
                      FROM RL_MR_RESULTADOS rf
                      JOIN RL_MR_FACTORES f ON f.MRF_ID = rf.MRR_FACTOR_ID
                     WHERE rf.MRR_MATRIZ_ID = m.MRMAT_ID
                       AND rf.MRR_TIPO_RESULTADO = 'FACTOR'
                       AND rf.MRR_ES_VIGENTE = 1
                       AND (
                            LOWER(NVL(f.MRF_CODIGO, '')) LIKE :repBuscar
                            OR LOWER(NVL(f.MRF_NOMBRE, '')) LIKE :repBuscar
                            OR LOWER(NVL(rf.MRR_NIVEL_RESIDUAL, '')) LIKE :repBuscar
                       )
                )
            )");
            parameters.Add(new OracleParameter("repBuscar", $"%{filtro.Buscar.Trim().ToLowerInvariant()}%"));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Responsable))
        {
            where.Add(@"EXISTS (
                SELECT 1
                  FROM RL_MR_CONTROLES ctrl
                 WHERE ctrl.MRCTRL_MATRIZ_ID = m.MRMAT_ID
                   AND LOWER(NVL(ctrl.MRCTRL_RESPONSABLE, '')) LIKE :repResponsable
            )");
            parameters.Add(new OracleParameter("repResponsable", $"%{filtro.Responsable.Trim().ToLowerInvariant()}%"));
        }
    }

    private static async Task<List<MatrizRiesgoConteoDto>> LeerConteosAsync(OracleCommand cmd)
    {
        var result = new List<MatrizRiesgoConteoDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoConteoDto
            {
                Nombre = reader["NOMBRE"].ToString() ?? string.Empty,
                Total = ToInt(reader["TOTAL"])
            });
        }
        return result;
    }

    private async Task<List<MatrizRiesgoConteoDto>> ObtenerConteosAsync(OracleConnection conn, string sql)
    {
        var result = new List<MatrizRiesgoConteoDto>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoConteoDto
            {
                Nombre = reader["NOMBRE"].ToString() ?? string.Empty,
                Total = ToInt(reader["TOTAL"])
            });
        }
        return result;
    }

    private async Task<bool> TienePlanTratadoParaCierreAsync(OracleConnection conn, OracleTransaction? tx, long matrizId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT COUNT(*)
              FROM RL_MR_PLANES_ACCION
             WHERE MRPA_MATRIZ_ID = :matrizId
               AND MRPA_ESTADO = 'CERRADO'
               AND TRIM(NVL(MRPA_MOTIVO_CIERRE, '')) IS NOT NULL";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }

    private async Task<bool> MatrizEstaCerradaOInactivaAsync(OracleConnection conn, OracleTransaction tx, long matrizId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT COUNT(*) FROM RL_MR_MATRICES WHERE MRMAT_ID = :matrizId AND MRMAT_ESTADO IN ('CERRADA','INACTIVA')";
        cmd.Parameters.Add(Param("matrizId", matrizId));
        return ToInt(await cmd.ExecuteScalarAsync()) > 0;
    }

    private async Task<long> NextValAsync(OracleConnection conn, OracleTransaction tx, string sequenceName)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"SELECT {sequenceName}.NEXTVAL FROM DUAL";
        return ToLong(await cmd.ExecuteScalarAsync());
    }

    private async Task RegistrarHistorialAsync(OracleConnection conn, OracleTransaction tx, long? matrizId, string tabla, string registroId, string accion, string? estadoAnterior, string? estadoNuevo, string? motivo, string? datosAnt, string? datosNvo, long usuarioId, string? usuarioEmail, string? ip)
    {
        var historialId = await NextValAsync(conn, tx, "SEQ_RL_MR_HISTORIAL");
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO RL_MR_HISTORIAL (
                MRH_ID, MRH_MATRIZ_ID, MRH_TABLA, MRH_REGISTRO_ID, MRH_ACCION,
                MRH_ESTADO_ANTERIOR, MRH_ESTADO_NUEVO, MRH_MOTIVO,
                MRH_DATOS_ANT, MRH_DATOS_NVO, MRH_USR_ID, MRH_USR_EMAIL,
                MRH_IP, MRH_FECHA
            ) VALUES (
                :id, :matrizId, :tabla, :registroId, :accion,
                :estadoAnterior, :estadoNuevo, :motivo,
                :datosAnt, :datosNvo, :usuarioId, :usuarioEmail,
                :ip, SYSDATE
            )";
        cmd.Parameters.Add(Param("id", historialId));
        cmd.Parameters.Add(Param("matrizId", matrizId));
        cmd.Parameters.Add(Param("tabla", tabla));
        cmd.Parameters.Add(Param("registroId", registroId));
        cmd.Parameters.Add(Param("accion", accion));
        cmd.Parameters.Add(Param("estadoAnterior", estadoAnterior));
        cmd.Parameters.Add(Param("estadoNuevo", estadoNuevo));
        cmd.Parameters.Add(Param("motivo", motivo));
        cmd.Parameters.Add(ClobParam("datosAnt", datosAnt));
        cmd.Parameters.Add(ClobParam("datosNvo", datosNvo));
        cmd.Parameters.Add(Param("usuarioId", usuarioId));
        cmd.Parameters.Add(Param("usuarioEmail", usuarioEmail));
        cmd.Parameters.Add(Param("ip", ip));
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task RegistrarAuditoriaAsync(OracleConnection conn, OracleTransaction tx, string tabla, string registroId, string accion, string? datosAnt, string? datosNvo, long usuarioId, string? usuarioEmail, string? ip)
    {
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO RL_AUDITORIA (
                AUD_ID, AUD_TABLA, AUD_REGISTRO_ID, AUD_ACCION,
                AUD_DATOS_ANT, AUD_DATOS_NVO, AUD_USR_ID, AUD_USR_EMAIL,
                AUD_IP, AUD_FECHA, AUD_MODULO
            ) VALUES (
                SEQ_RL_AUDITORIA.NEXTVAL, :tabla, :registroId, :accion,
                :datosAnt, :datosNvo, :usuarioId, :usuarioEmail,
                :ip, SYSDATE, :modulo
            )";
        cmd.Parameters.Add(Param("tabla", tabla));
        cmd.Parameters.Add(Param("registroId", registroId));
        cmd.Parameters.Add(Param("accion", accion));
        cmd.Parameters.Add(ClobParam("datosAnt", datosAnt));
        cmd.Parameters.Add(ClobParam("datosNvo", datosNvo));
        cmd.Parameters.Add(Param("usuarioId", usuarioId));
        cmd.Parameters.Add(Param("usuarioEmail", usuarioEmail));
        cmd.Parameters.Add(Param("ip", ip));
        cmd.Parameters.Add(Param("modulo", ModuloAuditoria));
        await cmd.ExecuteNonQueryAsync();
    }

    private static MatrizRiesgoResumenDto MapResumen(OracleDataReader reader)
    {
        return new MatrizRiesgoResumenDto
        {
            MatrizId = ToLong(reader["MRMAT_ID"]),
            ModeloId = ToLong(reader["MRMAT_MODELO_ID"]),
            ModeloVersion = reader["MRM_VERSION"].ToString() ?? string.Empty,
            SujetoTipo = reader["MRMAT_SUJETO_TIPO"].ToString() ?? string.Empty,
            SujetoIdExt = ToNullableString(reader["MRMAT_SUJETO_ID_EXT"]),
            Documento = ToNullableString(reader["MRMAT_DOCUMENTO"]),
            NombreSujeto = reader["MRMAT_NOMBRE_SUJETO"].ToString() ?? string.Empty,
            Estado = NormalizarEstadoFuncional(reader["MRMAT_ESTADO"].ToString()),
            FechaEvaluacion = ToDate(reader["MRMAT_FECHA_EVALUACION"]),
            PuntajeInherente = ToNullableDecimal(reader["MRR_PUNTAJE_INHERENTE"]),
            NivelInherente = ToNullableString(reader["MRR_NIVEL_INHERENTE"]),
            PuntajeResidual = ToNullableDecimal(reader["MRR_PUNTAJE_RESIDUAL"]),
            NivelResidual = ToNullableString(reader["MRR_NIVEL_RESIDUAL"]),
            RequierePlanAccion = ToInt(reader["MRR_REQUIERE_PLAN"]) == 1
        };
    }

    private static OracleParameter Param(string name, object? value)
    {
        return new OracleParameter(name, value ?? DBNull.Value);
    }

    private static OracleParameter ClobParam(string name, string? value)
    {
        return new OracleParameter(name, OracleDbType.Clob)
        {
            Value = string.IsNullOrEmpty(value) ? DBNull.Value : value
        };
    }

    private static string? ToNullableString(object value) => value == DBNull.Value ? null : value?.ToString();
    private static string NormalizarEstadoFuncional(string? estado)
    {
        var normalizado = (estado ?? string.Empty).Trim().ToUpperInvariant();
        return normalizado == "CALCULADA" ? "EN_REVISION" : normalizado;
    }

    private static int ToInt(object? value) => value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
    private static long ToLong(object? value) => value == null || value == DBNull.Value ? 0 : Convert.ToInt64(value);
    private static decimal ToDecimal(object value) => value == DBNull.Value ? 0m : Convert.ToDecimal(value);
    private static decimal? ToNullableDecimal(object value) => value == DBNull.Value ? null : Convert.ToDecimal(value);
    private static DateTime ToDate(object value) => Convert.ToDateTime(value);

    private static bool NivelResidualRequierePlanAccion(string? nivel)
    {
        var normalizado = NormalizarTexto(nivel);
        return normalizado == "ALTO" || normalizado == "CRITICO";
    }

    private static string NormalizarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
        var limpio = new StringBuilder(descompuesto.Length);

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
                limpio.Append(caracter);
        }

        return limpio.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    private sealed record FactorMetodologiaRow(long Id, string Codigo, string Nombre, decimal PesoInstitucional);

    private sealed class VariableSnapshot
    {
        public long VariableId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal PesoInterno { get; set; }
        public long FactorId { get; set; }
        public string FactorCodigo { get; set; } = string.Empty;
        public string FactorNombre { get; set; } = string.Empty;
    }
}
