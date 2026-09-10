using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Listas.Contracts;
using RL.API.Infrastructure.Caching;
using RL.API.Infrastructure.Database;
using RL.API.Infrastructure.Http;
using RL.API.Shared.Results;

namespace RL.API.Features.Listas.Persistence
{
    public class ListasRepository : IListasRepository
    {
        private readonly OracleDbContext _db;
        private readonly IAuditoriaRepository _auditoriaRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IApplicationCache _cache;
        private readonly ApplicationCacheSettings _cacheSettings;

        public ListasRepository(
            OracleDbContext db,
            IAuditoriaRepository auditoriaRepo,
            IHttpContextAccessor httpContextAccessor,
            IApplicationCache cache,
            ApplicationCacheSettings cacheSettings)
        {
            _db = db;
            _auditoriaRepo = auditoriaRepo;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
            _cacheSettings = cacheSettings;
        }

        public Task<MonitoreoPaginadoDto<CoincidenciaJuridicaDto>> ObtenerJuridicasPaginadasAsync(ConsultaMonitoreoPaginadaDto consulta, CancellationToken cancellationToken = default)
            => ObtenerMonitoreoPaginadoAsync(
                consulta,
                ConstruirConsultaMonitoreoJuridicas(),
                "NOMBRE ASC, NUMEPATRO ASC",
                reader => new CoincidenciaJuridicaDto
                {
                    Rtn = Texto(reader, "RTN"),
                    Nombre = Texto(reader, "NOMBRE"),
                    NumeroPatrono = Texto(reader, "NUMEPATRO"),
                    ListaCoincidencia = Texto(reader, "LISTA_CONCIDENCIA"),
                    FechaEncontro = Fecha(reader, "FECHA_ENCONTRO"),
                    FechaCalifico = Fecha(reader, "FECHA_CALIFICO"),
                    FechaRegistroInterno = Fecha(reader, "FECHA_REGISTRO_INTERNO"),
                    EsProveedorIhss = BoolTexto(reader, "ES_PROVEEDOR_IHSS"),
                    TieneMotivo = Entero(reader, "TIENE_MOTIVO") == 1,
                    EsManual = Entero(reader, "ES_MANUAL") == 1
                }, cancellationToken);

        public Task<List<CoincidenciaJuridicaDto>> ObtenerJuridicasParaExportarAsync(ConsultaMonitoreoPaginadaDto consulta)
            => ObtenerMonitoreoCompletoAsync(consulta, ConstruirConsultaMonitoreoJuridicas(), "NOMBRE ASC, NUMEPATRO ASC", reader => new CoincidenciaJuridicaDto
            {
                Rtn = Texto(reader, "RTN"), Nombre = Texto(reader, "NOMBRE"), NumeroPatrono = Texto(reader, "NUMEPATRO"), ListaCoincidencia = Texto(reader, "LISTA_CONCIDENCIA"),
                FechaEncontro = Fecha(reader, "FECHA_ENCONTRO"), FechaCalifico = Fecha(reader, "FECHA_CALIFICO"), FechaRegistroInterno = Fecha(reader, "FECHA_REGISTRO_INTERNO"),
                EsProveedorIhss = BoolTexto(reader, "ES_PROVEEDOR_IHSS"), TieneMotivo = Entero(reader, "TIENE_MOTIVO") == 1, EsManual = Entero(reader, "ES_MANUAL") == 1
            });

        public Task<MonitoreoPaginadoDto<CoincidenciaNaturalDto>> ObtenerNaturalesPaginadasAsync(ConsultaMonitoreoPaginadaDto consulta, CancellationToken cancellationToken = default)
            => ObtenerMonitoreoPaginadoAsync(
                consulta,
                ConstruirConsultaMonitoreoNaturales(),
                "TOTAL_REPETIDOS DESC, NOMBRE ASC, NUMERO_IDENTIFICACION ASC",
                reader => new CoincidenciaNaturalDto
                {
                    NumeroIdentificacion = Texto(reader, "NUMERO_IDENTIFICACION"),
                    Nombre = Texto(reader, "NOMBRE"),
                    ListaCoincidencia = Texto(reader, "LISTA_CONCIDENCIA"),
                    TotalRepetidos = Entero(reader, "TOTAL_REPETIDOS"),
                    FechaEncontro = Fecha(reader, "FECHA_ENCONTRO"),
                    FechaCalifico = Fecha(reader, "FECHA_CALIFICO"),
                    FechaRegistroInterno = Fecha(reader, "FECHA_REGISTRO_INTERNO"),
                    TieneMotivo = Entero(reader, "TIENE_MOTIVO") == 1,
                    EsManual = Entero(reader, "ES_MANUAL") == 1
                }, cancellationToken);

        public Task<List<CoincidenciaNaturalDto>> ObtenerNaturalesParaExportarAsync(ConsultaMonitoreoPaginadaDto consulta)
            => ObtenerMonitoreoCompletoAsync(consulta, ConstruirConsultaMonitoreoNaturales(), "TOTAL_REPETIDOS DESC, NOMBRE ASC, NUMERO_IDENTIFICACION ASC", reader => new CoincidenciaNaturalDto
            {
                NumeroIdentificacion = Texto(reader, "NUMERO_IDENTIFICACION"), Nombre = Texto(reader, "NOMBRE"), ListaCoincidencia = Texto(reader, "LISTA_CONCIDENCIA"), TotalRepetidos = Entero(reader, "TOTAL_REPETIDOS"),
                FechaEncontro = Fecha(reader, "FECHA_ENCONTRO"), FechaCalifico = Fecha(reader, "FECHA_CALIFICO"), FechaRegistroInterno = Fecha(reader, "FECHA_REGISTRO_INTERNO"), TieneMotivo = Entero(reader, "TIENE_MOTIVO") == 1, EsManual = Entero(reader, "ES_MANUAL") == 1
            });

        public Task<MonitoreoPaginadoDto<CoincidenciaEmpleadoDto>> ObtenerEmpleadosPaginadasAsync(ConsultaMonitoreoPaginadaDto consulta, CancellationToken cancellationToken = default)
            => ObtenerMonitoreoPaginadoAsync(
                consulta,
                ConstruirConsultaMonitoreoEmpleados(),
                "TOTAL_REPETIDOS DESC, NOMBRE ASC, IDENTIDAD ASC",
                reader => new CoincidenciaEmpleadoDto
                {
                    Identidad = Texto(reader, "IDENTIDAD"),
                    Nombre = Texto(reader, "NOMBRE"),
                    ListaCoincidencia = Texto(reader, "LISTA_CONCIDENCIA"),
                    TotalRepetidos = Entero(reader, "TOTAL_REPETIDOS"),
                    FechaEncontro = Fecha(reader, "FECHA_ENCONTRO"),
                    FechaCalifico = Fecha(reader, "FECHA_CALIFICO"),
                    FechaRegistroInterno = Fecha(reader, "FECHA_REGISTRO_INTERNO"),
                    TieneMotivo = Entero(reader, "TIENE_MOTIVO") == 1,
                    EsManual = Entero(reader, "ES_MANUAL") == 1
                }, cancellationToken);

        public Task<List<CoincidenciaEmpleadoDto>> ObtenerEmpleadosParaExportarAsync(ConsultaMonitoreoPaginadaDto consulta)
            => ObtenerMonitoreoCompletoAsync(consulta, ConstruirConsultaMonitoreoEmpleados(), "TOTAL_REPETIDOS DESC, NOMBRE ASC, IDENTIDAD ASC", reader => new CoincidenciaEmpleadoDto
            {
                Identidad = Texto(reader, "IDENTIDAD"), Nombre = Texto(reader, "NOMBRE"), ListaCoincidencia = Texto(reader, "LISTA_CONCIDENCIA"), TotalRepetidos = Entero(reader, "TOTAL_REPETIDOS"),
                FechaEncontro = Fecha(reader, "FECHA_ENCONTRO"), FechaCalifico = Fecha(reader, "FECHA_CALIFICO"), FechaRegistroInterno = Fecha(reader, "FECHA_REGISTRO_INTERNO"), TieneMotivo = Entero(reader, "TIENE_MOTIVO") == 1, EsManual = Entero(reader, "ES_MANUAL") == 1
            });

        public async Task<List<DetalleCoincidenciaNaturalDto>> ObtenerDetalleNaturalAsync(string numeroIdentificacion)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT S.NUMERO_IDENTIFICACION, S.NOMBRES_PERSONA, S.TIPO_CONDICION_ACTUA_DESC, S.NUMERO_PATRONAL, S.NOMBRE_EMPRESA, S.ES_PEP , R.LISTA_CONCIDENCIA, TRUNC(R.FECHA_CALIFICO) FECHA_CALIFICO,
                TRUNC(R.FECHA_ENCONTRO) FECHA_COINCIDENCIA
                FROM DNP_IHSS.V_SOCIOS_REPRESENTANTES S
                INNER JOIN DNP_IHSS.REPORTE_COINCIDENCIAS R ON S.NUMERO_IDENTIFICACION = R.DNI AND R.TIPO_CALIFICACION_ID = 1 AND R.FECHA_CALIFICO IS NOT NULL 
                WHERE S.NUMERO_IDENTIFICACION = :numeroIdentificacion";
            
            cmd.Parameters.Add(new OracleParameter("numeroIdentificacion", numeroIdentificacion));

