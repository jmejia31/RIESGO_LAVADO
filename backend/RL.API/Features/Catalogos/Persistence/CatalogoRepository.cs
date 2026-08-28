using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Catalogos.Contracts;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.Catalogos.Persistence;

public class CatalogoRepository : ICatalogoRepository
{
    private readonly OracleDbContext _db;

    public CatalogoRepository(OracleDbContext db)
    {
        _db = db;
    }

    public async Task<List<KeyValuePair<int, string>>> ObtenerRolesAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT ROL_ID, ROL_NOMBRE FROM RL_ROLES WHERE ROL_ACTIVO = 1 ORDER BY ROL_NOMBRE";

        var list = new List<KeyValuePair<int, string>>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new KeyValuePair<int, string>(
                Convert.ToInt32(reader["ROL_ID"]),
                reader["ROL_NOMBRE"].ToString()!
            ));
        }
        return list;
    }

    public async Task<List<KeyValuePair<int, string>>> ObtenerDominiosAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DOM_ID, DOM_NOMBRE FROM RL_DOMINIO WHERE DOM_ACTIVO = 1 ORDER BY DOM_NOMBRE";

        var list = new List<KeyValuePair<int, string>>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new KeyValuePair<int, string>(
                Convert.ToInt32(reader["DOM_ID"]),
                reader["DOM_NOMBRE"].ToString()!
            ));
        }
        return list;
    }

    public async Task<List<Modulo>> ObtenerModulosAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MOD_ID, MOD_NOMBRE, MOD_DESCRIPCION, MOD_RUTA, MOD_ICONO, MOD_SECCION, MOD_ACTIVO FROM RL_MODULOS WHERE MOD_ACTIVO = 1 ORDER BY MOD_SECCION, MOD_NOMBRE";

        var list = new List<Modulo>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Modulo
            {
                ModId = Convert.ToInt32(reader["MOD_ID"]),
                ModNombre = reader["MOD_NOMBRE"].ToString()!,
                ModDescripcion = reader["MOD_DESCRIPCION"] == DBNull.Value ? null : reader["MOD_DESCRIPCION"].ToString(),
                ModRuta = reader["MOD_RUTA"].ToString()!,
                ModIcono = reader["MOD_ICONO"].ToString()!,
                ModSeccion = reader["MOD_SECCION"].ToString()!,
                ModActivo = Convert.ToInt32(reader["MOD_ACTIVO"])
            });
        }
        return list;
    }

    public async Task<List<CatalogoMatricesDto>> ListarMatricesAsync(bool incluirInactivos)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        const string sql = @"SELECT CAT_ID, CAT_CODIGO, CAT_NOMBRE, CAT_ACTIVO FROM RL_MR_CATALOGOS
                             WHERE (:incluir = 1 OR CAT_ACTIVO = 1) ORDER BY CAT_CODIGO";
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new OracleParameter("incluir", incluirInactivos ? 1 : 0));
        var encabezados = new List<(long Id, string Codigo, string Nombre, bool Activo)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            long id = Convert.ToInt64(reader[0]);
            encabezados.Add((id, reader[1].ToString()!, reader[2].ToString()!, Convert.ToInt32(reader[3]) == 1));
        }
        var result = new List<CatalogoMatricesDto>(encabezados.Count);
        foreach (var encabezado in encabezados)
            result.Add(new CatalogoMatricesDto(encabezado.Id, encabezado.Codigo, encabezado.Nombre, encabezado.Activo, await ElementosAsync(conn, encabezado.Id, incluirInactivos)));
        return result;
    }

    private static async Task<IReadOnlyList<ElementoCatalogoMatricesDto>> ElementosAsync(OracleConnection conn, long catalogoId, bool incluirInactivos)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT ELE_ID, ELE_CODIGO, ELE_VALOR, ELE_ORDEN, ELE_ACTIVO FROM RL_MR_ELEMENTOS_CATALOGO
                            WHERE ELE_CATALOGO_ID = :id AND (:incluir = 1 OR ELE_ACTIVO = 1) ORDER BY ELE_ORDEN, ELE_CODIGO";
        cmd.Parameters.Add(new OracleParameter("id", catalogoId));
        cmd.Parameters.Add(new OracleParameter("incluir", incluirInactivos ? 1 : 0));
        var result = new List<ElementoCatalogoMatricesDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(new ElementoCatalogoMatricesDto(Convert.ToInt64(reader[0]), reader[1].ToString()!, reader[2].ToString()!, Convert.ToInt32(reader[3]), Convert.ToInt32(reader[4]) == 1));
        return result;
    }

    public async Task<long> CrearCatalogoMatricesAsync(string codigo, string nombre)
    {
        await using var conn = _db.CreateConnection(); await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO RL_MR_CATALOGOS (CAT_ID, CAT_CODIGO, CAT_NOMBRE, CAT_ACTIVO) VALUES (SEQ_RL_MR_CATALOGOS.NEXTVAL, :codigo, :nombre, 1) RETURNING CAT_ID INTO :id";
        cmd.Parameters.Add(new OracleParameter("codigo", codigo.Trim().ToUpperInvariant())); cmd.Parameters.Add(new OracleParameter("nombre", nombre.Trim()));
        var id = new OracleParameter("id", OracleDbType.Int64) { Direction = System.Data.ParameterDirection.Output }; cmd.Parameters.Add(id); await cmd.ExecuteNonQueryAsync(); return Convert.ToInt64(id.Value.ToString());
    }

    public async Task<bool> ActualizarCatalogoMatricesAsync(long id, string nombre, bool activo)
    {
        await using var conn = _db.CreateConnection(); await conn.OpenAsync(); await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE RL_MR_CATALOGOS SET CAT_NOMBRE = :nombre, CAT_ACTIVO = :activo WHERE CAT_ID = :id";
        cmd.Parameters.Add(new OracleParameter("nombre", nombre.Trim())); cmd.Parameters.Add(new OracleParameter("activo", activo ? 1 : 0)); cmd.Parameters.Add(new OracleParameter("id", id)); return await cmd.ExecuteNonQueryAsync() == 1;
    }

    public async Task<long> CrearElementoCatalogoMatricesAsync(long catalogoId, string codigo, string valor, int orden)
    {
        await using var conn = _db.CreateConnection(); await conn.OpenAsync(); await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO RL_MR_ELEMENTOS_CATALOGO (ELE_ID, ELE_CATALOGO_ID, ELE_CODIGO, ELE_VALOR, ELE_ORDEN, ELE_ACTIVO) VALUES (SEQ_RL_MR_ELEMENTOS.NEXTVAL, :cat, :codigo, :valor, :orden, 1) RETURNING ELE_ID INTO :id";
        cmd.Parameters.Add(new OracleParameter("cat", catalogoId)); cmd.Parameters.Add(new OracleParameter("codigo", codigo.Trim().ToUpperInvariant())); cmd.Parameters.Add(new OracleParameter("valor", valor.Trim())); cmd.Parameters.Add(new OracleParameter("orden", orden)); var id = new OracleParameter("id", OracleDbType.Int64) { Direction = System.Data.ParameterDirection.Output }; cmd.Parameters.Add(id); await cmd.ExecuteNonQueryAsync(); return Convert.ToInt64(id.Value.ToString());
    }

    public async Task<bool> ActualizarElementoCatalogoMatricesAsync(long id, string valor, int orden, bool activo)
    {
        await using var conn = _db.CreateConnection(); await conn.OpenAsync(); await using var cmd = conn.CreateCommand(); cmd.CommandText = "UPDATE RL_MR_ELEMENTOS_CATALOGO SET ELE_VALOR = :valor, ELE_ORDEN = :orden, ELE_ACTIVO = :activo WHERE ELE_ID = :id"; cmd.Parameters.Add(new OracleParameter("valor", valor.Trim())); cmd.Parameters.Add(new OracleParameter("orden", orden)); cmd.Parameters.Add(new OracleParameter("activo", activo ? 1 : 0)); cmd.Parameters.Add(new OracleParameter("id", id)); return await cmd.ExecuteNonQueryAsync() == 1;
    }
}
