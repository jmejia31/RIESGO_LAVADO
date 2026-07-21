from pathlib import Path
import re

root=Path(__file__).resolve().parents[1]
B=root/'backend/RL.API/Features/MatricesRiesgos'
F=root/'frontend/rl-app/src/app/features/admin/matrices-riesgos'

def read(p): return p.read_text(encoding='utf-8-sig')
def write(p,s): p.write_text(s,encoding='utf-8')
def rep(s,old,new,label):
    if old not in s:
        if new in s: return s
        raise RuntimeError(label)
    return s.replace(old,new,1)
def reg(s,pat,new,label):
    out,n=re.subn(pat,new,s,count=1,flags=re.S)
    if n!=1: raise RuntimeError(label)
    return out

p=B/'Persistence/MatricesRiesgosRepository.cs'; s=read(p)
pat=r'''    public async Task<MatricesRiesgoDashboardDto> ObtenerDashboardAsync\(\)\n    \{.*?\n    \}\n\n    public async Task<MatricesRiesgoReporteDto> ObtenerReporteAsync'''
new='''    public async Task<MatricesRiesgoDashboardDto> ObtenerDashboardAsync(MatrizRiesgoReporteFiltroDto filtro)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        filtro ??= new MatrizRiesgoReporteFiltroDto();
        var totales = await ObtenerTotalesReporteAsync(conn, filtro);
        var matricesFiltradas = await ObtenerMatricesDashboardAsync(conn, filtro);

        return new MatricesRiesgoDashboardDto
        {
            FechaGeneracion = DateTime.Now,
            Filtro = filtro,
            TotalMatrices = totales.TotalMatrices,
            TotalCalculadas = totales.TotalCalculadas,
            TotalCerradas = totales.TotalCerradas,
            TotalConPlanAccion = totales.TotalPlanAccionRequerido,
            TotalAltoCritico = totales.TotalAltoCritico,
            TotalPlanesVencidos = totales.TotalPlanesVencidos,
            PorEstado = await ObtenerConteosReporteAsync(conn, filtro, "CASE WHEN m.MRMAT_ESTADO = 'CALCULADA' THEN 'EN_REVISION' ELSE m.MRMAT_ESTADO END"),
            PorNivelInherente = await ObtenerConteosReporteAsync(conn, filtro, "NVL(ri.MRR_NIVEL_INHERENTE, 'SIN_CALCULO')"),
            PorNivelResidual = await ObtenerConteosReporteAsync(conn, filtro, "NVL(ri.MRR_NIVEL_RESIDUAL, 'SIN_CALCULO')"),
            MapaTransicion = await ObtenerMapaTransicionDashboardAsync(conn, filtro),
            MatricesCriticas = await ObtenerMatricesCriticasReporteAsync(conn, filtro),
            MatricesFiltradas = matricesFiltradas,
            PlanesAccion = await ObtenerPlanesAccionReporteAsync(conn, filtro)
        };
    }

    public async Task<MatricesRiesgoReporteDto> ObtenerReporteAsync'''
s=reg(s,pat,new,'repository dashboard')
old='''        if (!string.IsNullOrWhiteSpace(filtro.NivelResidual))
        {
            where.Add("TRANSLATE(UPPER(NVL(ri.MRR_NIVEL_RESIDUAL, '')), 'ÁÉÍÓÚ', 'AEIOU') = :repNivelResidual");
            parameters.Add(new OracleParameter("repNivelResidual", NormalizarTexto(filtro.NivelResidual)));
        }
'''
new='''        if (!string.IsNullOrWhiteSpace(filtro.NivelInherente))
        {
            where.Add("TRANSLATE(UPPER(NVL(ri.MRR_NIVEL_INHERENTE, '')), 'ÁÉÍÓÚ', 'AEIOU') = :repNivelInherente");
            parameters.Add(new OracleParameter("repNivelInherente", NormalizarTexto(filtro.NivelInherente)));
        }

        if (!string.IsNullOrWhiteSpace(filtro.NivelResidual))
        {
            where.Add("TRANSLATE(UPPER(NVL(ri.MRR_NIVEL_RESIDUAL, '')), 'ÁÉÍÓÚ', 'AEIOU') = :repNivelResidual");
            parameters.Add(new OracleParameter("repNivelResidual", NormalizarTexto(filtro.NivelResidual)));
        }
'''
s=rep(s,old,new,'repository inherent filter')
marker='    private async Task<List<MatrizRiesgoResumenDto>> ObtenerMatricesCriticasReporteAsync'
helpers='''    private async Task<List<MatrizRiesgoMapaTransicionDto>> ObtenerMapaTransicionDashboardAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro)
    {
        var where = new List<string>
        {
            "m.MRMAT_ESTADO_REGISTRO = 1",
            "ri.MRR_ES_VIGENTE = 1",
            "ri.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'"
        };
        var parameters = new List<OracleParameter>();
        AgregarFiltrosReporte(filtro, where, parameters);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT NVL(ri.MRR_NIVEL_INHERENTE, 'SIN_CALCULO') NIVEL_INHERENTE,
                   NVL(ri.MRR_NIVEL_RESIDUAL, 'SIN_CALCULO') NIVEL_RESIDUAL,
                   COUNT(*) TOTAL,
                   ROUND(AVG(NVL(ri.MRR_PUNTAJE_INHERENTE, 0)), 4) PROMEDIO_INHERENTE,
                   ROUND(AVG(NVL(ri.MRR_PUNTAJE_RESIDUAL, 0)), 4) PROMEDIO_RESIDUAL
              FROM RL_MR_MATRICES m
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              JOIN RL_MR_RESULTADOS ri
                ON ri.MRR_MATRIZ_ID = m.MRMAT_ID
             WHERE {string.Join(" AND ", where)}
             GROUP BY NVL(ri.MRR_NIVEL_INHERENTE, 'SIN_CALCULO'),
                      NVL(ri.MRR_NIVEL_RESIDUAL, 'SIN_CALCULO')
             ORDER BY MIN(ri.MRR_PUNTAJE_INHERENTE) DESC,
                      MIN(ri.MRR_PUNTAJE_RESIDUAL)";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        var result = new List<MatrizRiesgoMapaTransicionDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoMapaTransicionDto
            {
                NivelInherente = reader["NIVEL_INHERENTE"].ToString() ?? string.Empty,
                NivelResidual = reader["NIVEL_RESIDUAL"].ToString() ?? string.Empty,
                Total = ToInt(reader["TOTAL"]),
                PromedioInherente = ToDecimal(reader["PROMEDIO_INHERENTE"]),
                PromedioResidual = ToDecimal(reader["PROMEDIO_RESIDUAL"])
            });
        }

        return result;
    }

    private async Task<List<MatrizRiesgoResumenDto>> ObtenerMatricesDashboardAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro)
    {
        var where = new List<string> { "m.MRMAT_ESTADO_REGISTRO = 1" };
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
                     ORDER BY NVL(ri.MRR_PUNTAJE_RESIDUAL, -1) DESC,
                              m.MRMAT_FECHA_EVALUACION DESC,
                              m.MRMAT_ID DESC
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

'''
if marker not in s: raise RuntimeError('helper marker')
s=s.replace(marker,helpers+marker,1)
write(p,s)