            var list = new List<DetalleCoincidenciaNaturalDto>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DetalleCoincidenciaNaturalDto
                {
                    NumeroIdentificacion = reader["NUMERO_IDENTIFICACION"]?.ToString() ?? string.Empty,
                    NombresPersona = reader["NOMBRES_PERSONA"]?.ToString() ?? string.Empty,
                    TipoCondicionActuaDesc = reader["TIPO_CONDICION_ACTUA_DESC"]?.ToString() ?? string.Empty,
                    NumeroPatronal = reader["NUMERO_PATRONAL"]?.ToString() ?? string.Empty,
                    NombreEmpresa = reader["NOMBRE_EMPRESA"]?.ToString() ?? string.Empty,
                    EsPep = reader["ES_PEP"]?.ToString() ?? string.Empty,
                    ListaCoincidencia = reader["LISTA_CONCIDENCIA"]?.ToString() ?? string.Empty,
                    FechaCalifico = reader["FECHA_CALIFICO"] == DBNull.Value ? null : Convert.ToDateTime(reader["FECHA_CALIFICO"]),
                    FechaCoincidencia = reader["FECHA_COINCIDENCIA"] == DBNull.Value ? null : Convert.ToDateTime(reader["FECHA_COINCIDENCIA"])
                });
            }
            return list;
        }

        public async Task<List<DetalleCoincidenciaEmpleadoDto>> ObtenerDetalleEmpleadoAsync(string numeroIdentificacion)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT DISTINCT R.DNI, R.NOMBRE,  'EMPLEADO IHSS' TIPO_CONDICION_ACTUA_DESC, E.NUMEPATRO, E.NOMBRE NOMBRE_EMPRESA, E.RAZOSOCI, R.LISTA_CONCIDENCIA, R.FECHA_CALIFICO, R.FECHA_ENCONTRO FECHA_COINCIDENCIA
                FROM DNP_IHSS.REPORTE_COINCIDENCIAS R
                INNER JOIN  DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS E ON e.IDENTIDAD = R.DNI AND R.TIPO_CALIFICACION_ID = 1 AND R.FECHA_CALIFICO IS NOT NULL 
                WHERE  R.DNI = :numeroIdentificacion
                  AND E.PERIODO = TRUNC(ADD_MONTHS(SYSDATE, -1), 'MM')";
            
            cmd.Parameters.Add(new OracleParameter("numeroIdentificacion", numeroIdentificacion));

            var list = new List<DetalleCoincidenciaEmpleadoDto>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DetalleCoincidenciaEmpleadoDto
                {
                    Identidad = reader["DNI"]?.ToString() ?? string.Empty,
                    Nombre = reader["NOMBRE"]?.ToString() ?? string.Empty,
                    TipoCondicionActuaDesc = reader["TIPO_CONDICION_ACTUA_DESC"]?.ToString() ?? string.Empty,
                    NumeroPatrono = reader["NUMEPATRO"]?.ToString() ?? string.Empty,
                    NombreEmpresa = reader["NOMBRE_EMPRESA"]?.ToString() ?? string.Empty,
                    RazoSoci = reader["RAZOSOCI"]?.ToString() ?? string.Empty,
                    ListaCoincidencia = reader["LISTA_CONCIDENCIA"]?.ToString() ?? string.Empty,
                    FechaCalifico = reader["FECHA_CALIFICO"] == DBNull.Value ? null : Convert.ToDateTime(reader["FECHA_CALIFICO"]),
                    FechaCoincidencia = reader["FECHA_COINCIDENCIA"] == DBNull.Value ? null : Convert.ToDateTime(reader["FECHA_COINCIDENCIA"])
                });
            }
            return list;
        }

        public async Task<List<TipoDocumentoDto>> ObtenerTiposDocumentoAsync()
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT RL_TIPO_DOCUMENTO_ID, RL_TIPO_DOCUMENTO_DESCRIPCION 
                FROM RL_TIPOS_DOCUMENTO
                ORDER BY RL_TIPO_DOCUMENTO_ID";

            var list = new List<TipoDocumentoDto>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new TipoDocumentoDto
                {
                    TipoDocumentoId = Convert.ToInt32(reader["RL_TIPO_DOCUMENTO_ID"]),
                    Descripcion = reader["RL_TIPO_DOCUMENTO_DESCRIPCION"]?.ToString() ?? string.Empty
                });
            }
            return list;
        }

        public async Task<List<TipoListaCautelaDto>> ObtenerTiposListasCautelaAsync()
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT TIPO_LISTA_CAUTELA_ID, LISTA_CAUTELA_DESCRICPION, TIPO_ARCHIVO, CANTIDAD_COLUMNAS 
                FROM DNP_IHSS.TIPO_LISTAS_CAUTELA
                ORDER BY TIPO_LISTA_CAUTELA_ID";

            var list = new List<TipoListaCautelaDto>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new TipoListaCautelaDto
                {
                    TipoListaCautelaId = Convert.ToInt32(reader["TIPO_LISTA_CAUTELA_ID"]),
                    Descripcion = reader["LISTA_CAUTELA_DESCRICPION"]?.ToString() ?? string.Empty,
                    TipoArchivo = reader["TIPO_ARCHIVO"] == DBNull.Value ? null : reader["TIPO_ARCHIVO"].ToString(),
                    CantidadColumnas = reader["CANTIDAD_COLUMNAS"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["CANTIDAD_COLUMNAS"])
                });
            }
            return list;
        }

        private static string? NormalizarOrigenRegistro(string? origenRegistro)
        {
            return string.IsNullOrWhiteSpace(origenRegistro)
                ? null
                : origenRegistro.Trim().ToUpperInvariant();
        }

        public async Task<bool> RegistrarPositivoAsync(RegistrarPositivoDto dto, long creadoPorId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            long? existingId = null;
            string? existingDataJson = null;
            string? existingOrigenRegistro = null;
            var origenRegistro = NormalizarOrigenRegistro(dto.OrigenRegistro);

            await using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = @"
                    SELECT LSP_POSITIVO_ID, LSP_TIPO_DOCUMENTO_ID, LSP_MOTIVO_INGRESO, LSP_TIPO_LISTA_CAUTELA_ID, LSP_ORIGEN_REGISTRO
                    FROM RL_LISTA_POSITIVOS 
                    WHERE LSP_NO_DOCUMENTO = :noDoc AND LSP_ESTADO_REGISTRO = 1 AND ROWNUM = 1";
                checkCmd.Parameters.Add(new OracleParameter("noDoc", (object?)dto.NoDocumento ?? DBNull.Value));

                await using var reader = await checkCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    existingId = Convert.ToInt64(reader["LSP_POSITIVO_ID"]);
                    existingOrigenRegistro = reader["LSP_ORIGEN_REGISTRO"] == DBNull.Value ? null : reader["LSP_ORIGEN_REGISTRO"]?.ToString();
                    var existingDto = new RegistrarPositivoDto
                    {
                        TipoDocumentoId = Convert.ToInt32(reader["LSP_TIPO_DOCUMENTO_ID"]),
                        TipoPositivoId = dto.TipoPositivoId,
                        NoDocumento = dto.NoDocumento,
                        NombreCompleto = dto.NombreCompleto,
                        MotivoIngreso = reader["LSP_MOTIVO_INGRESO"]?.ToString() ?? string.Empty,
                        TipoListaCautelaId = reader["LSP_TIPO_LISTA_CAUTELA_ID"] == DBNull.Value ? null : Convert.ToInt32(reader["LSP_TIPO_LISTA_CAUTELA_ID"]),
                        OrigenRegistro = existingOrigenRegistro
                    };
                    existingDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(existingDto);
                }
            }

            if (existingId.HasValue)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE RL_LISTA_POSITIVOS 
                    SET LSP_TIPO_DOCUMENTO_ID = :tipoDocId, 
                        LSP_TIPO_POSITIVO_ID = :tipoPosId,
                        LSP_NOMBRE_COMPLETO = :nombre, 
                        LSP_MOTIVO_INGRESO = :motivo,
                        LSP_TIPO_LISTA_CAUTELA_ID = :cautelaId,
                        -- Si un cliente viejo no envia origen, se conserva el valor que ya tenia el positivo.
                        LSP_ORIGEN_REGISTRO = NVL(:origenRegistro, LSP_ORIGEN_REGISTRO)
                    WHERE LSP_POSITIVO_ID = :id";

                cmd.Parameters.Add(new OracleParameter("tipoDocId", dto.TipoDocumentoId));
                cmd.Parameters.Add(new OracleParameter("tipoPosId", dto.TipoPositivoId));
                cmd.Parameters.Add(new OracleParameter("nombre", dto.NombreCompleto));
                cmd.Parameters.Add(new OracleParameter("motivo", dto.MotivoIngreso));
                cmd.Parameters.Add(new OracleParameter("cautelaId", (object?)dto.TipoListaCautelaId ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("origenRegistro", (object?)origenRegistro ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("id", existingId.Value));

                int rows = await cmd.ExecuteNonQueryAsync();
                bool success = rows > 0;

                if (success)
                {
                    var auditDto = new RegistrarPositivoDto
                    {
                        TipoDocumentoId = dto.TipoDocumentoId,
                        TipoPositivoId = dto.TipoPositivoId,
                        NoDocumento = dto.NoDocumento,
                        NombreCompleto = dto.NombreCompleto,
                        MotivoIngreso = dto.MotivoIngreso,
                        TipoListaCautelaId = dto.TipoListaCautelaId,
                        OrigenRegistro = origenRegistro ?? existingOrigenRegistro
                    };
                    string dataJsonNvo = Newtonsoft.Json.JsonConvert.SerializeObject(auditDto);
                    await _auditoriaRepo.RegistrarAsync("RL_LISTA_POSITIVOS", existingId.Value.ToString(), "UPDATE", existingDataJson, dataJsonNvo, creadoPorId, null, null, "MonitoreoListas");
                    _cache.Invalidate(ApplicationCacheScopes.MonitoreoMetadata);
                }
                return success;
            }
            else
            {
                var origenNuevo = origenRegistro ?? "MANUAL_CUMPLIMIENTO";
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO RL_LISTA_POSITIVOS (
                        LSP_POSITIVO_ID, LSP_TIPO_DOCUMENTO_ID, LSP_TIPO_POSITIVO_ID, 
                        LSP_NO_DOCUMENTO, LSP_NOMBRE_COMPLETO, LSP_MOTIVO_INGRESO, 
                        LSP_FECHA_CREACION, LSP_USR_CREACION_ID, LSP_ESTADO_REGISTRO,
                        LSP_TIPO_LISTA_CAUTELA_ID, LSP_ORIGEN_REGISTRO
                    ) VALUES (
                        SEQ_RL_LISTA_POSITIVOS.NEXTVAL, :tipoDocId, :tipoPosId,
                        :noDoc, :nombre, :motivo,
                        SYSDATE, :creadoPor, 1, :cautelaId, :origenRegistro
                    ) RETURNING LSP_POSITIVO_ID INTO :newId";

                cmd.Parameters.Add(new OracleParameter("tipoDocId", dto.TipoDocumentoId));
                cmd.Parameters.Add(new OracleParameter("tipoPosId", dto.TipoPositivoId));
                cmd.Parameters.Add(new OracleParameter("noDoc", (object?)dto.NoDocumento ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("nombre", dto.NombreCompleto));
                cmd.Parameters.Add(new OracleParameter("motivo", dto.MotivoIngreso));
                cmd.Parameters.Add(new OracleParameter("creadoPor", creadoPorId));
                cmd.Parameters.Add(new OracleParameter("cautelaId", (object?)dto.TipoListaCautelaId ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("origenRegistro", origenNuevo));

                var outParam = new OracleParameter("newId", OracleDbType.Int64, System.Data.ParameterDirection.Output);
                cmd.Parameters.Add(outParam);

                int rows = await cmd.ExecuteNonQueryAsync();
                bool success = rows > 0;

                if (success && outParam.Value != DBNull.Value)
                {
                    long newId = Convert.ToInt64(outParam.Value.ToString());
                    var auditDto = new RegistrarPositivoDto
                    {
                        TipoDocumentoId = dto.TipoDocumentoId,
                        TipoPositivoId = dto.TipoPositivoId,
                        NoDocumento = dto.NoDocumento,
                        NombreCompleto = dto.NombreCompleto,
                        MotivoIngreso = dto.MotivoIngreso,
                        TipoListaCautelaId = dto.TipoListaCautelaId,
                        OrigenRegistro = origenNuevo
                    };
                    string dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(auditDto);
                    await _auditoriaRepo.RegistrarAsync("RL_LISTA_POSITIVOS", newId.ToString(), "INSERT", null, dataJson, creadoPorId, null, null, "MonitoreoListas");
                    _cache.Invalidate(ApplicationCacheScopes.MonitoreoMetadata);
                }

                return success;
            }
        }

        public async Task<ExistingPositivoDto?> ObtenerPositivoPorDocumentoAsync(string noDocumento)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT LSP_TIPO_DOCUMENTO_ID, LSP_MOTIVO_INGRESO, LSP_TIPO_LISTA_CAUTELA_ID, LSP_ORIGEN_REGISTRO, LSP_FECHA_CREACION
                FROM RL_LISTA_POSITIVOS 
                WHERE LSP_NO_DOCUMENTO = :noDoc AND LSP_ESTADO_REGISTRO = 1 AND ROWNUM = 1";
            cmd.Parameters.Add(new OracleParameter("noDoc", noDocumento));

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ExistingPositivoDto
                {
                    TipoDocumentoId = Convert.ToInt32(reader["LSP_TIPO_DOCUMENTO_ID"]),
                    MotivoIngreso = reader["LSP_MOTIVO_INGRESO"]?.ToString() ?? string.Empty,
                    TipoListaCautelaId = reader["LSP_TIPO_LISTA_CAUTELA_ID"] == DBNull.Value ? null : Convert.ToInt32(reader["LSP_TIPO_LISTA_CAUTELA_ID"]),
                    OrigenRegistro = reader["LSP_ORIGEN_REGISTRO"] == DBNull.Value ? null : reader["LSP_ORIGEN_REGISTRO"]?.ToString(),
                    FechaRegistroInterno = reader["LSP_FECHA_CREACION"] == DBNull.Value ? null : Convert.ToDateTime(reader["LSP_FECHA_CREACION"])
                };
            }
            return null;
        }

        public async Task<List<SeguimientoDto>> ObtenerSeguimientosAsync(string noDocumento, DateTime? desde = null, DateTime? hasta = null)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            long? positivoId = null;
            await using (var findCmd = conn.CreateCommand())
            {
                findCmd.CommandText = "SELECT LSP_POSITIVO_ID FROM RL_LISTA_POSITIVOS WHERE LSP_NO_DOCUMENTO = :noDoc AND LSP_ESTADO_REGISTRO = 1 AND ROWNUM = 1";
                findCmd.Parameters.Add(new OracleParameter("noDoc", noDocumento));
                var res = await findCmd.ExecuteScalarAsync();
                if (res != null && res != DBNull.Value)
                {
                    positivoId = Convert.ToInt64(res);
                }
            }

            if (!positivoId.HasValue)
            {
                return new List<SeguimientoDto>();
            }

            var list = new List<SeguimientoDto>();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT d.DLL_DETALLE_LISTA_ID, d.DLL_LSP_POSITIVO_ID, d.DLL_MOTIVO_INGRESO, 
                           d.DLL_FECHA_CREACION, d.DLL_USR_CREACION_ID, u.USR_EMAIL
                    FROM RL_DETALLE_LISTA d
                    LEFT JOIN RL_USUARIOS u ON d.DLL_USR_CREACION_ID = u.USR_ID
                    WHERE d.DLL_LSP_POSITIVO_ID = :posId AND d.DLL_ESTADO_REGISTRO = 1";
                if (desde.HasValue)
                {
                    cmd.CommandText += " AND d.DLL_FECHA_CREACION >= :desde";
                }
                if (hasta.HasValue)
                {
                    cmd.CommandText += " AND d.DLL_FECHA_CREACION < :hasta";
                }
                cmd.CommandText += " ORDER BY d.DLL_FECHA_CREACION DESC";
                cmd.Parameters.Add(new OracleParameter("posId", positivoId.Value));
                if (desde.HasValue)
                {
                    cmd.Parameters.Add(new OracleParameter("desde", desde.Value.Date));
                }
                if (hasta.HasValue)
                {
                    cmd.Parameters.Add(new OracleParameter("hasta", hasta.Value.Date.AddDays(1)));
                }

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new SeguimientoDto
                    {
                        DetalleListaId = Convert.ToInt64(reader["DLL_DETALLE_LISTA_ID"]),
                        PositivoId = Convert.ToInt64(reader["DLL_LSP_POSITIVO_ID"]),
                        MotivoIngreso = reader["DLL_MOTIVO_INGRESO"]?.ToString() ?? string.Empty,
                        FechaCreacion = Convert.ToDateTime(reader["DLL_FECHA_CREACION"]),
                        UsrCreacionId = reader["DLL_USR_CREACION_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["DLL_USR_CREACION_ID"]),
                        UsrEmail = reader["USR_EMAIL"]?.ToString() ?? "Sistema",
                        Evidencias = new List<EvidenciaDto>()
                    });
                }
            }

            foreach (var seg in list)
            {
                await using var cmdEvi = conn.CreateCommand();
                cmdEvi.CommandText = @"
                    SELECT EVI_ID, EVI_NOMBRE_ARCHIVO, EVI_TIPO_MIME 
                    FROM RL_DETALLE_EVIDENCIA 
                    WHERE EVI_DETALLE_ID = :detId
                      AND EVI_ESTADO_REGISTRO = 1
                    ORDER BY EVI_ID ASC";
                cmdEvi.Parameters.Add(new OracleParameter("detId", seg.DetalleListaId));

                await using var readerEvi = await cmdEvi.ExecuteReaderAsync();
                while (await readerEvi.ReadAsync())
                {
                    seg.Evidencias.Add(new EvidenciaDto
                    {
                        EvidenciaId = Convert.ToInt64(readerEvi["EVI_ID"]),
                        NombreArchivo = readerEvi["EVI_NOMBRE_ARCHIVO"]?.ToString() ?? string.Empty,
                        TipoMime = readerEvi["EVI_TIPO_MIME"]?.ToString() ?? string.Empty
                    });
                }
            }

            return list;
        }

        public async Task<long> RegistrarSeguimientoAsync(long positivoId, string motivo, long usuarioId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO RL_DETALLE_LISTA (
                    DLL_DETALLE_LISTA_ID, DLL_LSP_POSITIVO_ID, DLL_MOTIVO_INGRESO, 
                    DLL_USR_CREACION_ID, DLL_FECHA_CREACION, DLL_ESTADO_REGISTRO
                ) VALUES (
                    SEQ_RL_DETALLE_LISTA.NEXTVAL, :posId, :motivo, 
                    :usuarioId, SYSDATE, 1
                ) RETURNING DLL_DETALLE_LISTA_ID INTO :newId";

            cmd.Parameters.Add(new OracleParameter("posId", positivoId));
            cmd.Parameters.Add(new OracleParameter("motivo", motivo));
            cmd.Parameters.Add(new OracleParameter("usuarioId", usuarioId));

            var outParam = new OracleParameter("newId", OracleDbType.Int64, System.Data.ParameterDirection.Output);
            cmd.Parameters.Add(outParam);

            await cmd.ExecuteNonQueryAsync();
            long newId = Convert.ToInt64(outParam.Value.ToString());

            // Auditoría
            var dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { PositivoId = positivoId, Motivo = motivo });
            await _auditoriaRepo.RegistrarAsync("RL_DETALLE_LISTA", newId.ToString(), "INSERT", null, dataJson, usuarioId, null, null, "MonitoreoListas");
            _cache.Invalidate(ApplicationCacheScopes.MonitoreoMetadata);

            return newId;
        }
        public async Task GuardarEvidenciaMetaAsync(long detalleId, string nombreArchivo, string tipoMime, string rutaArchivo, long usuarioId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO RL_DETALLE_EVIDENCIA (
                    EVI_ID, EVI_DETALLE_ID, EVI_NOMBRE_ARCHIVO, 
                    EVI_TIPO_MIME, EVI_RUTA_ARCHIVO, EVI_FECHA_CREACION, EVI_USR_CREACION_ID,
                    EVI_ESTADO_REGISTRO
                ) VALUES (
                    SEQ_RL_DETALLE_EVIDENCIA.NEXTVAL, :detId, :nombre, 
                    :mime, :ruta, SYSDATE, :usrId, 1
                ) RETURNING EVI_ID INTO :newId";

            cmd.Parameters.Add(new OracleParameter("detId", detalleId));
            cmd.Parameters.Add(new OracleParameter("nombre", nombreArchivo));
            cmd.Parameters.Add(new OracleParameter("mime", tipoMime));
            cmd.Parameters.Add(new OracleParameter("ruta", rutaArchivo));
            cmd.Parameters.Add(new OracleParameter("usrId", usuarioId));

            var outParam = new OracleParameter("newId", OracleDbType.Int64, System.Data.ParameterDirection.Output);
            cmd.Parameters.Add(outParam);

            await cmd.ExecuteNonQueryAsync();
            long newId = Convert.ToInt64(outParam.Value.ToString());

            // Auditoría
            var dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { DetalleId = detalleId, NombreArchivo = nombreArchivo, TipoMime = tipoMime, NombreFisico = rutaArchivo });
            await _auditoriaRepo.RegistrarAsync("RL_DETALLE_EVIDENCIA", newId.ToString(), "INSERT", null, dataJson, usuarioId, null, null, "MonitoreoListas");
        }

        public async Task<long?> ObtenerPositivoIdPorDocumentoAsync(string noDocumento)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT LSP_POSITIVO_ID FROM RL_LISTA_POSITIVOS WHERE LSP_NO_DOCUMENTO = :noDoc AND LSP_ESTADO_REGISTRO = 1 AND ROWNUM = 1";
            cmd.Parameters.Add(new OracleParameter("noDoc", noDocumento));

            var res = await cmd.ExecuteScalarAsync();
            if (res != null && res != DBNull.Value)
            {
                return Convert.ToInt64(res);
            }
            return null;
        }

        public async Task<(string Nombre, string Ruta, string Mime)?> ObtenerEvidenciaPorIdAsync(long evidenciaId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT EVI_NOMBRE_ARCHIVO, EVI_RUTA_ARCHIVO, EVI_TIPO_MIME
                FROM RL_DETALLE_EVIDENCIA
                WHERE EVI_ID = :eviId
                  AND EVI_ESTADO_REGISTRO = 1";
            cmd.Parameters.Add(new OracleParameter("eviId", evidenciaId));

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    reader["EVI_NOMBRE_ARCHIVO"]?.ToString() ?? string.Empty,
                    reader["EVI_RUTA_ARCHIVO"]?.ToString() ?? string.Empty,
                    reader["EVI_TIPO_MIME"]?.ToString() ?? string.Empty
                );
            }
            return null;
        }

        public async Task RegistrarAuditoriaVisualizacionAsync(long evidenciaId, string dataJson, long usuarioId)
        {
            await _auditoriaRepo.RegistrarAsync("RL_DETALLE_EVIDENCIA", evidenciaId.ToString(), "VER", null, dataJson, usuarioId, null, null, "MonitoreoListas");
        }

        public async Task<bool> ActualizarSeguimientoAsync(long detalleId, string motivoIngreso, long usuarioId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string? anteriorMotivo = null;
            await using (var getCmd = conn.CreateCommand())
            {
                getCmd.CommandText = "SELECT DLL_MOTIVO_INGRESO FROM RL_DETALLE_LISTA WHERE DLL_DETALLE_LISTA_ID = :detId AND DLL_ESTADO_REGISTRO = 1";
                getCmd.Parameters.Add(new OracleParameter("detId", detalleId));
                var res = await getCmd.ExecuteScalarAsync();
                if (res == null || res == DBNull.Value) return false;
                anteriorMotivo = res.ToString();
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE RL_DETALLE_LISTA 
                SET DLL_MOTIVO_INGRESO = :motivo, 
                    DLL_USR_MODIFICACION_ID = :usrId, 
                    DLL_FECHA_MODIFICACION = SYSDATE 
                WHERE DLL_DETALLE_LISTA_ID = :detId AND DLL_ESTADO_REGISTRO = 1";
            cmd.Parameters.Add(new OracleParameter("motivo", motivoIngreso));
            cmd.Parameters.Add(new OracleParameter("usrId", usuarioId));
            cmd.Parameters.Add(new OracleParameter("detId", detalleId));

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
            {
                var valAnterior = Newtonsoft.Json.JsonConvert.SerializeObject(new { Motivo = anteriorMotivo });
                var valNuevo = Newtonsoft.Json.JsonConvert.SerializeObject(new { Motivo = motivoIngreso });
                await _auditoriaRepo.RegistrarAsync("RL_DETALLE_LISTA", detalleId.ToString(), "UPDATE", valAnterior, valNuevo, usuarioId, null, null, "MonitoreoListas");
                _cache.Invalidate(ApplicationCacheScopes.MonitoreoMetadata);
                return true;
            }
            return false;
        }

        public async Task<bool> EliminarEvidenciaMetaAsync(long evidenciaId, long usuarioId, string motivoEliminacion)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Se captura el estado anterior para que RL_AUDITORIA conserve que evidencia fue inactivada.
            string? anteriorJson = null;
            await using (var getCmd = conn.CreateCommand())
            {
                getCmd.CommandText = @"
                    SELECT EVI_NOMBRE_ARCHIVO, EVI_RUTA_ARCHIVO, EVI_TIPO_MIME, EVI_ESTADO_REGISTRO
                    FROM RL_DETALLE_EVIDENCIA
                    WHERE EVI_ID = :eviId
                      AND EVI_ESTADO_REGISTRO = 1";
                getCmd.Parameters.Add(new OracleParameter("eviId", evidenciaId));
                await using var reader = await getCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    anteriorJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { 
                        Nombre = reader["EVI_NOMBRE_ARCHIVO"]?.ToString(), 
                        NombreFisico = reader["EVI_RUTA_ARCHIVO"]?.ToString(),
                        TipoMime = reader["EVI_TIPO_MIME"]?.ToString(),
                        Estado = Convert.ToInt32(reader["EVI_ESTADO_REGISTRO"])
                    });
                }
            }

            if (anteriorJson == null) return false;

            // Eliminación lógica: no se borra el archivo ni la fila; solo se marca inactiva.
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE RL_DETALLE_EVIDENCIA
                SET EVI_ESTADO_REGISTRO = 0,
                    EVI_USR_INACTIVO_ID = :usrId,
                    EVI_FECHA_INACTIVO = SYSDATE
                WHERE EVI_ID = :eviId
                  AND EVI_ESTADO_REGISTRO = 1";
            cmd.Parameters.Add(new OracleParameter("usrId", usuarioId));
            cmd.Parameters.Add(new OracleParameter("eviId", evidenciaId));

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
            {
                var valNuevo = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    Estado = 0,
                    UsrInactivoId = usuarioId,
                    TipoEliminacion = "LOGICA",
                    MotivoEliminacion = motivoEliminacion
                });
                await _auditoriaRepo.RegistrarAsync("RL_DETALLE_EVIDENCIA", evidenciaId.ToString(), "DELETE", anteriorJson, valNuevo, usuarioId, null, null, "MonitoreoListas");
                return true;
            }
            return false;
        }

        public async Task<bool> EliminarSeguimientoLogicoAsync(long detalleId, long usuarioId, string motivoEliminacion)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Guardamos el comentario original para poder comparar antes/despues en bitacora.
            string? anteriorMotivo = null;
            await using (var getCmd = conn.CreateCommand())
            {
                getCmd.CommandText = "SELECT DLL_MOTIVO_INGRESO FROM RL_DETALLE_LISTA WHERE DLL_DETALLE_LISTA_ID = :detId AND DLL_ESTADO_REGISTRO = 1";
                getCmd.Parameters.Add(new OracleParameter("detId", detalleId));
                var res = await getCmd.ExecuteScalarAsync();
                if (res == null || res == DBNull.Value) return false;
                anteriorMotivo = res.ToString();
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE RL_DETALLE_LISTA 
                SET DLL_ESTADO_REGISTRO = 0, 
                    DLL_USR_INACTIVO_ID = :usrId, 
                    DLL_FECHA_INACTIVO = SYSDATE 
                WHERE DLL_DETALLE_LISTA_ID = :detId AND DLL_ESTADO_REGISTRO = 1";
            cmd.Parameters.Add(new OracleParameter("usrId", usuarioId));
            cmd.Parameters.Add(new OracleParameter("detId", detalleId));

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
            {
                var valAnterior = Newtonsoft.Json.JsonConvert.SerializeObject(new { Motivo = anteriorMotivo, Estado = 1 });
                // El motivo de eliminacion queda en AUD_DATOS_NVO, sin requerir campos nuevos.
                var valNuevo = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    Motivo = anteriorMotivo,
                    Estado = 0,
                    UsrInactivoId = usuarioId,
                    TipoEliminacion = "LOGICA",
                    MotivoEliminacion = motivoEliminacion
                });
                await _auditoriaRepo.RegistrarAsync("RL_DETALLE_LISTA", detalleId.ToString(), "DELETE", valAnterior, valNuevo, usuarioId, null, null, "MonitoreoListas");
                _cache.Invalidate(ApplicationCacheScopes.MonitoreoMetadata);
                return true;
            }
            return false;
        }

        public async Task RegistrarAuditoriaReporteImpresoAsync(string noDocumento, string dataJson, long usuarioId)
        {
            await _auditoriaRepo.RegistrarAsync("RL_LISTA_POSITIVOS", noDocumento, "VER", null, dataJson, usuarioId, null, null, "ReporteImpresoPatrono");
        }

        public async Task<int> CrearTipoListaCautelaAsync(string descripcion, string? tipoArchivo, int? cantidadColumnas, long usuarioId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Obtener el siguiente ID de forma segura
            int newId = 1;
            await using (var getCmd = conn.CreateCommand())
            {
                getCmd.CommandText = "SELECT NVL(MAX(TIPO_LISTA_CAUTELA_ID), 0) + 1 FROM DNP_IHSS.TIPO_LISTAS_CAUTELA";
                var res = await getCmd.ExecuteScalarAsync();
                if (res != null && res != DBNull.Value)
                {
                    newId = Convert.ToInt32(res);
                }
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO DNP_IHSS.TIPO_LISTAS_CAUTELA (TIPO_LISTA_CAUTELA_ID, LISTA_CAUTELA_DESCRICPION, TIPO_ARCHIVO, CANTIDAD_COLUMNAS)
                VALUES (:id, :descripcion, :tipoArchivo, :cantidadColumnas)";
            cmd.Parameters.Add(new OracleParameter("id", newId));
            cmd.Parameters.Add(new OracleParameter("descripcion", descripcion));
            cmd.Parameters.Add(new OracleParameter("tipoArchivo", (object?)tipoArchivo ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("cantidadColumnas", (object?)cantidadColumnas ?? DBNull.Value));

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
            {
                var dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { Descripcion = descripcion, TipoArchivo = tipoArchivo, CantidadColumnas = cantidadColumnas });
                await _auditoriaRepo.RegistrarAsync("TIPO_LISTAS_CAUTELA", newId.ToString(), "INSERT", null, dataJson, usuarioId, null, null, "TipoListas");
                return newId;
            }
            return 0;
        }

        public async Task<bool> ActualizarTipoListaCautelaAsync(int id, string descripcion, string? tipoArchivo, int? cantidadColumnas, long usuarioId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string? anteriorDescripcion = null;
            string? anteriorTipoArchivo = null;
            int? anteriorCantidadColumnas = null;
            await using (var getCmd = conn.CreateCommand())
            {
                getCmd.CommandText = "SELECT LISTA_CAUTELA_DESCRICPION, TIPO_ARCHIVO, CANTIDAD_COLUMNAS FROM DNP_IHSS.TIPO_LISTAS_CAUTELA WHERE TIPO_LISTA_CAUTELA_ID = :id";
                getCmd.Parameters.Add(new OracleParameter("id", id));
                await using var reader = await getCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    anteriorDescripcion = reader["LISTA_CAUTELA_DESCRICPION"]?.ToString();
                    anteriorTipoArchivo = reader["TIPO_ARCHIVO"] == DBNull.Value ? null : reader["TIPO_ARCHIVO"].ToString();
                    anteriorCantidadColumnas = reader["CANTIDAD_COLUMNAS"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["CANTIDAD_COLUMNAS"]);
                }
                else
                {
                    return false;
                }
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE DNP_IHSS.TIPO_LISTAS_CAUTELA 
                SET LISTA_CAUTELA_DESCRICPION = :descripcion,
                    TIPO_ARCHIVO = :tipoArchivo,
                    CANTIDAD_COLUMNAS = :cantidadColumnas
                WHERE TIPO_LISTA_CAUTELA_ID = :id";
            cmd.Parameters.Add(new OracleParameter("descripcion", descripcion));
            cmd.Parameters.Add(new OracleParameter("tipoArchivo", (object?)tipoArchivo ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("cantidadColumnas", (object?)cantidadColumnas ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("id", id));

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
            {
                var valAnterior = Newtonsoft.Json.JsonConvert.SerializeObject(new { Descripcion = anteriorDescripcion, TipoArchivo = anteriorTipoArchivo, CantidadColumnas = anteriorCantidadColumnas });
                var valNuevo = Newtonsoft.Json.JsonConvert.SerializeObject(new { Descripcion = descripcion, TipoArchivo = tipoArchivo, CantidadColumnas = cantidadColumnas });
                await _auditoriaRepo.RegistrarAsync("TIPO_LISTAS_CAUTELA", id.ToString(), "UPDATE", valAnterior, valNuevo, usuarioId, null, null, "TipoListas");
                return true;
            }
            return false;
        }

        public async Task<bool> EliminarTipoListaCautelaAsync(int id, long usuarioId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string? anteriorDescripcion = null;
            string? anteriorTipoArchivo = null;
            int? anteriorCantidadColumnas = null;
            await using (var getCmd = conn.CreateCommand())
            {
                getCmd.CommandText = "SELECT LISTA_CAUTELA_DESCRICPION, TIPO_ARCHIVO, CANTIDAD_COLUMNAS FROM DNP_IHSS.TIPO_LISTAS_CAUTELA WHERE TIPO_LISTA_CAUTELA_ID = :id";
                getCmd.Parameters.Add(new OracleParameter("id", id));
                await using var reader = await getCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    anteriorDescripcion = reader["LISTA_CAUTELA_DESCRICPION"]?.ToString();
                    anteriorTipoArchivo = reader["TIPO_ARCHIVO"] == DBNull.Value ? null : reader["TIPO_ARCHIVO"].ToString();
                    anteriorCantidadColumnas = reader["CANTIDAD_COLUMNAS"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["CANTIDAD_COLUMNAS"]);
                }
                else
                {
                    return false;
                }
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM DNP_IHSS.TIPO_LISTAS_CAUTELA WHERE TIPO_LISTA_CAUTELA_ID = :id";
            cmd.Parameters.Add(new OracleParameter("id", id));

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
            {
                var valAnterior = Newtonsoft.Json.JsonConvert.SerializeObject(new { Descripcion = anteriorDescripcion, TipoArchivo = anteriorTipoArchivo, CantidadColumnas = anteriorCantidadColumnas });
                await _auditoriaRepo.RegistrarAsync("TIPO_LISTAS_CAUTELA", id.ToString(), "DELETE", valAnterior, null, usuarioId, null, null, "TipoListas");
                return true;
            }
            return false;
        }

        public async Task<List<ResumenListaDto>> ObtenerResumenListasAsync()
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    t.LISTA_CAUTELA_DESCRICPION AS LISTA, 
                    NVL(v.USUARIO, 'N/A') AS USUARIO, 
                    v.FECHA_CREACION, 
                    NVL(v.CANTIDAD_REGISTRO, 0) AS CANTIDAD_REGISTRO,
                    t.TIPO_LISTA_CAUTELA_ID
                FROM DNP_IHSS.TIPO_LISTAS_CAUTELA t
                LEFT JOIN DNP_IHSS.V_LISTAS_CAUTELA v ON t.LISTA_CAUTELA_DESCRICPION = v.LISTA
                ORDER BY t.LISTA_CAUTELA_DESCRICPION";

            var list = new List<ResumenListaDto>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ResumenListaDto
                {
                    TipoListaCautelaId = reader["TIPO_LISTA_CAUTELA_ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TIPO_LISTA_CAUTELA_ID"]),
                    Lista = reader["LISTA"]?.ToString() ?? string.Empty,
                    Usuario = reader["USUARIO"]?.ToString() ?? string.Empty,
                    FechaCreacion = reader["FECHA_CREACION"] == DBNull.Value ? null : Convert.ToDateTime(reader["FECHA_CREACION"]),
                    CantidadRegistros = reader["CANTIDAD_REGISTRO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CANTIDAD_REGISTRO"])
                });
            }
            return list;
        }

        public async Task<List<Dictionary<string, object>>> ObtenerDetalleListaParaExportarAsync(int tipoListaId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // 1. Obtener la descripción/nombre del tipo de lista
            string listaNombre = string.Empty;
            await using (var cmdTipo = conn.CreateCommand())
            {
                cmdTipo.CommandText = "SELECT LISTA_CAUTELA_DESCRICPION FROM DNP_IHSS.TIPO_LISTAS_CAUTELA WHERE TIPO_LISTA_CAUTELA_ID = :id";
                cmdTipo.Parameters.Add(new OracleParameter("id", tipoListaId));
                listaNombre = (await cmdTipo.ExecuteScalarAsync())?.ToString() ?? string.Empty;
            }

            if (string.IsNullOrEmpty(listaNombre))
            {
                return new List<Dictionary<string, object>>();
            }

            // 2. Obtener el query template del parámetro 21
            string sqlQuery = string.Empty;
            await using (var cmdParam = conn.CreateCommand())
            {
                cmdParam.CommandText = "SELECT VALOR_PARAMETRO FROM DNP_IHSS.PARAMETRO_SISTEMA WHERE PARAMETRO_SISTEMA_ID = 21";
                sqlQuery = (await cmdParam.ExecuteScalarAsync())?.ToString() ?? string.Empty;
            }

            if (string.IsNullOrEmpty(sqlQuery))
            {
                throw new Exception("No se encontró la consulta de exportación en el parámetro 21 del sistema.");
            }

            // 3. Ejecutar la consulta dinámica
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sqlQuery;
            cmd.Parameters.Add(new OracleParameter("param0", listaNombre));

            var resultList = new List<Dictionary<string, object>>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string colName = reader.GetName(i);
                    object colVal = reader.GetValue(i);
                    row[colName] = colVal == DBNull.Value ? null! : colVal;
                }
                resultList.Add(row);
            }

            return resultList;
        }

        private async Task<MonitoreoPaginadoDto<T>> ObtenerMonitoreoPaginadoAsync<T>(
            ConsultaMonitoreoPaginadaDto consulta,
            string baseSql,
            string orderBy,
            Func<OracleDataReader, T> map,
            CancellationToken cancellationToken)
        {
            var requestStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var filteredSql = ConstruirConsultaMonitoreoFiltrada(baseSql, consulta);
            var pageSize = Math.Clamp(consulta.TamanoPagina, 1, 200);
            var requestedPage = Math.Max(consulta.Pagina, 1);

            await using var conn = _db.CreateConnection();
            var connectionStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await conn.OpenAsync(cancellationToken);
            connectionStopwatch.Stop();

            var pageResult = await EjecutarPaginaMonitoreoAsync(
                conn, filteredSql, orderBy, requestedPage, pageSize, consulta, map, cancellationToken);

            var metadataLookupStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var metadata = await _cache.GetOrCreateAsync(
                ApplicationCacheScopes.MonitoreoMetadata,
                CrearClaveMetadataMonitoreo(baseSql, consulta),
                _cacheSettings.MonitoreoMetadataTtl,
                () => ObtenerMetadataMonitoreoAsync(filteredSql, consulta, cancellationToken),
                cancellationToken: cancellationToken);
            metadataLookupStopwatch.Stop();

            var totalPages = metadata.TotalRegistros == 0
                ? 0
                : (int)Math.Ceiling(metadata.TotalRegistros / (double)pageSize);
            var effectivePage = totalPages == 0 ? 1 : Math.Clamp(requestedPage, 1, totalPages);
            if (pageResult.Items.Count == 0 && effectivePage != requestedPage)
            {
                // Sólo una página vacía activa la normalización y el único reintento.
                pageResult = await EjecutarPaginaMonitoreoAsync(
                    conn, filteredSql, orderBy, effectivePage, pageSize, consulta, map, cancellationToken);
            }

            requestStopwatch.Stop();
            Serilog.Log.Information(
                "MonitoringQuery completed: type={MonitoringType} page={Page} pageSize={PageSize} filtered={Filtered} connectionOpenMs={ConnectionOpenMs} pageExecuteMs={PageExecuteMs} firstRowMs={FirstRowMs} rowsReadMs={RowsReadMs} mappingMs={MappingMs} metadataLookupMs={MetadataLookupMs} totalRepositoryMs={TotalRepositoryMs}",
                ResolveMonitoringType(baseSql), effectivePage, pageSize,
                !string.IsNullOrWhiteSpace(consulta.Buscar)
                    || !string.Equals(consulta.Estado, "todos", StringComparison.OrdinalIgnoreCase)
                    || consulta.FechaDesde.HasValue || consulta.FechaHasta.HasValue,
                connectionStopwatch.ElapsedMilliseconds, pageResult.ExecuteMs, pageResult.FirstRowMs,
                pageResult.RowsReadMs, pageResult.MappingMs, metadataLookupStopwatch.ElapsedMilliseconds,
                requestStopwatch.ElapsedMilliseconds);

            return new MonitoreoPaginadoDto<T>
            {
                Items = pageResult.Items,
                Pagina = effectivePage,
                TamanoPagina = pageSize,
                TotalRegistros = metadata.TotalRegistros,
                TotalPaginas = totalPages,
                Totales = new MonitoreoTotalesDto
                {
                    TotalRegistros = metadata.TotalRegistros,
                    Pendientes = metadata.Pendientes,
                    ConMotivo = metadata.ConMotivo,
                    Manuales = metadata.Manuales,
                    CerradosPasivos = metadata.CerradosPasivos
                }
            };
        }

        private sealed record MonitoreoMetadata(int TotalRegistros, int Pendientes, int ConMotivo, int Manuales, int CerradosPasivos);

        private sealed record MonitoreoPageResult<T>(List<T> Items, long ExecuteMs, long FirstRowMs, long RowsReadMs, long MappingMs);

        private async Task<MonitoreoPageResult<T>> EjecutarPaginaMonitoreoAsync<T>(
            OracleConnection conn,
            string filteredSql,
            string orderBy,
            int page,
            int pageSize,
            ConsultaMonitoreoPaginadaDto consulta,
            Func<OracleDataReader, T> map,
            CancellationToken cancellationToken)
        {
            var firstRow = (page - 1) * pageSize;
            var lastRow = page * pageSize;
            var items = new List<T>(pageSize);
            var executeStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await using var command = conn.CreateCommand();
            command.BindByName = true;
            command.CommandText = $@"
                SELECT *
                FROM (
                    SELECT q.*, ROWNUM AS NUMERO_FILA
                    FROM (
                        {filteredSql}
                        ORDER BY {orderBy}
                    ) q
                    WHERE ROWNUM <= :filaFinal
                )
                WHERE NUMERO_FILA > :filaInicial";
            AddMonitoringParameters(command, consulta);
            command.Parameters.Add(new OracleParameter("filaFinal", lastRow));
            command.Parameters.Add(new OracleParameter("filaInicial", firstRow));
            OracleDataReader reader;
            try
            {
                reader = await command.ExecuteReaderAsync(cancellationToken);
            }
            catch (OracleException exception) when (exception.Number == 1013)
            {
                Serilog.Log.Warning(
                    "MonitoringQuery cancelled by Oracle: phase=page requestAborted={RequestAborted}",
                    cancellationToken.IsCancellationRequested || _httpContextAccessor.HttpContext?.RequestAborted.IsCancellationRequested == true);
                throw;
            }
            await using (reader)
            {
                executeStopwatch.Stop();

                var firstRowStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var firstRowMs = 0L;
                var rowsReadStopwatch = new System.Diagnostics.Stopwatch();
                var mappingMs = 0L;
                var hasRow = false;
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (!hasRow)
                    {
                        firstRowStopwatch.Stop();
                        firstRowMs = firstRowStopwatch.ElapsedMilliseconds;
                        rowsReadStopwatch.Start();
                        hasRow = true;
                    }

                    var mappingStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    items.Add(map(reader));
                    mappingStopwatch.Stop();
                    mappingMs += mappingStopwatch.ElapsedMilliseconds;
                }
                rowsReadStopwatch.Stop();
                if (!hasRow) firstRowMs = firstRowStopwatch.ElapsedMilliseconds;

                return new MonitoreoPageResult<T>(
                    items, executeStopwatch.ElapsedMilliseconds, firstRowMs,
                    rowsReadStopwatch.ElapsedMilliseconds, mappingMs);
            }
        }

        private async Task<MonitoreoMetadata> ObtenerMetadataMonitoreoAsync(
            string filteredSql,
            ConsultaMonitoreoPaginadaDto consulta,
            CancellationToken cancellationToken)
        {
            var metadataStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await using var conn = _db.CreateConnection();
            var connectionStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await conn.OpenAsync(cancellationToken);
            connectionStopwatch.Stop();
            await using var command = conn.CreateCommand();
            command.BindByName = true;
            command.CommandText = $@"
                SELECT COUNT(*) AS TOTAL_REGISTROS,
                       NVL(SUM(CASE WHEN ESTADO_MONITOREO = 'pendiente' THEN 1 ELSE 0 END), 0) AS PENDIENTES,
                       NVL(SUM(CASE WHEN ESTADO_MONITOREO = 'con_motivo' THEN 1 ELSE 0 END), 0) AS CON_MOTIVO,
                       NVL(SUM(CASE WHEN ES_MANUAL = 1 THEN 1 ELSE 0 END), 0) AS MANUALES,
                       NVL(SUM(CASE WHEN ESTADO_MONITOREO = 'cerrado_pasivo' THEN 1 ELSE 0 END), 0) AS CERRADOS_PASIVOS
                FROM ({filteredSql}) metadata_query";
            AddMonitoringParameters(command, consulta);
            OracleDataReader reader;
            var executeStopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                reader = await command.ExecuteReaderAsync(cancellationToken);
            }
            catch (OracleException exception) when (exception.Number == 1013)
            {
                Serilog.Log.Warning(
                    "MonitoringQuery cancelled by Oracle: phase=metadata requestAborted={RequestAborted}",
                    cancellationToken.IsCancellationRequested || _httpContextAccessor.HttpContext?.RequestAborted.IsCancellationRequested == true);
                throw;
            }
            await using (reader)
            {
                executeStopwatch.Stop();
                if (!await reader.ReadAsync(cancellationToken))
                {
                    metadataStopwatch.Stop();
                    Serilog.Log.Information(
                        "MonitoringMetadata completed: connectionOpenMs={ConnectionOpenMs} executeMs={ExecuteMs} totalMs={TotalMs}",
                        connectionStopwatch.ElapsedMilliseconds, executeStopwatch.ElapsedMilliseconds,
                        metadataStopwatch.ElapsedMilliseconds);
                    return new MonitoreoMetadata(0, 0, 0, 0, 0);
                }
                var metadata = new MonitoreoMetadata(
                    Entero(reader, "TOTAL_REGISTROS"), Entero(reader, "PENDIENTES"),
                    Entero(reader, "CON_MOTIVO"), Entero(reader, "MANUALES"), Entero(reader, "CERRADOS_PASIVOS"));
                metadataStopwatch.Stop();
                Serilog.Log.Information(
                    "MonitoringMetadata completed: connectionOpenMs={ConnectionOpenMs} executeMs={ExecuteMs} totalMs={TotalMs}",
                    connectionStopwatch.ElapsedMilliseconds, executeStopwatch.ElapsedMilliseconds,
                    metadataStopwatch.ElapsedMilliseconds);
                return metadata;
            }
        }

        private static string ConstruirConsultaMonitoreoFiltrada(string baseSql, ConsultaMonitoreoPaginadaDto consulta)
        {
            var filteredSql = $"SELECT f.* FROM (SELECT q.*, CASE WHEN q.ES_MANUAL = 1 THEN 'manual' WHEN q.TIENE_MOTIVO = 1 THEN 'con_motivo' ELSE 'pendiente' END AS ESTADO_MONITOREO FROM ({baseSql}) q) f WHERE 1 = 1";
            AppendMonitoringFilters(ref filteredSql, consulta);
            return filteredSql;
        }

        private static string CrearClaveMetadataMonitoreo(string baseSql, ConsultaMonitoreoPaginadaDto consulta)
            => string.Join("|", ResolveMonitoringType(baseSql), consulta.Buscar?.Trim().ToUpperInvariant(),
                consulta.Estado?.Trim().ToLowerInvariant(), consulta.FechaDesde?.ToString("O"),
                consulta.FechaHasta?.ToString("O"));

        private static string ResolveMonitoringType(string baseSql)
            => baseSql.Contains("TIPO_EMPRESA_ID", StringComparison.OrdinalIgnoreCase)
                ? "juridica"
                : baseSql.Contains("V_EMPLEADOS_IHSS_PLANILLAS", StringComparison.OrdinalIgnoreCase)
                    ? "empleado"
                    : "natural";

        private async Task<List<T>> ObtenerMonitoreoCompletoAsync<T>(
            ConsultaMonitoreoPaginadaDto consulta,
            string baseSql,
            string orderBy,
            Func<OracleDataReader, T> map)
        {
            var filteredSql = $"SELECT f.* FROM (SELECT q.*, CASE WHEN q.ES_MANUAL = 1 THEN 'manual' WHEN q.TIENE_MOTIVO = 1 THEN 'con_motivo' ELSE 'pendiente' END AS ESTADO_MONITOREO FROM ({baseSql}) q) f WHERE 1 = 1";
            AppendMonitoringFilters(ref filteredSql, consulta);

            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            await using var command = conn.CreateCommand();
            command.BindByName = true;
            command.CommandText = $"{filteredSql} ORDER BY {orderBy}";
            AddMonitoringParameters(command, consulta);
            var items = new List<T>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) items.Add(map(reader));
            return items;
        }

        private async Task<PaginadoDto<T>> ExecutePageAsync<T>(
            OracleConnection conn,
            string filteredSql,
            string orderBy,
            int requestedPage,
            int requestedPageSize,
            Action<OracleCommand> addParameters,
            Func<OracleDataReader, T> map)
        {
            var pageSize = Math.Clamp(requestedPageSize, 1, 200);
            await using var countCommand = conn.CreateCommand();
            countCommand.BindByName = true;
            countCommand.CommandText = $"SELECT COUNT(*) FROM ({filteredSql}) count_query";
            addParameters(countCommand);
            var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
            var page = totalPages == 0 ? 1 : Math.Clamp(requestedPage, 1, totalPages);
            var firstRow = (page - 1) * pageSize;
            var lastRow = page * pageSize;

            var items = new List<T>();
            if (total > 0)
            {
                await using var pageCommand = conn.CreateCommand();
                pageCommand.BindByName = true;
                pageCommand.CommandText = $@"
                    SELECT *
                    FROM (
                        SELECT q.*, ROWNUM AS NUMERO_FILA
                        FROM (
                            {filteredSql}
                            ORDER BY {orderBy}
                        ) q
                        WHERE ROWNUM <= :filaFinal
                    )
                    WHERE NUMERO_FILA > :filaInicial";
                addParameters(pageCommand);
                pageCommand.Parameters.Add(new OracleParameter("filaFinal", lastRow));
                pageCommand.Parameters.Add(new OracleParameter("filaInicial", firstRow));
                await using var reader = await pageCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add(map(reader));
            }

            return new PaginadoDto<T>
            {
                Items = items,
                Pagina = page,
                TamanoPagina = pageSize,
                TotalRegistros = total,
                TotalPaginas = totalPages
            };
        }

        private static void AppendMonitoringFilters(ref string sql, ConsultaMonitoreoPaginadaDto consulta)
        {
            if (!string.IsNullOrWhiteSpace(consulta.Buscar))
                sql += " AND (UPPER(NOMBRE) LIKE '%' || UPPER(:buscar) || '%' OR UPPER(LISTA_CONCIDENCIA) LIKE '%' || UPPER(:buscar) || '%' OR UPPER(BUSQUEDA) LIKE '%' || UPPER(:buscar) || '%')";
            if (!string.IsNullOrWhiteSpace(consulta.Estado) && !consulta.Estado.Equals("todos", StringComparison.OrdinalIgnoreCase))
                sql += " AND ESTADO_MONITOREO = :estado";
            if (consulta.FechaDesde.HasValue)
                sql += " AND NVL(FECHA_ENCONTRO, FECHA_REGISTRO_INTERNO) >= TRUNC(:fechaDesde)";
            if (consulta.FechaHasta.HasValue)
                sql += " AND NVL(FECHA_ENCONTRO, FECHA_REGISTRO_INTERNO) < TRUNC(:fechaHasta) + 1";
        }

        private static void AddMonitoringParameters(OracleCommand command, ConsultaMonitoreoPaginadaDto consulta)
        {
            if (!string.IsNullOrWhiteSpace(consulta.Buscar)) command.Parameters.Add(new OracleParameter("buscar", consulta.Buscar.Trim()));
            if (!string.IsNullOrWhiteSpace(consulta.Estado) && !consulta.Estado.Equals("todos", StringComparison.OrdinalIgnoreCase)) command.Parameters.Add(new OracleParameter("estado", consulta.Estado));
            if (consulta.FechaDesde.HasValue) command.Parameters.Add(new OracleParameter("fechaDesde", consulta.FechaDesde.Value));
            if (consulta.FechaHasta.HasValue) command.Parameters.Add(new OracleParameter("fechaHasta", consulta.FechaHasta.Value));
        }

        private static string ConstruirConsultaMonitoreoJuridicas() => @"
            WITH POSITIVOS_AGG AS (
                SELECT LSP_NO_DOCUMENTO,
                       MIN(LSP_FECHA_CREACION) AS FECHA_REGISTRO_INTERNO,
                       MAX(CASE WHEN LSP_MOTIVO_INGRESO IS NOT NULL THEN 1 ELSE 0 END) AS TIENE_MOTIVO
                FROM RL_LISTA_POSITIVOS
                WHERE LSP_ESTADO_REGISTRO = 1 AND LSP_TIPO_POSITIVO_ID = 1
                GROUP BY LSP_NO_DOCUMENTO
            ), Coincidencias AS (
                SELECT D.RTN, D.NOMBRE, D.NUMEPATRO, R.LISTA_CONCIDENCIA, R.FECHA_ENCONTRO, R.FECHA_CALIFICO,
                       CASE
                           WHEN pp.FECHA_REGISTRO_INTERNO IS NULL THEN pr.FECHA_REGISTRO_INTERNO
                           WHEN pr.FECHA_REGISTRO_INTERNO IS NULL THEN pp.FECHA_REGISTRO_INTERNO
                           WHEN pp.FECHA_REGISTRO_INTERNO <= pr.FECHA_REGISTRO_INTERNO THEN pp.FECHA_REGISTRO_INTERNO
                           ELSE pr.FECHA_REGISTRO_INTERNO
                       END AS FECHA_REGISTRO_INTERNO,
                       D.ES_PROVEEDOR_IHSS,
                       GREATEST(NVL(pp.TIENE_MOTIVO, 0), NVL(pr.TIENE_MOTIVO, 0)) AS TIENE_MOTIVO
                FROM DNP_IHSS.V_DATOS_EMPRESA d
                INNER JOIN DNP_IHSS.REPORTE_COINCIDENCIAS r ON D.NUMEPATRO = R.NUMERO_PATRONO
                LEFT JOIN POSITIVOS_AGG pp ON pp.LSP_NO_DOCUMENTO = D.NUMEPATRO
                LEFT JOIN POSITIVOS_AGG pr ON pr.LSP_NO_DOCUMENTO = D.RTN
                WHERE D.TIPO_EMPRESA_ID = 1 AND R.TIPO_CALIFICACION_ID = 1)
            SELECT RTN, NOMBRE, NUMEPATRO, LISTA_CONCIDENCIA, FECHA_ENCONTRO, FECHA_CALIFICO, FECHA_REGISTRO_INTERNO, ES_PROVEEDOR_IHSS, TIENE_MOTIVO, 0 AS ES_MANUAL, RTN || ' ' || NUMEPATRO AS BUSQUEDA FROM Coincidencias
            UNION ALL
            SELECT lp.LSP_NO_DOCUMENTO, lp.LSP_NOMBRE_COMPLETO, lp.LSP_NO_DOCUMENTO, NVL(lc.LISTA_CAUTELA_DESCRICPION, 'MANUAL'), CAST(NULL AS DATE), CAST(NULL AS DATE), lp.LSP_FECHA_CREACION, 0, 1, 1, lp.LSP_NO_DOCUMENTO AS BUSQUEDA
            FROM RL_LISTA_POSITIVOS lp LEFT JOIN DNP_IHSS.TIPO_LISTAS_CAUTELA lc ON lp.LSP_TIPO_LISTA_CAUTELA_ID = lc.TIPO_LISTA_CAUTELA_ID
            WHERE lp.LSP_TIPO_POSITIVO_ID = 1 AND lp.LSP_ESTADO_REGISTRO = 1
              AND NOT EXISTS (SELECT 1 FROM Coincidencias c WHERE c.NUMEPATRO = lp.LSP_NO_DOCUMENTO OR c.RTN = lp.LSP_NO_DOCUMENTO)";

        private static string ConstruirConsultaMonitoreoNaturales() => @"
            WITH POSITIVOS_AGG AS (
                SELECT LSP_NO_DOCUMENTO,
                       MIN(LSP_FECHA_CREACION) AS FECHA_REGISTRO_INTERNO,
                       MAX(CASE WHEN LSP_MOTIVO_INGRESO IS NOT NULL THEN 1 ELSE 0 END) AS TIENE_MOTIVO
                FROM RL_LISTA_POSITIVOS
                WHERE LSP_ESTADO_REGISTRO = 1 AND LSP_TIPO_POSITIVO_ID = 2
                GROUP BY LSP_NO_DOCUMENTO
            ), REPORTE_AGG AS (
                SELECT DNI, LISTA_CONCIDENCIA, COUNT(*) TOTAL_REPETIDOS,
                       MAX(FECHA_ENCONTRO) FECHA_ENCONTRO, MAX(FECHA_CALIFICO) FECHA_CALIFICO
                FROM DNP_IHSS.REPORTE_COINCIDENCIAS
                WHERE TIPO_CALIFICACION_ID = 1 AND FECHA_CALIFICO IS NOT NULL
                GROUP BY DNI, LISTA_CONCIDENCIA
            ), REPORTE_IDS AS (
                SELECT DISTINCT DNI FROM REPORTE_AGG
            ), PERSONA_FUENTE AS (
                SELECT D.NUMERO_IDENTIFICACION, TRIM(D.NOMBRES_PERSONA) AS NOMBRES_PERSONA
                FROM DNP_IHSS.V_SOCIOS_REPRESENTANTES D
                INNER JOIN REPORTE_IDS I ON I.DNI = D.NUMERO_IDENTIFICACION
            ), PERSONA_FUENTE_DISTINCTA AS (
                SELECT NUMERO_IDENTIFICACION, NOMBRES_PERSONA
                FROM PERSONA_FUENTE
                GROUP BY NUMERO_IDENTIFICACION, NOMBRES_PERSONA
            ), PERSONAS AS (
                SELECT NUMERO_IDENTIFICACION,
                       TRIM(REGEXP_REPLACE(NOMBRES_PERSONA, '[[:space:]]+', ' ')) AS NOMBRE
                FROM PERSONA_FUENTE_DISTINCTA
                GROUP BY NUMERO_IDENTIFICACION,
                         TRIM(REGEXP_REPLACE(NOMBRES_PERSONA, '[[:space:]]+', ' '))
            ), Coincidencias AS (
                SELECT R.DNI AS NUMERO_IDENTIFICACION, D.NOMBRE, R.LISTA_CONCIDENCIA, R.TOTAL_REPETIDOS, R.FECHA_ENCONTRO, R.FECHA_CALIFICO,
                       p.FECHA_REGISTRO_INTERNO,
                       NVL(p.TIENE_MOTIVO, 0) AS TIENE_MOTIVO
                FROM REPORTE_AGG R
                INNER JOIN PERSONAS D ON D.NUMERO_IDENTIFICACION = R.DNI
                LEFT JOIN POSITIVOS_AGG p ON p.LSP_NO_DOCUMENTO = D.NUMERO_IDENTIFICACION
            )
            SELECT NUMERO_IDENTIFICACION, NOMBRE, LISTA_CONCIDENCIA, TOTAL_REPETIDOS, FECHA_ENCONTRO, FECHA_CALIFICO, FECHA_REGISTRO_INTERNO, TIENE_MOTIVO, 0 AS ES_MANUAL, NUMERO_IDENTIFICACION AS BUSQUEDA FROM Coincidencias
            UNION ALL
            SELECT lp.LSP_NO_DOCUMENTO, lp.LSP_NOMBRE_COMPLETO, NVL(lc.LISTA_CAUTELA_DESCRICPION, 'MANUAL'), 0, CAST(NULL AS DATE), CAST(NULL AS DATE), lp.LSP_FECHA_CREACION, 1, 1, lp.LSP_NO_DOCUMENTO AS BUSQUEDA
            FROM RL_LISTA_POSITIVOS lp LEFT JOIN DNP_IHSS.TIPO_LISTAS_CAUTELA lc ON lp.LSP_TIPO_LISTA_CAUTELA_ID = lc.TIPO_LISTA_CAUTELA_ID
            WHERE lp.LSP_TIPO_POSITIVO_ID = 2 AND lp.LSP_ESTADO_REGISTRO = 1
              AND NOT EXISTS (SELECT 1 FROM Coincidencias c WHERE c.NUMERO_IDENTIFICACION = lp.LSP_NO_DOCUMENTO)";

        private static string ConstruirConsultaMonitoreoEmpleados() => @"
            WITH POSITIVOS_AGG AS (
                SELECT LSP_NO_DOCUMENTO,
                       MIN(LSP_FECHA_CREACION) AS FECHA_REGISTRO_INTERNO,
                       MAX(CASE WHEN LSP_MOTIVO_INGRESO IS NOT NULL THEN 1 ELSE 0 END) AS TIENE_MOTIVO
                FROM RL_LISTA_POSITIVOS
                WHERE LSP_ESTADO_REGISTRO = 1 AND LSP_TIPO_POSITIVO_ID = 3
                GROUP BY LSP_NO_DOCUMENTO
            ), Coincidencias AS (
                SELECT D.IDENTIDAD, TRIM(REGEXP_REPLACE(D.NOMBRE_EMPLEADO, '[[:space:]]+', ' ')) NOMBRE, R.LISTA_CONCIDENCIA, COUNT(*) TOTAL_REPETIDOS, MAX(R.FECHA_ENCONTRO) FECHA_ENCONTRO, MAX(R.FECHA_CALIFICO) FECHA_CALIFICO,
                       p.FECHA_REGISTRO_INTERNO,
                       NVL(p.TIENE_MOTIVO, 0) AS TIENE_MOTIVO
                FROM DNP_IHSS.V_EMPLEADOS_IHSS_PLANILLAS D
                JOIN DNP_IHSS.REPORTE_COINCIDENCIAS R ON D.IDENTIDAD = R.DNI AND R.TIPO_CALIFICACION_ID = 1
                LEFT JOIN POSITIVOS_AGG p ON p.LSP_NO_DOCUMENTO = D.IDENTIDAD
                WHERE D.PERIODO = TRUNC(ADD_MONTHS(SYSDATE, -1), 'MM')
                GROUP BY D.IDENTIDAD, TRIM(REGEXP_REPLACE(D.NOMBRE_EMPLEADO, '[[:space:]]+', ' ')), R.LISTA_CONCIDENCIA, p.FECHA_REGISTRO_INTERNO, p.TIENE_MOTIVO)
            SELECT IDENTIDAD, NOMBRE, LISTA_CONCIDENCIA, TOTAL_REPETIDOS, FECHA_ENCONTRO, FECHA_CALIFICO, FECHA_REGISTRO_INTERNO, TIENE_MOTIVO, 0 AS ES_MANUAL, IDENTIDAD AS BUSQUEDA FROM Coincidencias
            UNION ALL
            SELECT lp.LSP_NO_DOCUMENTO, lp.LSP_NOMBRE_COMPLETO, NVL(lc.LISTA_CAUTELA_DESCRICPION, 'MANUAL'), 0, CAST(NULL AS DATE), CAST(NULL AS DATE), lp.LSP_FECHA_CREACION, 1, 1, lp.LSP_NO_DOCUMENTO AS BUSQUEDA
            FROM RL_LISTA_POSITIVOS lp LEFT JOIN DNP_IHSS.TIPO_LISTAS_CAUTELA lc ON lp.LSP_TIPO_LISTA_CAUTELA_ID = lc.TIPO_LISTA_CAUTELA_ID
            WHERE lp.LSP_TIPO_POSITIVO_ID = 3 AND lp.LSP_ESTADO_REGISTRO = 1
              AND NOT EXISTS (SELECT 1 FROM Coincidencias c WHERE c.IDENTIDAD = lp.LSP_NO_DOCUMENTO)";

        private static string Texto(DbDataReader reader, string column) => reader[column] == DBNull.Value ? string.Empty : reader[column]?.ToString() ?? string.Empty;
        private static int Entero(DbDataReader reader, string column) => reader[column] == DBNull.Value ? 0 : Convert.ToInt32(reader[column]);
        private static DateTime? Fecha(DbDataReader reader, string column) => reader[column] == DBNull.Value ? null : Convert.ToDateTime(reader[column]);
        private static string BoolTexto(DbDataReader reader, string column)
        {
            var value = Texto(reader, column).Trim().ToUpperInvariant();
            return value is "S" or "SI" or "1" ? "Si" : "No";
        }

        public async Task<PaginadoDto<CoincidenciaPatronoResumenDto>> ObtenerResumenCoincidenciasPatronoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta)
            => await ObtenerResumenCoincidenciasPaginadoAsync(consulta, "TIPO_PERSONA NOT LIKE '%IHSS'");

        public async Task<PaginadoDto<CoincidenciaPatronoResumenDto>> ObtenerResumenCoincidenciasEmpleadoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta)
            => await ObtenerResumenCoincidenciasPaginadoAsync(consulta, "TIPO_PERSONA LIKE '%IHSS'");

        private async Task<PaginadoDto<CoincidenciaPatronoResumenDto>> ObtenerResumenCoincidenciasPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta, string personFilter)
        {
            var sql = $"SELECT FECHA_ENCONTRO AS FechaEncontro, COUNT(*) AS CantidadRegistros FROM DNP_IHSS.V_REPORTE_COINCIDENCIA WHERE {personFilter}";
            if (!string.IsNullOrWhiteSpace(consulta.Buscar)) sql += " AND TO_CHAR(FECHA_ENCONTRO, 'YYYY-MM-DD') LIKE '%' || :buscar || '%'";
            sql += " GROUP BY FECHA_ENCONTRO";
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            return await ExecutePageAsync(conn, sql, "FECHA_ENCONTRO DESC", consulta.Pagina, consulta.TamanoPagina, command => { if (!string.IsNullOrWhiteSpace(consulta.Buscar)) command.Parameters.Add(new OracleParameter("buscar", consulta.Buscar.Trim())); }, reader => new CoincidenciaPatronoResumenDto { FechaEncontro = Fecha(reader, "FechaEncontro"), CantidadRegistros = Entero(reader, "CantidadRegistros") });
        }

        public Task<PaginadoDto<CoincidenciaPatronoDetalleDto>> ObtenerDetalleCoincidenciasPatronoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta)
            => ObtenerDetalleCoincidenciasPaginadoAsync(consulta, "TIPO_PERSONA NOT LIKE '%IHSS'");

        public Task<PaginadoDto<CoincidenciaPatronoDetalleDto>> ObtenerDetalleCoincidenciasEmpleadoPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta)
            => ObtenerDetalleCoincidenciasPaginadoAsync(consulta, "TIPO_PERSONA LIKE '%IHSS'");

        private async Task<PaginadoDto<CoincidenciaPatronoDetalleDto>> ObtenerDetalleCoincidenciasPaginadoAsync(ConsultaCoincidenciasPaginadaDto consulta, string personFilter)
        {
            var sql = $@"SELECT REPORTE_COINCIDENCIA_ID, DATA_ID, DNI, FECHA_ENCONTRO, LISTA_CONCIDENCIA, NACIONALIDAD, NOMBRE, NUMERO_PATRONO, OBSERVACION_LISTA, TIPO_PERSONA, USUARIO_ENCONTRO, TIPO_CALIFICACION
                FROM DNP_IHSS.V_REPORTE_COINCIDENCIA WHERE {personFilter} AND TRUNC(FECHA_ENCONTRO) = TO_DATE(:fecha, 'YYYY-MM-DD')";
            if (!string.IsNullOrWhiteSpace(consulta.Buscar)) sql += " AND (UPPER(NOMBRE) LIKE '%' || UPPER(:buscar) || '%' OR UPPER(DNI) LIKE '%' || UPPER(:buscar) || '%' OR UPPER(NUMERO_PATRONO) LIKE '%' || UPPER(:buscar) || '%' OR UPPER(LISTA_CONCIDENCIA) LIKE '%' || UPPER(:buscar) || '%' OR UPPER(TIPO_PERSONA) LIKE '%' || UPPER(:buscar) || '%')";
            if (!string.IsNullOrWhiteSpace(consulta.Calificacion)) sql += " AND UPPER(TIPO_CALIFICACION) = UPPER(:calificacion)";
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            return await ExecutePageAsync(conn, sql, "NOMBRE ASC, REPORTE_COINCIDENCIA_ID ASC", consulta.Pagina, consulta.TamanoPagina, command =>
            {
                command.Parameters.Add(new OracleParameter("fecha", consulta.Fecha ?? string.Empty));
                if (!string.IsNullOrWhiteSpace(consulta.Buscar)) command.Parameters.Add(new OracleParameter("buscar", consulta.Buscar.Trim()));
                if (!string.IsNullOrWhiteSpace(consulta.Calificacion)) command.Parameters.Add(new OracleParameter("calificacion", consulta.Calificacion.Trim()));
            }, reader => new CoincidenciaPatronoDetalleDto
            {
                ReporteCoincidenciaId = Convert.ToInt64(reader["REPORTE_COINCIDENCIA_ID"]), DataId = reader["DATA_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["DATA_ID"]), Dni = Texto(reader, "DNI"), FechaEncontro = Fecha(reader, "FECHA_ENCONTRO"), ListaCoincidencia = Texto(reader, "LISTA_CONCIDENCIA"), Nacionalidad = Texto(reader, "NACIONALIDAD"), Nombre = Texto(reader, "NOMBRE"), NumeroPatrono = Texto(reader, "NUMERO_PATRONO"), ObservacionLista = Texto(reader, "OBSERVACION_LISTA"), TipoPersona = Texto(reader, "TIPO_PERSONA"), UsuarioEncontro = reader["USUARIO_ENCONTRO"] == DBNull.Value ? 0 : Convert.ToInt64(reader["USUARIO_ENCONTRO"]), TipoCalificacion = Texto(reader, "TIPO_CALIFICACION")
            });
        }

        public async Task<List<CoincidenciaPatronoDetalleDto>> ObtenerDetalleCoincidenciasPatronoParaExportarAsync(string fecha)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    REPORTE_COINCIDENCIA_ID, DATA_ID, DNI, FECHA_ENCONTRO,
                    LISTA_CONCIDENCIA, NACIONALIDAD, NOMBRE, NUMERO_PATRONO,
                    OBSERVACION_LISTA, TIPO_PERSONA, USUARIO_ENCONTRO, TIPO_CALIFICACION
                FROM DNP_IHSS.V_REPORTE_COINCIDENCIA
                WHERE TIPO_PERSONA NOT LIKE '%IHSS'
                  AND TRUNC(FECHA_ENCONTRO) = TO_DATE(:fecha, 'YYYY-MM-DD')
                ORDER BY NOMBRE ASC";

            cmd.Parameters.Add(new OracleParameter("fecha", fecha));

            var list = new List<CoincidenciaPatronoDetalleDto>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new CoincidenciaPatronoDetalleDto
                {
                    ReporteCoincidenciaId = Convert.ToInt64(reader["REPORTE_COINCIDENCIA_ID"]),
                    DataId = reader["DATA_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["DATA_ID"]),
                    Dni = reader["DNI"]?.ToString() ?? string.Empty,
                    FechaEncontro = reader["FECHA_ENCONTRO"] == DBNull.Value ? null : Convert.ToDateTime(reader["FECHA_ENCONTRO"]),
                    ListaCoincidencia = reader["LISTA_CONCIDENCIA"]?.ToString() ?? string.Empty,
                    Nacionalidad = reader["NACIONALIDAD"]?.ToString() ?? string.Empty,
                    Nombre = reader["NOMBRE"]?.ToString() ?? string.Empty,
                    NumeroPatrono = reader["NUMERO_PATRONO"]?.ToString() ?? string.Empty,
                    ObservacionLista = reader["OBSERVACION_LISTA"]?.ToString() ?? string.Empty,
                    TipoPersona = reader["TIPO_PERSONA"]?.ToString() ?? string.Empty,
                    UsuarioEncontro = reader["USUARIO_ENCONTRO"] == DBNull.Value ? 0 : Convert.ToInt64(reader["USUARIO_ENCONTRO"]),
                    TipoCalificacion = reader["TIPO_CALIFICACION"]?.ToString() ?? string.Empty
                });
            }
            return list;
        }

        public async Task<List<CoincidenciaPatronoDetalleDto>> ObtenerDetalleCoincidenciasEmpleadoParaExportarAsync(string fecha)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    REPORTE_COINCIDENCIA_ID, DATA_ID, DNI, FECHA_ENCONTRO,
                    LISTA_CONCIDENCIA, NACIONALIDAD, NOMBRE, NUMERO_PATRONO,
                    OBSERVACION_LISTA, TIPO_PERSONA, USUARIO_ENCONTRO, TIPO_CALIFICACION
                FROM DNP_IHSS.V_REPORTE_COINCIDENCIA
                WHERE TIPO_PERSONA LIKE '%IHSS'
                  AND TRUNC(FECHA_ENCONTRO) = TO_DATE(:fecha, 'YYYY-MM-DD')
                ORDER BY NOMBRE ASC";

            cmd.Parameters.Add(new OracleParameter("fecha", fecha));

            var list = new List<CoincidenciaPatronoDetalleDto>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new CoincidenciaPatronoDetalleDto
                {
                    ReporteCoincidenciaId = Convert.ToInt64(reader["REPORTE_COINCIDENCIA_ID"]),
                    DataId = reader["DATA_ID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["DATA_ID"]),
                    Dni = reader["DNI"]?.ToString() ?? string.Empty,
                    FechaEncontro = reader["FECHA_ENCONTRO"] == DBNull.Value ? null : Convert.ToDateTime(reader["FECHA_ENCONTRO"]),
                    ListaCoincidencia = reader["LISTA_CONCIDENCIA"]?.ToString() ?? string.Empty,
                    Nacionalidad = reader["NACIONALIDAD"]?.ToString() ?? string.Empty,
                    Nombre = reader["NOMBRE"]?.ToString() ?? string.Empty,
                    NumeroPatrono = reader["NUMERO_PATRONO"]?.ToString() ?? string.Empty,
                    ObservacionLista = reader["OBSERVACION_LISTA"]?.ToString() ?? string.Empty,
                    TipoPersona = reader["TIPO_PERSONA"]?.ToString() ?? string.Empty,
                    UsuarioEncontro = reader["USUARIO_ENCONTRO"] == DBNull.Value ? 0 : Convert.ToInt64(reader["USUARIO_ENCONTRO"]),
                    TipoCalificacion = reader["TIPO_CALIFICACION"]?.ToString() ?? string.Empty
                });
            }
            return list;
        }

        public async Task<bool> CalificarCoincidenciaAsync(long reporteCoincidenciaId, int tipoCalificacionId, long usuarioId, bool esEmpleado)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // 1. Leer calificación anterior (si existe) para auditoría
            string? anteriorDataJson = null;
            await using (var getCmd = conn.CreateCommand())
            {
                getCmd.CommandText = @"
                    SELECT REPORTE_COINCIDENCIA_ID, TIPO_CALIFICACION_ID, USUARIO_CALIFICO, FECHA_CALIFICO
                    FROM DNP_IHSS.REPORTE_COINCIDENCIAS
                    WHERE REPORTE_COINCIDENCIA_ID = :id";
                getCmd.Parameters.Add(new OracleParameter("id", reporteCoincidenciaId));
                await using var reader = await getCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    anteriorDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        ReporteCoincidenciaId = reporteCoincidenciaId,
                        TipoCalificacionId    = reader["TIPO_CALIFICACION_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["TIPO_CALIFICACION_ID"]),
                        UsuarioCalifico       = reader["USUARIO_CALIFICO"]     == DBNull.Value ? (long?)null : Convert.ToInt64(reader["USUARIO_CALIFICO"]),
                        FechaCalifico         = reader["FECHA_CALIFICO"]       == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FECHA_CALIFICO"])
                    });
                }
            }

            // 2. Actualizar DNP_IHSS.REPORTE_COINCIDENCIAS
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE DNP_IHSS.REPORTE_COINCIDENCIAS
                SET TIPO_CALIFICACION_ID = :tipoCalificacionId,
                    USUARIO_CALIFICO     = :usuarioId,
                    FECHA_CALIFICO       = SYSDATE
                WHERE REPORTE_COINCIDENCIA_ID = :reporteId
                  AND EXISTS (
                      SELECT 1
                      FROM DNP_IHSS.V_REPORTE_COINCIDENCIA V
                      WHERE V.REPORTE_COINCIDENCIA_ID = :reporteIdVista
                        AND (
                            (:esEmpleado = 1 AND V.TIPO_PERSONA LIKE '%IHSS')
                            OR (:esEmpleado = 0 AND V.TIPO_PERSONA NOT LIKE '%IHSS')
                        )
                  )";

            cmd.Parameters.Add(new OracleParameter("tipoCalificacionId", tipoCalificacionId));
            cmd.Parameters.Add(new OracleParameter("usuarioId",          usuarioId));
            cmd.Parameters.Add(new OracleParameter("reporteId",          reporteCoincidenciaId));
            cmd.Parameters.Add(new OracleParameter("reporteIdVista",     reporteCoincidenciaId));
            cmd.Parameters.Add(new OracleParameter("esEmpleado",         esEmpleado ? 1 : 0));

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0)
            {
                var nuevoDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    ReporteCoincidenciaId = reporteCoincidenciaId,
                    TipoCalificacionId    = tipoCalificacionId,
                    UsuarioCalifico       = usuarioId,
                    FechaCalifico         = DateTime.Now
                });
                await _auditoriaRepo.RegistrarAsync(
                    "DNP_IHSS.REPORTE_COINCIDENCIAS",
                    reporteCoincidenciaId.ToString(),
                    "UPDATE",
                    anteriorDataJson,
                    nuevoDataJson,
                    usuarioId, null, null,
                    "Coincidencias");
                _cache.Invalidate(ApplicationCacheScopes.MonitoreoMetadata);
                return true;
            }
            return false;
        }


        public async Task<string> ObtenerResumenMatchListaAsync(long dataId, string nombre)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // We implement the matching logic of F_CREAR_WHERE_2 directly in C# 
            // because the RIESGO_LAVADO database user doesn't have EXECUTE permissions on it.
            string whereClause = RebuildWhereClause(nombre);

            // 2. Ejecutar consulta sobre LISTA_CAUTELA
            var sb = new System.Text.StringBuilder();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $@"
                    SELECT 
                        REPLACE(L.RESUMEN, '^', '<br>') AS DETALLE, 
                        (SELECT T.LISTA_CAUTELA_DESCRICPION 
                         FROM DNP_IHSS.TIPO_LISTAS_CAUTELA T 
                         WHERE T.TIPO_LISTA_CAUTELA_ID = L.TIPO_LISTA_CAUTELA_ID) AS LISTA  
                    FROM DNP_IHSS.LISTA_CAUTELA L 
                    WHERE L.DATA_ID = :dataId OR {whereClause}";

                cmd.Parameters.Add(new OracleParameter("dataId", dataId));

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string lista = reader["LISTA"]?.ToString() ?? "Desconocida";
                    string detalle = reader["DETALLE"]?.ToString() ?? string.Empty;
                    sb.AppendFormat("Lista Origen: <b>{0}</b><br>{1}<br><br>", lista, detalle);
                }
            }

            return sb.ToString();
        }

        private static string RebuildWhereClause(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return "1=0";

            var tokens = nombre.Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 2)
                .Select(t => t.Replace("'", "''"))
                .ToList();

            if (tokens.Count == 0)
                return "1=0";

            var clauses = tokens.Select(t => $"lower(L.resumen) like lower('%{t}%')");
            return string.Join(" and ", clauses);
        }

        public async Task<(bool EsValido, string Mensaje)> ValidarArchivoCautelaAsync(Microsoft.AspNetCore.Http.IFormFile archivo, int tipoListaCautelaId)
        {
            if (archivo == null || archivo.Length == 0)
                return (false, "El archivo está vacío o no fue seleccionado.");

            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TIPO_ARCHIVO FROM DNP_IHSS.TIPO_LISTAS_CAUTELA WHERE TIPO_LISTA_CAUTELA_ID = :id";
            cmd.Parameters.Add(new OracleParameter("id", tipoListaCautelaId));

            string? tipoArchivoEsperado = null;

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                tipoArchivoEsperado = reader["TIPO_ARCHIVO"]?.ToString();
            }

            if (string.IsNullOrWhiteSpace(tipoArchivoEsperado))
            {
                return (false, "No se encontró la configuración del tipo de lista de cautela.");
            }

            var extension = System.IO.Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var tipoEsperadoNormalizado = tipoArchivoEsperado.ToLowerInvariant().Replace(".", "");
            var extNormalizada = extension.Replace(".", "");

            // Permitir variaciones de excel
            if (tipoEsperadoNormalizado == "xls" || tipoEsperadoNormalizado == "xlsx")
            {
                if (extNormalizada != "xls" && extNormalizada != "xlsx")
                {
                    return (false, $"El tipo de archivo ({extension}) no coincide con el formato de Excel esperado.");
                }
            }
            else if (extNormalizada != tipoEsperadoNormalizado)
            {
                return (false, $"El tipo de archivo ({extension}) no coincide con el formato esperado (.{tipoEsperadoNormalizado}).");
            }

            return (true, "El archivo es válido.");
        }

        private string GetNacionalidadOFAC(string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                var arr = str.Split(';');
                foreach (var item in arr)
                {
                    if (item.ToUpper().Contains("NATIONALITY"))
                    {
                        var nacionalidad = item.Remove(0, 12).Trim();
                        return nacionalidad.ToUpper();
                    }
                }
            }
            return string.Empty;
        }

        private async Task<int> ContarRegistrosListaCautelaAsync(OracleConnection conn, int tipoListaCautelaId)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM DNP_IHSS.LISTA_CAUTELA WHERE TIPO_LISTA_CAUTELA_ID = :id";
            cmd.Parameters.Add(new OracleParameter("id", tipoListaCautelaId));
            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }

        private async Task ReemplazarRegistrosListaCautelaAsync(
            OracleConnection conn,
            OracleTransaction tx,
            int tipoListaCautelaId,
            long usuarioId,
            List<long> listDataId,
            List<string> listT1,
            List<string> listT2,
            List<string> listT3,
            List<string> listT4,
            List<string> listT5,
            List<string> listT6,
            List<string> listT7,
            List<string> listT8,
            List<string> listT9,
            List<string> listT10,
            List<string> listT11,
            List<string> listResumen)
        {
            await using (var cmdDel = conn.CreateCommand())
            {
                cmdDel.Transaction = tx;
                cmdDel.CommandText = "DELETE FROM DNP_IHSS.LISTA_CAUTELA WHERE TIPO_LISTA_CAUTELA_ID = :id";
                cmdDel.Parameters.Add(new OracleParameter("id", tipoListaCautelaId));
                await cmdDel.ExecuteNonQueryAsync();
            }

            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO DNP_IHSS.LISTA_CAUTELA (
                    LISTA_CAUTELA_ID, TIPO_LISTA_CAUTELA_ID, DATA_ID, TEXTO1, TEXTO2, TEXTO3, TEXTO4, TEXTO5,
                    TEXTO6, TEXTO7, TEXTO8, TEXTO9, TEXT10, TEXTO11,
                    RESUMEN, USUARIO_CREO, FECHA_CREACION, ESTADO_REGISTRO
                ) VALUES (
                    DNP_IHSS.LISTA_CAUTELA_SEQ.NEXTVAL, :tipoLista, :dataId, :t1, :t2, :t3, :t4, :t5,
                    :t6, :t7, :t8, :t9, :t10, :t11,
                    :resumen, :usrLog, SYSDATE, 1
                )";

            cmd.ArrayBindCount = listDataId.Count;
            cmd.Parameters.Add(new OracleParameter("tipoLista", OracleDbType.Int32) { Value = System.Linq.Enumerable.Repeat(tipoListaCautelaId, listDataId.Count).ToArray() });
            cmd.Parameters.Add(new OracleParameter("dataId", OracleDbType.Int64) { Value = listDataId.ToArray() });
            cmd.Parameters.Add(new OracleParameter("t1", OracleDbType.Varchar2) { Value = listT1.ToArray() });
            cmd.Parameters.Add(new OracleParameter("t2", OracleDbType.Varchar2) { Value = listT2.ToArray() });
            cmd.Parameters.Add(new OracleParameter("t3", OracleDbType.Varchar2) { Value = listT3.ToArray() });
            cmd.Parameters.Add(new OracleParameter("t4", OracleDbType.Varchar2) { Value = listT4.ToArray() });
            cmd.Parameters.Add(new OracleParameter("t5", OracleDbType.Varchar2) { Value = listT5.ToArray() });
            cmd.Parameters.Add(new OracleParameter("t6", OracleDbType.Varchar2) { Value = listT6.ToArray() });
            cmd.Parameters.Add(new OracleParameter("t7", OracleDbType.Varchar2) { Value = listT7.ToArray() });
            cmd.Parameters.Add(new OracleParameter("t8", OracleDbType.Varchar2) { Value = listT8.ToArray() });
            cmd.Parameters.Add(new OracleParameter("t9", OracleDbType.Varchar2) { Value = listT9.ToArray() });
            cmd.Parameters.Add(new OracleParameter("t10", OracleDbType.Varchar2) { Value = listT10.ToArray() });
            cmd.Parameters.Add(new OracleParameter("t11", OracleDbType.Varchar2) { Value = listT11.ToArray() });
            cmd.Parameters.Add(new OracleParameter("resumen", OracleDbType.Varchar2) { Value = listResumen.ToArray() });
            cmd.Parameters.Add(new OracleParameter("usrLog", OracleDbType.Int64) { Value = System.Linq.Enumerable.Repeat(usuarioId, listDataId.Count).ToArray() });

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task RegistrarAuditoriaCargaListaAsync(OracleConnection conn, OracleTransaction tx, int tipoListaCautelaId, long usuarioId, string nombreArchivo, string extension, int registrosAnteriores, int registrosNuevos)
        {
            var dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Accion = "CARGA_LISTA_CAUTELA",
                TipoListaCautelaId = tipoListaCautelaId,
                NombreArchivo = nombreArchivo,
                Extension = extension,
                RegistrosAnteriores = registrosAnteriores,
                RegistrosNuevos = registrosNuevos,
                Resultado = "EXITOSO",
                FechaCarga = DateTime.Now
            });

            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO RL_AUDITORIA (
                    AUD_ID, AUD_TABLA, AUD_REGISTRO_ID, AUD_ACCION,
                    AUD_DATOS_ANT, AUD_DATOS_NVO, AUD_USR_ID, AUD_USR_EMAIL,
                    AUD_IP, AUD_FECHA, AUD_MODULO
                ) VALUES (
                    SEQ_RL_AUDITORIA.NEXTVAL, :tabla, :regId, :accion,
                    :datosAnt, :datosNvo, :usrId, :email,
                    :ip, SYSDATE, :modulo
                )";
            cmd.Parameters.Add(new OracleParameter("tabla", "DNP_IHSS.LISTA_CAUTELA"));
            cmd.Parameters.Add(new OracleParameter("regId", tipoListaCautelaId.ToString()));
            cmd.Parameters.Add(new OracleParameter("accion", "UPLOAD"));
            cmd.Parameters.Add(new OracleParameter("datosAnt", DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("datosNvo", dataJson));
            cmd.Parameters.Add(new OracleParameter("usrId", usuarioId));
            cmd.Parameters.Add(new OracleParameter("email", DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("ip", (object?)ObtenerIpCliente() ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("modulo", "CargaListas"));

            await cmd.ExecuteNonQueryAsync();
        }

        private string? ObtenerIpCliente() => _httpContextAccessor.HttpContext?.GetClientIp();

        private static string ConstruirMensajeCargaExitosa(int registrosAnteriores, int registrosNuevos)
            => $"Carga exitosa. Se reemplazaron {registrosAnteriores} registros anteriores por {registrosNuevos} registros nuevos.";

        public async Task<(bool Success, string Mensaje)> ProcesarArchivoCsvOfacAsync(Microsoft.AspNetCore.Http.IFormFile archivo, int tipoListaCautelaId, long usuarioId)
        {
            try
            {
                await using var conn = _db.CreateConnection();
                await conn.OpenAsync();
                var registrosAnteriores = await ContarRegistrosListaCautelaAsync(conn, tipoListaCautelaId);

                // 1. Procesar CSV y preparar registros antes de reemplazar la lista actual.
                using var stream = archivo.OpenReadStream();
                using var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(stream);
                parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                parser.SetDelimiters(",");

                var listDataId = new List<long>();
                var listT1 = new List<string>();
                var listT2 = new List<string>();
                var listT3 = new List<string>();
                var listT4 = new List<string>();
                var listT5 = new List<string>();
                var listT6 = new List<string>();
                var listT7 = new List<string>();
                var listT8 = new List<string>();
                var listT9 = new List<string>();
                var listT10 = new List<string>();
                var listT11 = new List<string>();
                var listResumen = new List<string>();

                while (!parser.EndOfData)
                {
                    try
                    {
                        var currentRow = parser.ReadFields();
                        if (currentRow != null && currentRow.Length != 1)
                        {
                            var dataIdStr = currentRow[0];
                            if (string.IsNullOrWhiteSpace(dataIdStr)) continue;

                            if (!long.TryParse(dataIdStr, out long dataId))
                                continue;

                            string texto1 = currentRow.Length > 1 ? currentRow[1].ToUpper() : "";
                            string texto2 = currentRow.Length > 2 ? currentRow[2].ToUpper() : "";
                            string texto3 = currentRow.Length > 11 ? GetNacionalidadOFAC(currentRow[11]) : "";
                            string texto4 = currentRow.Length > 4 ? currentRow[4].ToUpper() : "";
                            string texto5 = currentRow.Length > 5 ? currentRow[5].ToUpper() : "";
                            string texto6 = currentRow.Length > 6 ? currentRow[6].ToUpper() : "";
                            string texto7 = currentRow.Length > 7 ? currentRow[7].ToUpper() : "";
                            string texto8 = currentRow.Length > 8 ? currentRow[8].ToUpper() : "";
                            string texto9 = currentRow.Length > 9 ? currentRow[9].ToUpper() : "";
                            string texto10 = currentRow.Length > 10 ? currentRow[10].ToUpper() : "";
                            string texto11 = currentRow.Length > 11 ? currentRow[11].ToUpper().Replace("-", "") : "";

                            string resumen = $"{dataId}^{texto1}^{texto2}^{texto3}^{texto4}^{texto5}^{texto6}^{texto7}^{texto8}^{texto9}^{texto10}^{texto11}".ToUpper();

                            listDataId.Add(dataId);
                            listT1.Add(texto1);
                            listT2.Add(texto2);
                            listT3.Add(texto3);
                            listT4.Add(texto4);
                            listT5.Add(texto5);
                            listT6.Add(texto6);
                            listT7.Add(texto7);
                            listT8.Add(texto8);
                            listT9.Add(texto9);
                            listT10.Add(texto10);
                            listT11.Add(texto11);
                            listResumen.Add(resumen);
                        }
                    }
                    catch (Microsoft.VisualBasic.FileIO.MalformedLineException ex)
                    {
                        return (false, "Línea malformada en el archivo: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Error preparando registro OFAC");
                    }
                }
                
                if (listDataId.Count == 0)
                    return (false, "No se cargo la lista porque el archivo no contiene registros validos.");

                using var tx = conn.BeginTransaction();
                try
                {
                    await ReemplazarRegistrosListaCautelaAsync(conn, tx, tipoListaCautelaId, usuarioId, listDataId, listT1, listT2, listT3, listT4, listT5, listT6, listT7, listT8, listT9, listT10, listT11, listResumen);
                    await RegistrarAuditoriaCargaListaAsync(conn, tx, tipoListaCautelaId, usuarioId, archivo.FileName, System.IO.Path.GetExtension(archivo.FileName), registrosAnteriores, listDataId.Count);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }

                return (true, ConstruirMensajeCargaExitosa(registrosAnteriores, listDataId.Count));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al intentar procesar la lista de cautela OFAC.");
                return (false, "Error al intentar cargar la lista de cautela.");
            }
        }

        public async Task<(bool Success, string Mensaje)> ProcesarArchivoXmlOnuAsync(Microsoft.AspNetCore.Http.IFormFile archivo, int tipoListaCautelaId, long usuarioId)
        {
            try
            {
                await using var conn = _db.CreateConnection();
                await conn.OpenAsync();
                var registrosAnteriores = await ContarRegistrosListaCautelaAsync(conn, tipoListaCautelaId);

                // 1. Procesar XML y preparar registros antes de reemplazar la lista actual.
                var docXml = new System.Xml.XmlDocument();
                using (var stream = archivo.OpenReadStream())
                {
                    docXml.Load(stream);
                }

                var listDataId = new List<long>();
                var listT1 = new List<string>();
                var listT2 = new List<string>();
                var listT3 = new List<string>();
                var listT4 = new List<string>();
                var listT5 = new List<string>();
                var listT6 = new List<string>();
                var listT7 = new List<string>();
                var listT8 = new List<string>();
                var listT9 = new List<string>();
                var listT10 = new List<string>();
                var listT11 = new List<string>();
                var listResumen = new List<string>();

                string Safe(string? val) => string.IsNullOrWhiteSpace(val) ? "-0-" : val;

                // INDIVIDUALS
                var individuals = docXml.SelectNodes("/CONSOLIDATED_LIST/INDIVIDUALS/INDIVIDUAL");
                if (individuals != null)
                {
                    foreach (System.Xml.XmlNode nodo in individuals)
                    {
                        var dataIdStr = nodo["DATAID"]?.InnerText;
                        if (!long.TryParse(dataIdStr, out long dataId)) continue;

                        string t0 = nodo["FIRST_NAME"]?.InnerText ?? "";
                        string t1 = nodo["SECOND_NAME"]?.InnerText ?? "";
                        string t2 = nodo["THIRD_NAME"]?.InnerText ?? "";
                        string texto1 = $"{t0}, {t1} {t2}".Trim();
                        string texto2 = "INDIVIDUAL";
                        string texto3 = Safe(nodo["NATIONALITY"]?.InnerText);
                        string texto4 = Safe(nodo["LISTED_ON"]?.InnerText);

                        string texto5 = "-0-";
                        var designation = nodo["DESIGNATION"];
                        if (designation != null && designation.HasChildNodes)
                        {
                            var ds = new List<string>();
                            foreach (System.Xml.XmlNode child in designation.ChildNodes) ds.Add(child.InnerText);
                            texto5 = string.Join(", ", ds);
                        }

                        string texto6 = "-0-";
                        var individualAlias = nodo["INDIVIDUAL_ALIAS"];
                        if (individualAlias != null && individualAlias.HasChildNodes)
                        {
                            texto6 = individualAlias.ChildNodes[0]?.InnerText ?? "-0-";
                        }

                        string texto7 = "-0-";
                        var dateOfBirth = nodo["INDIVIDUAL_DATE_OF_BIRTH"];
                        if (dateOfBirth != null && dateOfBirth.ChildNodes.Count > 1)
                        {
                            t0 = dateOfBirth.ChildNodes[0]?.InnerText ?? "";
                            t1 = dateOfBirth.ChildNodes[1]?.InnerText ?? "";
                            texto7 = $"{t0}, {t1}";
                        }

                        string texto8 = "-0-";
                        var placeOfBirth = nodo["INDIVIDUAL_PLACE_OF_BIRTH"];
                        if (placeOfBirth != null && placeOfBirth.ChildNodes.Count > 1)
                        {
                            t0 = "CITY: " + (placeOfBirth.ChildNodes[0]?.InnerText ?? "");
                            t1 = "COUNTRY: " + (placeOfBirth.ChildNodes[1]?.InnerText ?? "");
                            texto8 = $"{t0}, {t1}";
                        }

                        string texto9 = "-0-";
                        var individualDocument = nodo["INDIVIDUAL_DOCUMENT"];
                        if (individualDocument != null && individualDocument.ChildNodes.Count > 1)
                        {
                            t0 = individualDocument.ChildNodes[0]?.InnerText ?? "";
                            t1 = individualDocument.ChildNodes[1]?.InnerText ?? "";
                            texto9 = $"{t0}, {t1}";
                        }

                        string texto10 = "-0-";
                        string texto11 = "-0-";

                        string resumen = $"{dataId}^{texto1}^{texto2}^{texto3}^{texto4}^{texto5}^{texto6}^{texto7}^{texto8}^{texto9}^{texto10}^{texto11}".ToUpper();

                        listDataId.Add(dataId);
                        listT1.Add(texto1);
                        listT2.Add(texto2);
                        listT3.Add(texto3);
                        listT4.Add(texto4);
                        listT5.Add(texto5);
                        listT6.Add(texto6);
                        listT7.Add(texto7);
                        listT8.Add(texto8);
                        listT9.Add(texto9);
                        listT10.Add(texto10);
                        listT11.Add(texto11);
                        listResumen.Add(resumen);
                    }
                }

                // ENTITIES
                var entities = docXml.SelectNodes("/CONSOLIDATED_LIST/ENTITIES/ENTITY");
                if (entities != null)
                {
                    foreach (System.Xml.XmlNode nodo in entities)
                    {
                        var dataIdStr = nodo["DATAID"]?.InnerText;
                        if (!long.TryParse(dataIdStr, out long dataId)) continue;

                        string texto1 = nodo["FIRST_NAME"]?.InnerText?.Trim() ?? "";
                        string texto2 = "ENTITY";
                        string texto3 = "-0-";
                        string texto4 = Safe(nodo["LISTED_ON"]?.InnerText);
                        string texto5 = "-0-";

                        string texto6 = "-0-";
                        var entityAlias = nodo["ENTITY_ALIAS"];
                        if (entityAlias != null && entityAlias.ChildNodes.Count > 1)
                        {
                            texto6 = entityAlias.ChildNodes[1]?.InnerText ?? "-0-";
                        }

                        string texto7 = "-0-";

                        string texto8 = "-0-";
                        var entityAddress = nodo["ENTITY_ADDRESS"];
                        if (entityAddress != null && entityAddress.ChildNodes.Count > 1)
                        {
                            string t0 = "STREET: " + (entityAddress.ChildNodes[0]?.InnerText ?? "");
                            string t1 = "CITY: " + (entityAddress.ChildNodes[1]?.InnerText ?? "");
                            texto8 = $"{t0}, {t1}";
                        }

                        string texto9 = "-0-";
                        string texto10 = Safe(nodo["COMMENTS1"]?.InnerText);
                        string texto11 = "-0-";

                        string resumen = $"{dataId}^{texto1}^{texto2}^{texto3}^{texto4}^{texto5}^{texto6}^{texto7}^{texto8}^{texto9}^{texto10}^{texto11}".ToUpper();

                        listDataId.Add(dataId);
                        listT1.Add(texto1);
                        listT2.Add(texto2);
                        listT3.Add(texto3);
                        listT4.Add(texto4);
                        listT5.Add(texto5);
                        listT6.Add(texto6);
                        listT7.Add(texto7);
                        listT8.Add(texto8);
                        listT9.Add(texto9);
                        listT10.Add(texto10);
                        listT11.Add(texto11);
                        listResumen.Add(resumen);
                    }
                }

                if (listDataId.Count == 0)
                    return (false, "No se cargo la lista porque el archivo no contiene registros validos.");

                using var tx = conn.BeginTransaction();
                try
                {
                    await ReemplazarRegistrosListaCautelaAsync(conn, tx, tipoListaCautelaId, usuarioId, listDataId, listT1, listT2, listT3, listT4, listT5, listT6, listT7, listT8, listT9, listT10, listT11, listResumen);
                    await RegistrarAuditoriaCargaListaAsync(conn, tx, tipoListaCautelaId, usuarioId, archivo.FileName, System.IO.Path.GetExtension(archivo.FileName), registrosAnteriores, listDataId.Count);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }

                return (true, ConstruirMensajeCargaExitosa(registrosAnteriores, listDataId.Count));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al intentar procesar la lista de cautela ONU.");
                return (false, "Error al intentar cargar la lista de cautela.");
            }
        }

        public async Task<(bool Success, string Mensaje)> ProcesarArchivoExcelEngelAsync(Microsoft.AspNetCore.Http.IFormFile archivo, int tipoListaCautelaId, long usuarioId)
        {
            try
            {
                await using var conn = _db.CreateConnection();
                await conn.OpenAsync();
                var registrosAnteriores = await ContarRegistrosListaCautelaAsync(conn, tipoListaCautelaId);

                // 1. Procesar Excel y preparar registros antes de reemplazar la lista actual.
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using var stream = archivo.OpenReadStream();
                using var excelReader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);

                var listDataId = new List<long>();
                var listT1 = new List<string>();
                var listT2 = new List<string>();
                var listT3 = new List<string>();
                var listT4 = new List<string>();
                var listT5 = new List<string>();
                var listT6 = new List<string>();
                var listT7 = new List<string>();
                var listT8 = new List<string>();
                var listT9 = new List<string>();
                var listT10 = new List<string>();
                var listT11 = new List<string>();
                var listResumen = new List<string>();

                bool isFirstRow = true;
                while (excelReader.Read())
                {
                    if (isFirstRow)
                    {
                        isFirstRow = false;
                        var val0 = excelReader.GetValue(0)?.ToString();
                        // Si la primera fila es encabezado (no numérico), nos la saltamos
                        if (string.IsNullOrWhiteSpace(val0) || !long.TryParse(val0, out _))
                            continue;
                    }

                    var val = excelReader.GetValue(0);
                    if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        if (!long.TryParse(val.ToString(), out long dataId))
                            continue;

                        int fieldCount = excelReader.FieldCount;
                        string texto1 = fieldCount > 1 && excelReader.GetValue(1) != null ? excelReader.GetValue(1).ToString()!.ToUpper() : "";
                        string texto2 = "INDIVIDUAL";
                        string texto3 = fieldCount > 3 && excelReader.GetValue(3) != null ? excelReader.GetValue(3).ToString()!.ToUpper() : "";
                        string texto4 = fieldCount > 2 && excelReader.GetValue(2) != null ? excelReader.GetValue(2).ToString()!.ToUpper() : "";
                        string texto5 = "-0-";
                        string texto6 = "-0-";
                        string texto7 = "-0-";
                        string texto8 = "-0-";
                        string texto9 = "-0-";
                        string texto10 = "-0-";
                        string texto11 = "-0-";

                        string resumen = $"{dataId}^{texto1}^{texto2}^{texto3}^{texto4}^{texto5}^{texto6}^{texto7}^{texto8}^{texto9}^{texto10}^{texto11}".ToUpper();

                        listDataId.Add(dataId);
                        listT1.Add(texto1);
                        listT2.Add(texto2);
                        listT3.Add(texto3);
                        listT4.Add(texto4);
                        listT5.Add(texto5);
                        listT6.Add(texto6);
                        listT7.Add(texto7);
                        listT8.Add(texto8);
                        listT9.Add(texto9);
                        listT10.Add(texto10);
                        listT11.Add(texto11);
                        listResumen.Add(resumen);
                    }
                }

                if (listDataId.Count == 0)
                    return (false, "No se cargo la lista porque el archivo no contiene registros validos.");

                using var tx = conn.BeginTransaction();
                try
                {
                    await ReemplazarRegistrosListaCautelaAsync(conn, tx, tipoListaCautelaId, usuarioId, listDataId, listT1, listT2, listT3, listT4, listT5, listT6, listT7, listT8, listT9, listT10, listT11, listResumen);
                    await RegistrarAuditoriaCargaListaAsync(conn, tx, tipoListaCautelaId, usuarioId, archivo.FileName, System.IO.Path.GetExtension(archivo.FileName), registrosAnteriores, listDataId.Count);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }

                return (true, ConstruirMensajeCargaExitosa(registrosAnteriores, listDataId.Count));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al intentar procesar la lista de cautela Engel.");
                return (false, "Error al intentar cargar la lista de cautela Engel.");
            }
        }

        public async Task<string> ObtenerDescripcionListaAsync(int tipoListaCautelaId)
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT LISTA_CAUTELA_DESCRICPION FROM DNP_IHSS.TIPO_LISTAS_CAUTELA WHERE TIPO_LISTA_CAUTELA_ID = :id";
            cmd.Parameters.Add(new OracleParameter("id", tipoListaCautelaId));
            var res = await cmd.ExecuteScalarAsync();
            return res?.ToString() ?? "";
        }

        public async Task<(bool Success, string Mensaje)> ProcesarArchivoExcelPepsAsync(Microsoft.AspNetCore.Http.IFormFile archivo, int tipoListaCautelaId, long usuarioId)
        {
            try
            {
                await using var conn = _db.CreateConnection();
                await conn.OpenAsync();
                var registrosAnteriores = await ContarRegistrosListaCautelaAsync(conn, tipoListaCautelaId);

                // 1. Procesar Excel y preparar registros antes de reemplazar la lista actual.
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using var stream = archivo.OpenReadStream();
                using var excelReader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);

                var listDataId = new List<long>();
                var listT1 = new List<string>();
                var listT2 = new List<string>();
                var listT3 = new List<string>();
                var listT4 = new List<string>();
                var listT5 = new List<string>();
                var listT6 = new List<string>();
                var listT7 = new List<string>();
                var listT8 = new List<string>();
                var listT9 = new List<string>();
                var listT10 = new List<string>();
                var listT11 = new List<string>();
                var listResumen = new List<string>();

                bool isFirstRow = true;
                while (excelReader.Read())
                {
                    if (isFirstRow)
                    {
                        isFirstRow = false;
                        var val0 = excelReader.GetValue(0)?.ToString();
                        // Si la primera fila es encabezado (no numérico), la saltamos
                        if (string.IsNullOrWhiteSpace(val0) || !long.TryParse(val0, out _))
                            continue;
                    }

                    var val = excelReader.GetValue(0);
                    if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        if (!long.TryParse(val.ToString(), out long dataId))
                            continue;

                        int fieldCount = excelReader.FieldCount;
                        
                        string texto1 = fieldCount > 2 && excelReader.GetValue(2) != null ? excelReader.GetValue(2).ToString()!.ToUpper() : "";
                        string texto2 = "INDIVIDUAL";
                        string texto3 = "HONDURAS";
                        string texto4 = fieldCount > 6 && excelReader.GetValue(6) != null ? excelReader.GetValue(6).ToString()!.ToUpper() : "";
                        string texto5 = fieldCount > 3 && excelReader.GetValue(3) != null ? excelReader.GetValue(3).ToString()!.ToUpper() : "";
                        string texto6 = "-0-";
                        string texto7 = "-0-";
                        
                        string depto = fieldCount > 5 && excelReader.GetValue(5) != null ? excelReader.GetValue(5)?.ToString() ?? "" : "";
                        string muni = fieldCount > 4 && excelReader.GetValue(4) != null ? excelReader.GetValue(4)?.ToString() ?? "" : "";
                        string texto8 = $"DEPARTAMENTO: {depto}, MUNICIPIO: {muni}".ToUpper();
                        
                        string texto9 = fieldCount > 1 && excelReader.GetValue(1) != null ? excelReader.GetValue(1).ToString()!.ToUpper() : "";
                        
                        string partido = fieldCount > 7 && excelReader.GetValue(7) != null ? excelReader.GetValue(7)?.ToString() ?? "" : "";
                        string texto10 = $"PARTIDO: {partido}";
                        
                        string texto11 = "-0-";

                        // Validar que no sea un registro en blanco. Exigimos que al menos el Nombre (texto1) 
                        // o la Identidad (texto9) contengan datos reales.
                        if ((string.IsNullOrWhiteSpace(texto1) || texto1.Replace("\u00A0", "").Trim() == "") && 
                            (string.IsNullOrWhiteSpace(texto9) || texto9.Replace("\u00A0", "").Trim() == ""))
                        {
                            continue;
                        }

                        string resumen = $"{dataId}^{texto1}^{texto2}^{texto3}^{texto4}^{texto5}^{texto6}^{texto7}^{texto8}^{texto9}^{texto10}^{texto11}".ToUpper();

                        listDataId.Add(dataId);
                        listT1.Add(texto1);
                        listT2.Add(texto2);
                        listT3.Add(texto3);
                        listT4.Add(texto4);
                        listT5.Add(texto5);
                        listT6.Add(texto6);
                        listT7.Add(texto7);
                        listT8.Add(texto8);
                        listT9.Add(texto9);
                        listT10.Add(texto10);
                        listT11.Add(texto11);
                        listResumen.Add(resumen);
                    }
                }

                if (listDataId.Count == 0)
                    return (false, "No se cargo la lista porque el archivo no contiene registros validos.");

                using var tx = conn.BeginTransaction();
                try
                {
                    await ReemplazarRegistrosListaCautelaAsync(conn, tx, tipoListaCautelaId, usuarioId, listDataId, listT1, listT2, listT3, listT4, listT5, listT6, listT7, listT8, listT9, listT10, listT11, listResumen);
                    await RegistrarAuditoriaCargaListaAsync(conn, tx, tipoListaCautelaId, usuarioId, archivo.FileName, System.IO.Path.GetExtension(archivo.FileName), registrosAnteriores, listDataId.Count);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }

                return (true, ConstruirMensajeCargaExitosa(registrosAnteriores, listDataId.Count));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al intentar procesar la lista de cautela PEPS.");
                return (false, "Error al intentar cargar la lista de cautela PEPS.");
            }
        }
    }
}
