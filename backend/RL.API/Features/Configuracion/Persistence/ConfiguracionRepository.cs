using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Configuracion.Contracts;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.Configuracion.Persistence;

public class ConfiguracionRepository : IConfiguracionRepository, ITransactionalConfiguracionRepository
{
    private readonly OracleDbContext _db;
    private readonly IAuditoriaRepository _auditoria;

    public ConfiguracionRepository(OracleDbContext db, IAuditoriaRepository auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<ConfigSistema?> ObtenerConfigSistemaAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM RL_CONFIG_SISTEMA WHERE SFS_ID = 1";

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new ConfigSistema
        {
            SfsId = Convert.ToInt32(reader["SFS_ID"]),
            NombreInstitucion = reader["SFS_NOMBRE_INSTITUCION"].ToString()!,
            NombreSistema = reader["SFS_NOMBRE_SISTEMA"].ToString()!,
            LogoUrl = reader["SFS_LOGO_URL"] == DBNull.Value ? null : reader["SFS_LOGO_URL"].ToString(),
            IconoUrl = reader["SFS_ICONO_URL"] == DBNull.Value ? null : reader["SFS_ICONO_URL"].ToString(),
            ColorPrimario = reader["SFS_COLOR_PRIMARIO"] == DBNull.Value ? null : reader["SFS_COLOR_PRIMARIO"].ToString(),
            ColorSecundario = reader["SFS_COLOR_SECUNDARIO"] == DBNull.Value ? null : reader["SFS_COLOR_SECUNDARIO"].ToString(),
            ColorEncabezadoReportes = TryGetString(reader, "SFS_COLOR_ENCABEZADO_REPORTES"),
            TimeoutSesion = Convert.ToInt32(reader["SFS_TIMEOUT_SESION"]),
            AcuerdoLegal = reader["SFS_ACUERDO_LEGAL"] == DBNull.Value ? null : reader["SFS_ACUERDO_LEGAL"].ToString(),
            MaxIntentos = reader["SFS_MAX_INTENTOS"] == DBNull.Value ? 5 : Convert.ToInt32(reader["SFS_MAX_INTENTOS"]),
            ValidezClaveTemp = reader["SFS_VALIDEZ_CLAVE_TEMP"] == DBNull.Value ? 15 : Convert.ToInt32(reader["SFS_VALIDEZ_CLAVE_TEMP"]),
            UltimaActualizacion = Convert.ToDateTime(reader["SFS_ULTIMA_ACTUALIZACION"])
        };
    }

    public async Task<bool> GuardarConfigSistemaAsync(ConfigSistema config)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = @"
            UPDATE RL_CONFIG_SISTEMA
            SET SFS_NOMBRE_INSTITUCION = :NombreInstitucion,
                SFS_NOMBRE_SISTEMA = :NombreSistema,
                SFS_LOGO_URL = :LogoUrl,
                SFS_ICONO_URL = :IconoUrl,
                SFS_COLOR_PRIMARIO = :ColorPrimario,
                SFS_COLOR_SECUNDARIO = :ColorSecundario,
                SFS_COLOR_ENCABEZADO_REPORTES = :ColorEncabezadoReportes,
                SFS_TIMEOUT_SESION = SFS_TIMEOUT_SESION,
                SFS_ACUERDO_LEGAL = :AcuerdoLegal,
                SFS_MAX_INTENTOS = SFS_MAX_INTENTOS,
                SFS_VALIDEZ_CLAVE_TEMP = SFS_VALIDEZ_CLAVE_TEMP,
                SFS_ULTIMA_ACTUALIZACION = SYSDATE
            WHERE SFS_ID = 1";

