using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Identidad.Contracts;
using RL.API.Features.Identidad.Domain;
using RL.API.Infrastructure;

namespace RL.API.Features.Identidad.Persistence;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly OracleDbContext _db;
    public UsuarioRepository(OracleDbContext db) => _db = db;

    public async Task<Usuario?> ObtenerPorEmailAsync(string email)
    {
        Usuario? u = null;
        await using (var conn = _db.CreateConnection())
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT U.*, R.ROL_NOMBRE, R.ROL_DESCRIPCION, D.DOM_NOMBRE AS USR_DOMINIO
                FROM RL_USUARIOS U
                INNER JOIN RL_ROLES R ON U.USR_ROL_ID = R.ROL_ID
                LEFT JOIN RL_DOMINIO D ON U.USR_DOM_ID = D.DOM_ID
                WHERE UPPER(U.USR_EMAIL) = UPPER(:p_email) AND U.USR_ACTIVO = 1";
            cmd.Parameters.Add(new OracleParameter("p_email", email));

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                u = MapUsuario(reader);
            }
        }

        if (u != null)
        {
            u.ModulosIds = await ObtenerModulosIdsPorUsuarioAsync(u.UsrId);
        }
        return u;
    }

    public async Task<Usuario?> ObtenerPorLoginAsync(string identifier)
    {
        Usuario? u = null;
        await using (var conn = _db.CreateConnection())
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT U.*, R.ROL_NOMBRE, R.ROL_DESCRIPCION, D.DOM_NOMBRE AS USR_DOMINIO
                FROM RL_USUARIOS U
                INNER JOIN RL_ROLES R ON U.USR_ROL_ID = R.ROL_ID
                LEFT JOIN RL_DOMINIO D ON U.USR_DOM_ID = D.DOM_ID
                WHERE (UPPER(U.USR_EMAIL) = UPPER(:p_id) OR UPPER(U.USUARIO_DOMINIO) = UPPER(:p_id))
                  AND U.USR_ACTIVO = 1";
            cmd.Parameters.Add(new OracleParameter("p_id", identifier));

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                u = MapUsuario(reader);
            }
        }

        if (u != null)
        {
            u.ModulosIds = await ObtenerModulosIdsPorUsuarioAsync(u.UsrId);
        }
        return u;
    }

    public async Task<Usuario?> ObtenerPorIdAsync(long id)
    {
        Usuario? u = null;
        await using (var conn = _db.CreateConnection())
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT U.*, R.ROL_NOMBRE, R.ROL_DESCRIPCION, D.DOM_NOMBRE AS USR_DOMINIO
                FROM RL_USUARIOS U
                INNER JOIN RL_ROLES R ON U.USR_ROL_ID = R.ROL_ID
                LEFT JOIN RL_DOMINIO D ON U.USR_DOM_ID = D.DOM_ID
                WHERE U.USR_ID = :p_id";
            cmd.Parameters.Add(new OracleParameter("p_id", id));

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                u = MapUsuario(reader);
            }
        }

        if (u != null)
        {
            u.ModulosIds = await ObtenerModulosIdsPorUsuarioAsync(u.UsrId);
        }
        return u;
    }

    public async Task<long> CrearAsync(CrearUsuarioDto dto, string hash, string salt)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO RL_USUARIOS (
                USR_ID, USR_NOMBRE, USR_APELLIDO, USR_EMAIL,
                USR_PASSWORD_HASH, USR_PASSWORD_SALT, USR_ROL_ID, USR_EMPLEADO_ID,
                USR_DNI, ES_USUARIO_DOMINIO, USUARIO_DOMINIO, USR_DOM_ID,
                USR_DEBE_CAMBIAR_PASS, USR_FECHA_CLAVE_TEMP
            ) VALUES (
                SEQ_RL_USUARIOS.NEXTVAL, :nombre, :apellido, :email,
                :hash, :salt, :rol_id, :emp_id,
                :u_dni, :u_es_dom, :u_dom_user, :u_dom_id,
                :debe_cambiar, :fecha_temp
            )
            RETURNING USR_ID INTO :new_id";

        cmd.Parameters.Add(new OracleParameter("nombre",   dto.Nombre));
        cmd.Parameters.Add(new OracleParameter("apellido", dto.Apellido));
        cmd.Parameters.Add(new OracleParameter("email",    dto.Email));
        cmd.Parameters.Add(new OracleParameter("hash",     hash));
        cmd.Parameters.Add(new OracleParameter("salt",     salt));
        cmd.Parameters.Add(new OracleParameter("rol_id",   dto.RolId));
        cmd.Parameters.Add(new OracleParameter("emp_id",   (object?)dto.EmpleadoId ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("u_dni",    (object?)dto.Dni ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("u_es_dom", dto.EsUsuarioDominio));
        cmd.Parameters.Add(new OracleParameter("u_dom_user", (object?)dto.UsuarioDominio ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("u_dom_id", (object?)dto.DominioId ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("debe_cambiar", dto.EsUsuarioDominio == 0 ? 1 : 0));
        cmd.Parameters.Add(new OracleParameter("fecha_temp", dto.EsUsuarioDominio == 0 ? (object)DateTime.Now : DBNull.Value));

        var pNewId = new OracleParameter("new_id", OracleDbType.Int64)
            { Direction = System.Data.ParameterDirection.Output };
        cmd.Parameters.Add(pNewId);

        await cmd.ExecuteNonQueryAsync();
        var rawVal = pNewId.Value;
        long newId = rawVal is Oracle.ManagedDataAccess.Types.OracleDecimal od
            ? (long)od.Value
            : Convert.ToInt64(rawVal.ToString());

        if (dto.ModulosIds != null && dto.ModulosIds.Count > 0)
        {
            foreach (var modId in dto.ModulosIds)
            {
                await using var mCmd = conn.CreateCommand();
                mCmd.CommandText = "INSERT INTO RL_USUARIO_MODULOS (USM_USR_ID, USM_MOD_ID) VALUES (:usr_id, :mod_id)";
                mCmd.Parameters.Add(new OracleParameter("usr_id", newId));
                mCmd.Parameters.Add(new OracleParameter("mod_id", modId));
                await mCmd.ExecuteNonQueryAsync();
            }
        }

        return newId;
    }

    public async Task<bool> ActualizarAsync(long id, ActualizarUsuarioDto dto, string? hash, string? salt)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;

        string sql = @"
            UPDATE RL_USUARIOS SET 
                USR_NOMBRE = :nombre, 
                USR_APELLIDO = :apellido, 
                USR_EMAIL = :email,
                USR_ROL_ID = :rol_id, 
                USR_DNI = :u_dni, 
                ES_USUARIO_DOMINIO = :u_es_dom, 
                USUARIO_DOMINIO = :u_dom_user, 
                USR_DOM_ID = :u_dom_id, 
                USR_FECHA_MODIFICACION = SYSDATE";

        if (!string.IsNullOrEmpty(hash))
        {
            sql += @", 
                USR_PASSWORD_HASH = :hash, 
                USR_PASSWORD_SALT = :salt";
            cmd.Parameters.Add(new OracleParameter("hash", hash));
            cmd.Parameters.Add(new OracleParameter("salt", (object?)salt ?? DBNull.Value));
        }

        sql += " WHERE USR_ID = :id";
        cmd.CommandText = sql;

        cmd.Parameters.Add(new OracleParameter("nombre",   dto.Nombre));
        cmd.Parameters.Add(new OracleParameter("apellido", dto.Apellido));
        cmd.Parameters.Add(new OracleParameter("email",    dto.Email));
        cmd.Parameters.Add(new OracleParameter("rol_id",   dto.RolId));
        cmd.Parameters.Add(new OracleParameter("u_dni",    (object?)dto.Dni ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("u_es_dom", dto.EsUsuarioDominio));
        cmd.Parameters.Add(new OracleParameter("u_dom_user", (object?)dto.UsuarioDominio ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("u_dom_id", (object?)dto.DominioId ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("id",       id));

        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows > 0)
        {
            // Clear existing modules
            await using var delCmd = conn.CreateCommand();
            delCmd.CommandText = "DELETE FROM RL_USUARIO_MODULOS WHERE USM_USR_ID = :usr_id";
            delCmd.Parameters.Add(new OracleParameter("usr_id", id));
            await delCmd.ExecuteNonQueryAsync();

            // Insert new modules
            if (dto.ModulosIds != null && dto.ModulosIds.Count > 0)
            {
                foreach (var modId in dto.ModulosIds)
                {
                    await using var insCmd = conn.CreateCommand();
                    insCmd.CommandText = "INSERT INTO RL_USUARIO_MODULOS (USM_USR_ID, USM_MOD_ID) VALUES (:usr_id, :mod_id)";
                    insCmd.Parameters.Add(new OracleParameter("usr_id", id));
                    insCmd.Parameters.Add(new OracleParameter("mod_id", modId));
                    await insCmd.ExecuteNonQueryAsync();
                }
            }
        }
        return rows > 0;
    }

    public async Task<bool> ActualizarPasswordAsync(long usrId, string hash, string salt)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE RL_USUARIOS
            SET USR_PASSWORD_HASH = :hash, USR_PASSWORD_SALT = :salt,
                USR_DEBE_CAMBIAR_PASS = 0,
                USR_FECHA_CLAVE_TEMP = NULL,
                USR_FECHA_MODIFICACION = SYSDATE
            WHERE USR_ID = :id";

        cmd.Parameters.Add(new OracleParameter("hash", hash));
        cmd.Parameters.Add(new OracleParameter("salt", salt));
        cmd.Parameters.Add(new OracleParameter("id",   usrId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> ForzarCambioPasswordAsync(long usrId, string hash, string salt)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE RL_USUARIOS
            SET USR_PASSWORD_HASH = :hash, USR_PASSWORD_SALT = :salt,
                USR_DEBE_CAMBIAR_PASS = 1,
                USR_FECHA_CLAVE_TEMP = :fecha_temp,
                USR_FECHA_MODIFICACION = SYSDATE
            WHERE USR_ID = :id";

        cmd.Parameters.Add(new OracleParameter("hash", hash));
        cmd.Parameters.Add(new OracleParameter("salt", salt));
        cmd.Parameters.Add(new OracleParameter("fecha_temp", DateTime.Now));
        cmd.Parameters.Add(new OracleParameter("id",   usrId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> ActualizarEstadoAsync(long id, bool activo)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE RL_USUARIOS SET USR_ACTIVO = :activo, USR_FECHA_MODIFICACION = SYSDATE WHERE USR_ID = :id";
        cmd.Parameters.Add(new OracleParameter("activo", activo ? 1 : 0));
        cmd.Parameters.Add(new OracleParameter("id", id));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<string?> ObtenerRefreshTokenAsync(long usrId, string token)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT RFT_TOKEN FROM RL_REFRESH_TOKENS
            WHERE RFT_USR_ID = :usr_id AND RFT_TOKEN = :token
              AND RFT_REVOCADO = 0 AND RFT_EXPIRA > SYSDATE";

        cmd.Parameters.Add(new OracleParameter("usr_id", usrId));
        cmd.Parameters.Add(new OracleParameter("token",  token));

        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }

    public async Task GuardarRefreshTokenAsync(long usrId, string token, DateTime expira, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO RL_REFRESH_TOKENS
                (RFT_ID, RFT_USR_ID, RFT_TOKEN, RFT_EXPIRA, RFT_IP_ORIGEN)
            VALUES
                (SEQ_RL_REFRESH_TOKENS.NEXTVAL, :usr_id, :token, :expira, :ip)";

        cmd.Parameters.Add(new OracleParameter("usr_id", usrId));
        cmd.Parameters.Add(new OracleParameter("token",  token));
        cmd.Parameters.Add(new OracleParameter("expira", expira));
        cmd.Parameters.Add(new OracleParameter("ip",     (object?)ip ?? DBNull.Value));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RevocarRefreshTokenAsync(string token)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE RL_REFRESH_TOKENS SET RFT_REVOCADO = 1 WHERE RFT_TOKEN = :token";
        cmd.Parameters.Add(new OracleParameter("token", token));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RevocarTodosTokensAsync(long usrId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE RL_REFRESH_TOKENS SET RFT_REVOCADO = 1 WHERE RFT_USR_ID = :usr_id";
        cmd.Parameters.Add(new OracleParameter("usr_id", usrId));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<UsuarioInfoDto>> ListarAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT U.USR_ID, U.USR_NOMBRE, U.USR_APELLIDO, U.USR_EMAIL,
                   R.ROL_NOMBRE, R.ROL_ID, U.ES_USUARIO_DOMINIO, U.USUARIO_DOMINIO,
                   U.USR_DOM_ID, D.DOM_NOMBRE, U.USR_DNI
            FROM RL_USUARIOS U
            INNER JOIN RL_ROLES R ON U.USR_ROL_ID = R.ROL_ID
            LEFT JOIN RL_DOMINIO D ON U.USR_DOM_ID = D.DOM_ID
            ORDER BY U.USR_NOMBRE";

        var list = new List<UsuarioInfoDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new UsuarioInfoDto
            {
                Id       = Convert.ToInt64(reader["USR_ID"]),
                Nombre   = reader["USR_NOMBRE"].ToString()!,
                Apellido = reader["USR_APELLIDO"].ToString()!,
                Email    = reader["USR_EMAIL"].ToString()!,
                Rol      = (reader["ROL_NOMBRE"]?.ToString() ?? string.Empty).Trim().ToUpper(),
                RolId    = Convert.ToInt32(reader["ROL_ID"]),
                EsUsuarioDominio = Convert.ToInt32(reader["ES_USUARIO_DOMINIO"]),
                UsuarioDominio  = reader["USUARIO_DOMINIO"]?.ToString(),
                DominioId = reader["USR_DOM_ID"] == DBNull.Value ? null : Convert.ToInt32(reader["USR_DOM_ID"]),
                Dominio  = reader["DOM_NOMBRE"]?.ToString(),
                Dni      = reader["USR_DNI"]?.ToString()
            });
        }

        // Query modules mapping
        await using var mCmd = conn.CreateCommand();
        mCmd.CommandText = "SELECT USM_USR_ID, USM_MOD_ID FROM RL_USUARIO_MODULOS";
        var modMap = new Dictionary<long, List<int>>();
        await using var mReader = await mCmd.ExecuteReaderAsync();
        while (await mReader.ReadAsync())
        {
            long uId = Convert.ToInt64(mReader["USM_USR_ID"]);
            int mId = Convert.ToInt32(mReader["USM_MOD_ID"]);
            if (!modMap.ContainsKey(uId))
                modMap[uId] = new List<int>();
            modMap[uId].Add(mId);
        }

        foreach (var u in list)
        {
            u.ModulosIds = modMap.TryGetValue(u.Id, out var ids) ? ids : new List<int>();
        }

        return list;
    }

    private static Usuario MapUsuario(System.Data.Common.DbDataReader r) => new()
    {
        UsrId           = Convert.ToInt64(r["USR_ID"]),
        UsrNombre       = r["USR_NOMBRE"].ToString()!,
        UsrApellido     = r["USR_APELLIDO"].ToString()!,
        UsrEmail        = r["USR_EMAIL"].ToString()!,
        UsrPasswordHash = r["USR_PASSWORD_HASH"].ToString()!,
        UsrPasswordSalt = r["USR_PASSWORD_SALT"].ToString()!,
        UsrRolId        = Convert.ToInt32(r["USR_ROL_ID"]),
        UsrEmpleadoId   = r["USR_EMPLEADO_ID"] as string,
        UsrDni          = r["USR_DNI"] as string,
        UsrActivo       = Convert.ToInt32(r["USR_ACTIVO"]) == 1,
        EsUsuarioDominio = Convert.ToInt32(r["ES_USUARIO_DOMINIO"]),
        UsuarioDominio  = r["USUARIO_DOMINIO"] as string,
        UsrDomId        = r["USR_DOM_ID"] == DBNull.Value ? null : Convert.ToInt32(r["USR_DOM_ID"]),
        UsrDominio      = r["USR_DOMINIO"] == DBNull.Value ? null : r["USR_DOMINIO"].ToString(),
        UsrFechaCreacion = Convert.ToDateTime(r["USR_FECHA_CREACION"]),
        UsrIntentosFallidos = r["USR_INTENTOS_FALLIDOS"] == DBNull.Value ? 0 : Convert.ToInt32(r["USR_INTENTOS_FALLIDOS"]),
        UsrFechaBloqueo = r["USR_FECHA_BLOQUEO"] == DBNull.Value ? null : Convert.ToDateTime(r["USR_FECHA_BLOQUEO"]),
        UsrDebeCambiarPass = r["USR_DEBE_CAMBIAR_PASS"] == DBNull.Value ? 0 : Convert.ToInt32(r["USR_DEBE_CAMBIAR_PASS"]),
        UsrFechaClaveTemp = r["USR_FECHA_CLAVE_TEMP"] == DBNull.Value ? null : Convert.ToDateTime(r["USR_FECHA_CLAVE_TEMP"]),
        Rol = new Rol
        {
            RolId          = Convert.ToInt32(r["USR_ROL_ID"]),
            RolNombre      = (r["ROL_NOMBRE"]?.ToString() ?? string.Empty).Trim().ToUpper(),
            RolDescripcion = r["ROL_DESCRIPCION"] as string
        }
    };

    public async Task<List<int>> ObtenerModulosIdsPorUsuarioAsync(long usrId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT USM_MOD_ID FROM RL_USUARIO_MODULOS WHERE USM_USR_ID = :usr_id";
        cmd.Parameters.Add(new OracleParameter("usr_id", usrId));

        var list = new List<int>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(Convert.ToInt32(reader["USM_MOD_ID"]));
        }
        return list;
    }

    public async Task RegistrarIntentoFallidoAsync(long usrId, int nuevosIntentos, DateTime? fechaBloqueo)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE RL_USUARIOS 
            SET USR_INTENTOS_FALLIDOS = :intentos,
                USR_FECHA_BLOQUEO = :fecha_bloqueo,
                USR_FECHA_MODIFICACION = SYSDATE 
            WHERE USR_ID = :id";
        cmd.Parameters.Add(new OracleParameter("intentos", nuevosIntentos));
        cmd.Parameters.Add(new OracleParameter("fecha_bloqueo", (object?)fechaBloqueo ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("id", usrId));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RestablecerIntentosAsync(long usrId)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE RL_USUARIOS 
            SET USR_INTENTOS_FALLIDOS = 0,
                USR_FECHA_BLOQUEO = NULL,
                USR_FECHA_MODIFICACION = SYSDATE 
            WHERE USR_ID = :id";
        cmd.Parameters.Add(new OracleParameter("id", usrId));

        await cmd.ExecuteNonQueryAsync();
    }
}
