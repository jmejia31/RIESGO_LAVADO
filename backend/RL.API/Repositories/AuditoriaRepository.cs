using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using RL.API.DTOs;
using RL.API.Infrastructure;

namespace RL.API.Repositories;

public interface IAuditoriaRepository
{
    Task RegistrarAsync(string tabla, string registroId, string accion, string? datosAnt, string? datosNvo, long? usrId, string? email, string? ip, string? modulo);
    Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(int pagina, int limite, string? buscar, string? accion, string? modulo, DateTime? fechaInicio, DateTime? fechaFin);
}

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly OracleDbContext _db;

    public AuditoriaRepository(OracleDbContext db)
    {
        _db = db;
    }

    public async Task RegistrarAsync(string tabla, string registroId, string accion, string? datosAnt, string? datosNvo, long? usrId, string? email, string? ip, string? modulo)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        if (string.IsNullOrEmpty(email) && usrId.HasValue)
        {
            try
            {
                await using var emailCmd = conn.CreateCommand();
                emailCmd.CommandText = "SELECT USR_EMAIL FROM RL_USUARIOS WHERE USR_ID = :usrId";
                emailCmd.Parameters.Add(new OracleParameter("usrId", usrId.Value));
                var res = await emailCmd.ExecuteScalarAsync();
                if (res != null && res != DBNull.Value)
                {
                    email = res.ToString();
                }
            }
            catch
            {
                // Silently ignore to ensure audit log doesn't block main business logic execution
            }
        }

        await using var cmd = conn.CreateCommand();
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

        cmd.Parameters.Add(new OracleParameter("tabla", tabla));
        cmd.Parameters.Add(new OracleParameter("regId", registroId));
        cmd.Parameters.Add(new OracleParameter("accion", accion));
        cmd.Parameters.Add(new OracleParameter("datosAnt", (object?)datosAnt ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("datosNvo", (object?)datosNvo ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("usrId", (object?)usrId ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("email", (object?)email ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("ip", (object?)ip ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("modulo", (object?)modulo ?? DBNull.Value));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(
        int pagina, int limite, string? buscar, string? accion, string? modulo, DateTime? fechaInicio, DateTime? fechaFin)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        var whereClauses = new List<string>();
        var parameters = new List<OracleParameter>();

        if (!string.IsNullOrWhiteSpace(accion))
        {
            whereClauses.Add("AUD_ACCION = :accionFilter");
            parameters.Add(new OracleParameter("accionFilter", accion));
        }

        if (!string.IsNullOrWhiteSpace(modulo))
        {
            whereClauses.Add("AUD_MODULO = :moduloFilter");
            parameters.Add(new OracleParameter("moduloFilter", modulo));
        }

        if (fechaInicio.HasValue)
        {
            whereClauses.Add("AUD_FECHA >= :fechaInicioFilter");
            parameters.Add(new OracleParameter("fechaInicioFilter", fechaInicio.Value.Date));
        }

        if (fechaFin.HasValue)
        {
            whereClauses.Add("AUD_FECHA <= :fechaFinFilter");
            parameters.Add(new OracleParameter("fechaFinFilter", fechaFin.Value.Date.AddDays(1).AddSeconds(-1)));
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            whereClauses.Add("(LOWER(AUD_USR_EMAIL) LIKE :buscarFilter OR LOWER(NVL(AUD_USR_EMAIL, (SELECT USR_EMAIL FROM RL_USUARIOS WHERE USR_ID = AUD_USR_ID))) LIKE :buscarFilter OR LOWER(AUD_TABLA) LIKE :buscarFilter OR LOWER(AUD_IP) LIKE :buscarFilter OR LOWER(AUD_REGISTRO_ID) LIKE :buscarFilter)");
            parameters.Add(new OracleParameter("buscarFilter", $"%{buscar.Trim().ToLower()}%"));
        }

        string whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        // 1. Obtener total
        int total = 0;
        await using (var countCmd = conn.CreateCommand())
        {
            countCmd.BindByName = true;
            countCmd.CommandText = $"SELECT COUNT(*) FROM RL_AUDITORIA {whereSql}";
            foreach (var p in parameters)
            {
                countCmd.Parameters.Add(new OracleParameter(p.ParameterName, p.Value));
            }
            total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        }

        // 2. Obtener datos paginados
        var list = new List<AuditoriaDto>();
        await using (var queryCmd = conn.CreateCommand())
        {
            queryCmd.BindByName = true;
            int maxRow = pagina * limite;
            int minRow = (pagina - 1) * limite;

            queryCmd.CommandText = $@"
                SELECT * FROM (
                    SELECT a.*, ROWNUM rnum FROM (
                        SELECT AUD_ID, AUD_TABLA, AUD_REGISTRO_ID, AUD_ACCION, 
                               AUD_DATOS_ANT, AUD_DATOS_NVO, AUD_USR_ID, 
                               NVL((SELECT USR_NOMBRE || ' ' || USR_APELLIDO FROM RL_USUARIOS WHERE USR_ID = AUD_USR_ID), AUD_USR_EMAIL) AS AUD_USR_EMAIL, 
                               AUD_IP, AUD_FECHA, AUD_MODULO
                        FROM RL_AUDITORIA
                        {whereSql}
                        ORDER BY AUD_FECHA DESC, AUD_ID DESC
                    ) a WHERE ROWNUM <= :maxRow
                ) WHERE rnum > :minRow";

            foreach (var p in parameters)
            {
                queryCmd.Parameters.Add(new OracleParameter(p.ParameterName, p.Value));
            }
            queryCmd.Parameters.Add(new OracleParameter("maxRow", maxRow));
            queryCmd.Parameters.Add(new OracleParameter("minRow", minRow));

            await using var reader = await queryCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new AuditoriaDto
                {
                    AudId = Convert.ToInt64(reader["AUD_ID"]),
                    Tabla = reader["AUD_TABLA"]?.ToString() ?? string.Empty,
                    RegistroId = reader["AUD_REGISTRO_ID"]?.ToString() ?? string.Empty,
                    Accion = reader["AUD_ACCION"]?.ToString() ?? string.Empty,
                    DatosAnt = reader["AUD_DATOS_ANT"] == DBNull.Value ? null : reader["AUD_DATOS_ANT"]?.ToString(),
                    DatosNvo = reader["AUD_DATOS_NVO"] == DBNull.Value ? null : reader["AUD_DATOS_NVO"]?.ToString(),
                    UsrId = reader["AUD_USR_ID"] == DBNull.Value ? null : Convert.ToInt64(reader["AUD_USR_ID"]),
                    UsrEmail = reader["AUD_USR_EMAIL"] == DBNull.Value ? null : reader["AUD_USR_EMAIL"]?.ToString(),
                    Ip = reader["AUD_IP"] == DBNull.Value ? null : reader["AUD_IP"]?.ToString(),
                    Fecha = Convert.ToDateTime(reader["AUD_FECHA"]),
                    Modulo = reader["AUD_MODULO"] == DBNull.Value ? null : reader["AUD_MODULO"]?.ToString()
                });
            }
        }

        return (list, total);
    }
}