        cmd.Parameters.Add(new OracleParameter("NombreInstitucion", config.NombreInstitucion));
        cmd.Parameters.Add(new OracleParameter("NombreSistema", config.NombreSistema));
        cmd.Parameters.Add(new OracleParameter("LogoUrl", (object?)config.LogoUrl ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("IconoUrl", (object?)config.IconoUrl ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("ColorPrimario", (object?)config.ColorPrimario ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("ColorSecundario", (object?)config.ColorSecundario ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("ColorEncabezadoReportes", (object?)config.ColorEncabezadoReportes ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("AcuerdoLegal", (object?)config.AcuerdoLegal ?? DBNull.Value));


        int rowsAffected;
        try
        {
            rowsAffected = await cmd.ExecuteNonQueryAsync();
        }
        catch (OracleException exception) when (EsFalloEsquemaBranding(exception))
        {
            throw CrearErrorEsquemaBranding(exception);
        }
        // La ruta de producción utiliza la operación transaccional; esta compatibilidad
        // directa no debe ejecutar un segundo UPDATE ni publicar éxito parcial.
        if (DebeEjecutarActualizacionCompatibilidad(rowsAffected, config.ColorEncabezadoReportes))
        {
            // La columna se incorpora mediante la migración B06 controlada. Mantener
            // el resto de la configuración operativa aun cuando la migración aún no
            // se haya desplegado en una base de desarrollo.
            try
            {
                await using var brandingCmd = conn.CreateCommand();
                brandingCmd.BindByName = true;
                brandingCmd.CommandText = "UPDATE RL_CONFIG_SISTEMA SET SFS_COLOR_ENCABEZADO_REPORTES = :ColorEncabezadoReportes WHERE SFS_ID = 1";
                brandingCmd.Parameters.Add(new OracleParameter("ColorEncabezadoReportes", config.ColorEncabezadoReportes));
                await brandingCmd.ExecuteNonQueryAsync();
            }
            catch (OracleException exception) when (exception.Number is 904 or 942 or 2289)
            {
                throw new InvalidOperationException("B06_BRANDING_SCHEMA=NOT_READY: falta la columna de color de encabezado de reportes.", exception);
            }
        }
        return rowsAffected > 0;
    }

    public async Task<bool> GuardarConfigSistemaConAuditoriaAsync(
        ConfigSistema config,
        string? datosAnteriores,
        string datosNuevos,
        long usuarioId,
        string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var transaction = (OracleTransaction)await conn.BeginTransactionAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.BindByName = true;
            cmd.CommandText = @"
                UPDATE RL_CONFIG_SISTEMA
                SET SFS_NOMBRE_INSTITUCION = :NombreInstitucion,
                    SFS_NOMBRE_SISTEMA = :NombreSistema,
                    SFS_LOGO_URL = :LogoUrl,
                    SFS_ICONO_URL = :IconoUrl,
                    SFS_COLOR_PRIMARIO = :ColorPrimario,
                    SFS_COLOR_SECUNDARIO = :ColorSecundario,
                    SFS_COLOR_ENCABEZADO_REPORTES = :ColorEncabezadoReportes,
                    SFS_TIMEOUT_SESION = SFS_TIMEOUT_SESION,
                    SFS_ACUERDO_LEGAL = :AcuerdoLegal,
                    SFS_MAX_INTENTOS = SFS_MAX_INTENTOS,
                    SFS_VALIDEZ_CLAVE_TEMP = SFS_VALIDEZ_CLAVE_TEMP,
                    SFS_ULTIMA_ACTUALIZACION = SYSDATE
                WHERE SFS_ID = 1";
            cmd.Parameters.Add(new OracleParameter("NombreInstitucion", config.NombreInstitucion));
            cmd.Parameters.Add(new OracleParameter("NombreSistema", config.NombreSistema));
            cmd.Parameters.Add(new OracleParameter("LogoUrl", (object?)config.LogoUrl ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("IconoUrl", (object?)config.IconoUrl ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("ColorPrimario", (object?)config.ColorPrimario ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("ColorSecundario", (object?)config.ColorSecundario ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("ColorEncabezadoReportes", (object?)config.ColorEncabezadoReportes ?? DBNull.Value));
            cmd.Parameters.Add(new OracleParameter("AcuerdoLegal", (object?)config.AcuerdoLegal ?? DBNull.Value));

            int rowsAffected;
            try
            {
                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }
            catch (OracleException exception) when (EsFalloEsquemaBranding(exception))
            {
                try { await transaction.RollbackAsync(); } catch { /* preservar la excepción primaria */ }
                throw CrearErrorEsquemaBranding(exception);
            }

            if (rowsAffected <= 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await _auditoria.RegistrarAsync(
                conn, transaction, "RL_CONFIG_SISTEMA", "1", "UPDATE",
                datosAnteriores, datosNuevos, usuarioId, null, ip, "Configuracion");
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            try { await transaction.RollbackAsync(); } catch { /* preservar la excepción primaria */ }
            throw;
        }
    }

    private static string? TryGetString(OracleDataReader reader, string columnName)
    {
        try
        {
            var value = reader[columnName];
            return value == DBNull.Value ? null : value.ToString();
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    private static bool EsFalloEsquemaBranding(OracleException exception) =>
        exception.Number is 904 or 942 or 2289;

    private static InvalidOperationException CrearErrorEsquemaBranding(OracleException exception) =>
        new(
            "B06_BRANDING_SCHEMA=NOT_READY: falta la estructura de persistencia de identidad institucional. Aplique la migración B06 autorizada y ejecute su postcheck.",
            exception);

    private static bool DebeEjecutarActualizacionCompatibilidad(int rowsAffected, string? color) => false;

    public async Task<List<LoginSlide>> ObtenerSlidesAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM RL_LOGIN_SLIDES WHERE SGL_ACTIVO = 1 ORDER BY SGL_ORDEN ASC";

        var list = new List<LoginSlide>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new LoginSlide
            {
                Id = Convert.ToInt32(reader["SGL_ID"]),
                ImagenUrl = reader["SGL_IMAGEN_URL"].ToString()!,
                Titulo = reader["SGL_TITULO"] == DBNull.Value ? null : reader["SGL_TITULO"].ToString(),
                Descripcion = reader["SGL_DESCRIPCION"] == DBNull.Value ? null : reader["SGL_DESCRIPCION"].ToString(),
                Orden = Convert.ToInt32(reader["SGL_ORDEN"]),
                Activo = Convert.ToInt32(reader["SGL_ACTIVO"]) == 1,
                ImagenIcono = reader["SGL_IMAGEN_ICONO"] == DBNull.Value ? null : reader["SGL_IMAGEN_ICONO"].ToString(),
                FechaModif = Convert.ToDateTime(reader["SGL_FECHA_MODIF"])
            });
        }
        return list;
    }

    public async Task<List<LoginSlide>> ObtenerTodosSlidesAsync()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM RL_LOGIN_SLIDES ORDER BY SGL_ORDEN ASC";

        var list = new List<LoginSlide>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new LoginSlide
            {
                Id = Convert.ToInt32(reader["SGL_ID"]),
                ImagenUrl = reader["SGL_IMAGEN_URL"].ToString()!,
                Titulo = reader["SGL_TITULO"] == DBNull.Value ? null : reader["SGL_TITULO"].ToString(),
                Descripcion = reader["SGL_DESCRIPCION"] == DBNull.Value ? null : reader["SGL_DESCRIPCION"].ToString(),
                Orden = Convert.ToInt32(reader["SGL_ORDEN"]),
                Activo = Convert.ToInt32(reader["SGL_ACTIVO"]) == 1,
                ImagenIcono = reader["SGL_IMAGEN_ICONO"] == DBNull.Value ? null : reader["SGL_IMAGEN_ICONO"].ToString(),
                FechaModif = Convert.ToDateTime(reader["SGL_FECHA_MODIF"])
            });
        }
        return list;
    }

    public async Task<bool> CrearSlideAsync(LoginSlide slide)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var idCmd = conn.CreateCommand();
        idCmd.CommandText = "SELECT COALESCE(MAX(SGL_ID), 0) + 1 FROM RL_LOGIN_SLIDES";
        var nextIdObj = await idCmd.ExecuteScalarAsync();
        int nextId = Convert.ToInt32(nextIdObj);
        slide.Id = nextId;

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = @"
            INSERT INTO RL_LOGIN_SLIDES (
                SGL_ID, SGL_IMAGEN_URL, SGL_TITULO, SGL_DESCRIPCION, SGL_ORDEN, SGL_ACTIVO, SGL_IMAGEN_ICONO, SGL_FECHA_MODIF
            ) VALUES (
                :Id, :ImagenUrl, :Titulo, :Descripcion, :Orden, :Activo, :ImagenIcono, SYSDATE
            )";

        cmd.Parameters.Add(new OracleParameter("Id", nextId));
        cmd.Parameters.Add(new OracleParameter("ImagenUrl", slide.ImagenUrl));
        cmd.Parameters.Add(new OracleParameter("Titulo", (object?)slide.Titulo ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("Descripcion", (object?)slide.Descripcion ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("Orden", slide.Orden));
        cmd.Parameters.Add(new OracleParameter("Activo", slide.Activo ? 1 : 0));
        cmd.Parameters.Add(new OracleParameter("ImagenIcono", (object?)slide.ImagenIcono ?? DBNull.Value));

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> ActualizarSlideAsync(LoginSlide slide)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = @"
            UPDATE RL_LOGIN_SLIDES
            SET SGL_IMAGEN_URL = :ImagenUrl,
                SGL_TITULO = :Titulo,
                SGL_DESCRIPCION = :Descripcion,
                SGL_ORDEN = :Orden,
                SGL_ACTIVO = :Activo,
                SGL_IMAGEN_ICONO = :ImagenIcono,
                SGL_FECHA_MODIF = SYSDATE
            WHERE SGL_ID = :Id";

        cmd.Parameters.Add(new OracleParameter("ImagenUrl", slide.ImagenUrl));
        cmd.Parameters.Add(new OracleParameter("Titulo", (object?)slide.Titulo ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("Descripcion", (object?)slide.Descripcion ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("Orden", slide.Orden));
        cmd.Parameters.Add(new OracleParameter("Activo", slide.Activo ? 1 : 0));
        cmd.Parameters.Add(new OracleParameter("ImagenIcono", (object?)slide.ImagenIcono ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("Id", slide.Id));

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> EliminarSlideAsync(int id)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = "DELETE FROM RL_LOGIN_SLIDES WHERE SGL_ID = :Id";
        cmd.Parameters.Add(new OracleParameter("Id", id));

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }
}
