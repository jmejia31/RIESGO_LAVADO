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
}
