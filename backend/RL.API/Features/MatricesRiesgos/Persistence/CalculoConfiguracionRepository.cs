using System.Data;
using System.Data.Common;
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Infrastructure.Database;

namespace RL.API.Features.MatricesRiesgos.Persistence;

public sealed class CalculoConfiguracionRepository : ICalculoConfiguracionRepository
{
    private const string Modulo = "MatricesRiesgos";
    private readonly OracleDbContext _db;
    private readonly IAuditoriaRepository _auditoria;

    public CalculoConfiguracionRepository(OracleDbContext db, IAuditoriaRepository auditoria)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<IReadOnlyList<FormulaDto>> ListarFormulasAsync(bool incluirInactivas)
    {
        await using var c = _db.CreateConnection(); await c.OpenAsync();
        const string sql = "SELECT FOR_ID,FOR_CODIGO,FOR_NOMBRE,FOR_DESCRIPCION,FOR_ESTADO,FOR_FECHA_CREACION,FOR_VERSION_ROW FROM RL_MR_FORMULAS WHERE (:all=1 OR FOR_ESTADO='ACTIVE') ORDER BY FOR_CODIGO";
        await using var cmd = Command(sql, c); cmd.Parameters.Add(new OracleParameter("all", incluirInactivas ? 1 : 0));
        var result = new List<FormulaDto>(); await using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) result.Add(ReadFormula(r)); return result;
    }

    public async Task<FormulaDto?> ObtenerFormulaAsync(long id)
    {
        await using var c = _db.CreateConnection(); await c.OpenAsync();
        await using var cmd = Command("SELECT FOR_ID,FOR_CODIGO,FOR_NOMBRE,FOR_DESCRIPCION,FOR_ESTADO,FOR_FECHA_CREACION,FOR_VERSION_ROW FROM RL_MR_FORMULAS WHERE FOR_ID=:id", c); cmd.Parameters.Add(new OracleParameter("id", id));
        await using var r = await cmd.ExecuteReaderAsync(); return await r.ReadAsync() ? ReadFormula(r) : null;
    }

    public async Task<long> CrearFormulaAsync(CrearFormulaDto dto, long usuarioId, string? ip)
    {
        await using var c = _db.CreateConnection(); await c.OpenAsync(); await using var tx = c.BeginTransaction();
        try
        {
            long formulaId = await Next(c, tx, "SEQ_RL_MR_FORMULAS");
            await Execute(c, tx, "INSERT INTO RL_MR_FORMULAS(FOR_ID,FOR_CODIGO,FOR_NOMBRE,FOR_DESCRIPCION,FOR_ESTADO,FOR_FECHA_CREACION,FOR_USR_CREACION,FOR_VERSION_ROW) VALUES(:id,:code,:name,:description,'ACTIVE',SYSDATE,:userId,1)",
                P("id", formulaId), P("code", dto.Codigo), P("name", dto.Nombre), P("description", (object?)dto.Descripcion ?? DBNull.Value), P("userId", usuarioId));
            await InsertFormulaVersion(c, tx, formulaId, 1, dto.VersionInicial, usuarioId);
            await _auditoria.RegistrarAsync(c, tx, "RL_MR_FORMULAS", formulaId.ToString(), "INSERT", null, AuditJson(dto.Codigo, 1, "DRAFT"), usuarioId, null, ip, Modulo);
            await tx.CommitAsync(); return formulaId;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<long> CrearFormulaVersionAsync(long formulaId, CrearFormulaVersionDto dto, long usuarioId, string? ip)
    {
        await using var c = _db.CreateConnection(); await c.OpenAsync(); await using var tx = c.BeginTransaction();
        try
        {
            await Lock(c, tx, "SELECT FOR_ID FROM RL_MR_FORMULAS WHERE FOR_ID=:id AND FOR_ESTADO='ACTIVE' FOR UPDATE", formulaId);
            int next = await NextVersion(c, tx, "FOV_VERSION", "FOV_FORMULA_ID", "RL_MR_FORMULA_VERSIONES", formulaId);
            long id = await Next(c, tx, "SEQ_RL_MR_FORMULA_VERSIONES"); await InsertFormulaVersion(c, tx, formulaId, next, dto, usuarioId, id);
            await _auditoria.RegistrarAsync(c, tx, "RL_MR_FORMULA_VERSIONES", id.ToString(), "INSERT", null, AuditJson(formulaId, next, "DRAFT"), usuarioId, null, ip, Modulo);
            await tx.CommitAsync(); return id;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<bool> ActualizarFormulaBorradorAsync(long versionId, ActualizarFormulaBorradorDto dto, long usuarioId, string? ip)
    {
        string hash = FormulaHash(dto.Expresion, dto.TipoResultado);
        await using var c = _db.CreateConnection(); await c.OpenAsync(); await using var tx = c.BeginTransaction();
        try
        {
            const string sql = "UPDATE RL_MR_FORMULA_VERSIONES SET FOV_EXPRESION=:expression,FOV_TIPO_RESULTADO=:resultType,FOV_HASH=:hash,FOV_VERSION_ROW=FOV_VERSION_ROW+1 WHERE FOV_ID=:id AND FOV_ESTADO='DRAFT' AND FOV_VERSION_ROW=:row";
            int n = await Execute(c, tx, sql, PClob("expression", dto.Expresion), P("resultType", dto.TipoResultado.Trim().ToUpperInvariant()), P("hash", hash), P("id", versionId), P("row", dto.VersionRow));
            if (n != 1) { await tx.RollbackAsync(); return false; }
            await _auditoria.RegistrarAsync(c, tx, "RL_MR_FORMULA_VERSIONES", versionId.ToString(), "UPDATE", null, AuditJson(versionId, dto.VersionRow + 1, "DRAFT"), usuarioId, null, ip, Modulo);
            await tx.CommitAsync(); return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<bool> CrearFormulaUsoAsync(CrearFormulaUsoDto dto, long usuarioId, string? ip)
    {
        string fieldKey = dto.CampoClave.Trim();
        if (fieldKey.Length == 0) throw new InvalidOperationException("El campo contractual es obligatorio.");
        await using var c = _db.CreateConnection(); await c.OpenAsync(); await using var tx = c.BeginTransaction();
        try
        {
            long id = await Next(c, tx, "SEQ_RL_MR_FORMULA_USOS");
            await Execute(c, tx, "INSERT INTO RL_MR_FORMULA_USOS(FUS_ID,FUS_VERSION_FORMULARIO_ID,FUS_CAMPO_CLAVE,FUS_FORMULA_VERSION_ID,FUS_FECHA_CREACION,FUS_USR_CREACION) VALUES(:id,:formVersion,:fieldKey,:formulaVersion,SYSDATE,:userId)",
                P("id", id), P("formVersion", dto.VersionFormularioId), P("fieldKey", fieldKey), P("formulaVersion", dto.FormulaVersionId), P("userId", usuarioId));
            await _auditoria.RegistrarAsync(c, tx, "RL_MR_FORMULA_USOS", id.ToString(), "INSERT", null, AuditJson(fieldKey, 0, "ACTIVE"), usuarioId, null, ip, Modulo);
            await tx.CommitAsync(); return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<IReadOnlyList<FormulaVersionDto>> ListarFormulaVersionesAsync(long formulaId)
    {
        await using var c = _db.CreateConnection(); await c.OpenAsync();
        const string sql = "SELECT FOV_ID,FOV_FORMULA_ID,FOV_VERSION,FOV_EXPRESION,FOV_TIPO_RESULTADO,FOV_ESTADO,FOV_HASH,FOV_FECHA_INICIO,FOV_FECHA_FIN,FOV_FECHA_CREACION,FOV_VERSION_ROW FROM RL_MR_FORMULA_VERSIONES WHERE FOV_FORMULA_ID=:id ORDER BY FOV_VERSION DESC";
        await using var cmd = Command(sql, c); cmd.Parameters.Add(new OracleParameter("id", formulaId)); return await ReadFormulaVersions(cmd);
    }

    public async Task<IReadOnlyList<FormulaUsageDto>> ListarFormulaUsagesAsync(long formulaId)
    {
        await using var c = _db.CreateConnection(); await c.OpenAsync();
        const string sql = "SELECT u.FUS_ID,u.FUS_VERSION_FORMULARIO_ID,u.FUS_CAMPO_CLAVE,u.FUS_FORMULA_VERSION_ID,v.FOV_VERSION,f.FOR_CODIGO FROM RL_MR_FORMULA_USOS u JOIN RL_MR_FORMULA_VERSIONES v ON v.FOV_ID=u.FUS_FORMULA_VERSION_ID JOIN RL_MR_FORMULAS f ON f.FOR_ID=v.FOV_FORMULA_ID WHERE v.FOV_FORMULA_ID=:id ORDER BY u.FUS_VERSION_FORMULARIO_ID,u.FUS_CAMPO_CLAVE";
        await using var cmd = Command(sql, c); cmd.Parameters.Add(new OracleParameter("id", formulaId)); var result = new List<FormulaUsageDto>(); await using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) result.Add(new FormulaUsageDto { Id=r.GetInt64(0), VersionFormularioId=r.GetInt64(1), CampoClave=r.GetString(2), FormulaVersionId=r.GetInt64(3), FormulaVersion=r.GetInt32(4), FormulaCodigo=r.GetString(5) }); return result;
    }

    public async Task<bool> CambiarEstadoFormulaAsync(long formulaId, string estado, int versionRow, long usuarioId, string? ip)
    {
        await using var c = _db.CreateConnection(); await c.OpenAsync(); await using var tx = c.BeginTransaction();
        try { int n=await Execute(c,tx,"UPDATE RL_MR_FORMULAS SET FOR_ESTADO=:state,FOR_VERSION_ROW=FOR_VERSION_ROW+1 WHERE FOR_ID=:id AND FOR_VERSION_ROW=:row",P("state",estado),P("id",formulaId),P("row",versionRow)); if(n!=1){await tx.RollbackAsync();return false;} await _auditoria.RegistrarAsync(c,tx,"RL_MR_FORMULAS",formulaId.ToString(),"UPDATE",null,AuditJson(formulaId,versionRow+1,estado),usuarioId,null,ip,Modulo); await tx.CommitAsync();return true; }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<IReadOnlyList<FuncionDto>> ListarFuncionesAsync(bool incluirInactivas)
    {
        await using var c=_db.CreateConnection();await c.OpenAsync();await using var cmd=Command("SELECT FUN_ID,FUN_CODIGO,FUN_NOMBRE,FUN_DESCRIPCION,FUN_CATEGORIA,FUN_ESTADO,FUN_VERSION_ROW FROM RL_MR_FUNCIONES WHERE (:all=1 OR FUN_ESTADO='ACTIVE') ORDER BY FUN_CODIGO",c);cmd.Parameters.Add(new OracleParameter("all",incluirInactivas?1:0));var x=new List<FuncionDto>();await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())x.Add(new FuncionDto{Id=r.GetInt64(0),Codigo=r.GetString(1),Nombre=r.GetString(2),Descripcion=NullString(r,3),Categoria=r.GetString(4),Estado=r.GetString(5),VersionRow=r.GetInt32(6)});return x;
    }

    public async Task<FuncionDto?> ObtenerFuncionAsync(long id)
    {
        await using var c=_db.CreateConnection();await c.OpenAsync();await using var cmd=Command("SELECT FUN_ID,FUN_CODIGO,FUN_NOMBRE,FUN_DESCRIPCION,FUN_CATEGORIA,FUN_ESTADO,FUN_VERSION_ROW FROM RL_MR_FUNCIONES WHERE FUN_ID=:id",c);cmd.Parameters.Add(new OracleParameter("id",id));await using var r=await cmd.ExecuteReaderAsync();return await r.ReadAsync()?new FuncionDto{Id=r.GetInt64(0),Codigo=r.GetString(1),Nombre=r.GetString(2),Descripcion=NullString(r,3),Categoria=r.GetString(4),Estado=r.GetString(5),VersionRow=r.GetInt32(6)}:null;
    }

    public async Task<long> CrearFuncionAsync(CrearFuncionDto dto,long usuarioId,string? ip)
    {
        var validated=CalculoConfiguracionValidation.ValidateFunctionVersion(dto.VersionInicial);await using var c=_db.CreateConnection();await c.OpenAsync();await using var tx=c.BeginTransaction();try{long id=await Next(c,tx,"SEQ_RL_MR_FUNCIONES");await Execute(c,tx,"INSERT INTO RL_MR_FUNCIONES(FUN_ID,FUN_CODIGO,FUN_NOMBRE,FUN_DESCRIPCION,FUN_CATEGORIA,FUN_ESTADO,FUN_FECHA_CREACION,FUN_USR_CREACION,FUN_VERSION_ROW) VALUES(:id,:code,:name,:description,:category,'ACTIVE',SYSDATE,:userId,1)",P("id",id),P("code",dto.Codigo),P("name",dto.Nombre),P("description",(object?)dto.Descripcion??DBNull.Value),P("category",dto.Categoria),P("userId",usuarioId));long versionId=await Next(c,tx,"SEQ_RL_MR_FUNCION_VERSIONES");await InsertFunctionVersion(c,tx,id,1,dto.VersionInicial,validated,versionId,usuarioId);await _auditoria.RegistrarAsync(c,tx,"RL_MR_FUNCIONES",id.ToString(),"INSERT",null,AuditJson(dto.Codigo,1,"DRAFT"),usuarioId,null,ip,Modulo);await tx.CommitAsync();return id;}catch{await tx.RollbackAsync();throw;}
    }

    public async Task<long> CrearFuncionVersionAsync(long funcionId,CrearFuncionVersionDto dto,long usuarioId,string? ip)
    {
        var validated=CalculoConfiguracionValidation.ValidateFunctionVersion(dto);await using var c=_db.CreateConnection();await c.OpenAsync();await using var tx=c.BeginTransaction();try{await Lock(c,tx,"SELECT FUN_ID FROM RL_MR_FUNCIONES WHERE FUN_ID=:id AND FUN_ESTADO='ACTIVE' FOR UPDATE",funcionId);int next=await NextVersion(c,tx,"FUV_VERSION","FUV_FUNCION_ID","RL_MR_FUNCION_VERSIONES",funcionId);long id=await Next(c,tx,"SEQ_RL_MR_FUNCION_VERSIONES");await InsertFunctionVersion(c,tx,funcionId,next,dto,validated,id,usuarioId);await _auditoria.RegistrarAsync(c,tx,"RL_MR_FUNCION_VERSIONES",id.ToString(),"INSERT",null,AuditJson(funcionId,next,"DRAFT"),usuarioId,null,ip,Modulo);await tx.CommitAsync();return id;}catch{await tx.RollbackAsync();throw;}
    }

    public async Task<bool> ActualizarFuncionBorradorAsync(long versionId, ActualizarFuncionBorradorDto dto, long usuarioId, string? ip)
    {
        var normalized = CalculoConfiguracionValidation.ValidateFunctionVersion(dto);
        await using var c = _db.CreateConnection(); await c.OpenAsync(); await using var tx = c.BeginTransaction();
        try
        {
            const string update = "UPDATE RL_MR_FUNCION_VERSIONES SET FUV_TIPO=:type,FUV_TIPO_RESULTADO=:resultType,FUV_SIGNATURE_JSON=:signature,FUV_DEFINICION_DSL=:dsl,FUV_HANDLER_KEY=:handler,FUV_MIN_ARITY=:minArity,FUV_MAX_ARITY=:maxArity,FUV_HASH=:hash,FUV_VERSION_ROW=FUV_VERSION_ROW+1 WHERE FUV_ID=:id AND FUV_ESTADO='DRAFT' AND FUV_VERSION_ROW=:row";
            string hash = CalculoConfiguracionValidation.Hash(string.Join("|", normalized.Tipo, dto.TipoResultado.Trim().ToUpperInvariant(), normalized.Handler, normalized.Dsl, normalized.Signature));
            int n = await Execute(c, tx, update, P("type", normalized.Tipo), P("resultType", dto.TipoResultado.Trim().ToUpperInvariant()), PClob("signature", normalized.Signature), PClob("dsl", (object?)normalized.Dsl ?? DBNull.Value), P("handler", (object?)normalized.Handler ?? DBNull.Value), P("minArity", dto.MinArity), P("maxArity", (object?)dto.MaxArity ?? DBNull.Value), P("hash", hash), P("id", versionId), P("row", dto.VersionRow));
            if (n != 1) { await tx.RollbackAsync(); return false; }
            await Execute(c, tx, "DELETE FROM RL_MR_FUNCION_ARGUMENTOS WHERE FUA_FUNCION_VERSION_ID=:id", P("id", versionId));
            foreach (var arg in dto.Argumentos)
            {
                long argId = await Next(c, tx, "SEQ_RL_MR_FUNCION_ARGUMENTOS");
                await Execute(c, tx, "INSERT INTO RL_MR_FUNCION_ARGUMENTOS(FUA_ID,FUA_FUNCION_VERSION_ID,FUA_POSICION,FUA_CODIGO,FUA_NOMBRE,FUA_TIPO,FUA_REQUERIDO,FUA_VARIADIC,FUA_DEFAULT_JSON,FUA_DESCRIPCION) VALUES(:id,:versionId,:position,:code,:name,:type,:required,:variadic,:defaultJson,:description)",
                    P("id", argId), P("versionId", versionId), P("position", arg.Posicion), P("code", arg.Codigo.Trim().ToUpperInvariant()), P("name", arg.Nombre), P("type", arg.Tipo.Trim().ToUpperInvariant()), P("required", arg.Requerido ? 1 : 0), P("variadic", arg.Variadic ? 1 : 0), PClob("defaultJson", (object?)arg.ValorDefaultJson ?? DBNull.Value), P("description", (object?)arg.Descripcion ?? DBNull.Value));
            }
            await _auditoria.RegistrarAsync(c, tx, "RL_MR_FUNCION_VERSIONES", versionId.ToString(), "UPDATE", null, AuditJson(versionId, dto.VersionRow + 1, "DRAFT"), usuarioId, null, ip, Modulo);
            await tx.CommitAsync(); return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<bool> CambiarEstadoFuncionVersionAsync(long versionId,CambiarEstadoConfiguracionDto dto,long usuarioId,string? ip)=>await ChangeVersionState(VersionResource.Function,versionId,dto,usuarioId,ip);

    public async Task<IReadOnlyList<FuncionVersionDto>> ListarFuncionVersionesAsync(long funcionId)
    {
        await using var c=_db.CreateConnection();await c.OpenAsync();await using var cmd=Command("SELECT FUV_ID,FUV_FUNCION_ID,FUV_VERSION,FUV_TIPO,FUV_TIPO_RESULTADO,FUV_SIGNATURE_JSON,FUV_DEFINICION_DSL,FUV_HANDLER_KEY,FUV_MIN_ARITY,FUV_MAX_ARITY,FUV_ESTADO,FUV_HASH,FUV_VERSION_ROW FROM RL_MR_FUNCION_VERSIONES WHERE FUV_FUNCION_ID=:id ORDER BY FUV_VERSION DESC",c);cmd.Parameters.Add(new OracleParameter("id",funcionId));var x=new List<FuncionVersionDto>();await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())x.Add(ReadFunctionVersion(r));return x;
    }

    public async Task<IReadOnlyList<FuncionArgumentoDto>> ListarFuncionArgumentosAsync(long versionId)
    {
        await using var c=_db.CreateConnection();await c.OpenAsync();await using var cmd=Command("SELECT FUA_ID,FUA_FUNCION_VERSION_ID,FUA_POSICION,FUA_CODIGO,FUA_NOMBRE,FUA_TIPO,FUA_REQUERIDO,FUA_VARIADIC,FUA_DEFAULT_JSON,FUA_DESCRIPCION FROM RL_MR_FUNCION_ARGUMENTOS WHERE FUA_FUNCION_VERSION_ID=:id ORDER BY FUA_POSICION",c);cmd.Parameters.Add(new OracleParameter("id",versionId));var x=new List<FuncionArgumentoDto>();await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())x.Add(new FuncionArgumentoDto{Id=r.GetInt64(0),FuncionVersionId=r.GetInt64(1),Posicion=r.GetInt32(2),Codigo=r.GetString(3),Nombre=r.GetString(4),Tipo=r.GetString(5),Requerido=r.GetInt32(6)==1,Variadic=r.GetInt32(7)==1,ValorDefaultJson=NullString(r,8),Descripcion=NullString(r,9)});return x;
    }

    public async Task<IReadOnlyList<ParametroDto>> ListarParametrosAsync(bool incluirInactivas)
    {
        await using var c=_db.CreateConnection();await c.OpenAsync();await using var cmd=Command("SELECT PAC_ID,PAC_CODIGO,PAC_NOMBRE,PAC_DESCRIPCION,PAC_TIPO,PAC_ESTADO,PAC_VERSION_ROW FROM RL_MR_PARAMETROS_CALCULO WHERE (:all=1 OR PAC_ESTADO='ACTIVE') ORDER BY PAC_CODIGO",c);cmd.Parameters.Add(new OracleParameter("all",incluirInactivas?1:0));var x=new List<ParametroDto>();await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())x.Add(new ParametroDto{Id=r.GetInt64(0),Codigo=r.GetString(1),Nombre=r.GetString(2),Descripcion=NullString(r,3),Tipo=r.GetString(4),Estado=r.GetString(5),VersionRow=r.GetInt32(6)});return x;
    }

    public async Task<ParametroDto?> ObtenerParametroAsync(long id)
    {
        await using var c=_db.CreateConnection();await c.OpenAsync();await using var cmd=Command("SELECT PAC_ID,PAC_CODIGO,PAC_NOMBRE,PAC_DESCRIPCION,PAC_TIPO,PAC_ESTADO,PAC_VERSION_ROW FROM RL_MR_PARAMETROS_CALCULO WHERE PAC_ID=:id",c);cmd.Parameters.Add(new OracleParameter("id",id));await using var r=await cmd.ExecuteReaderAsync();return await r.ReadAsync()?new ParametroDto{Id=r.GetInt64(0),Codigo=r.GetString(1),Nombre=r.GetString(2),Descripcion=NullString(r,3),Tipo=r.GetString(4),Estado=r.GetString(5),VersionRow=r.GetInt32(6)}:null;
    }

    public async Task<long> CrearParametroAsync(CrearParametroDto dto,long usuarioId,string? ip)
    {
        string type=CalculoConfiguracionValidation.ValidateParameterVersion(dto.VersionInicial);await using var c=_db.CreateConnection();await c.OpenAsync();await using var tx=c.BeginTransaction();try{long id=await Next(c,tx,"SEQ_RL_MR_PARAMETROS");await Execute(c,tx,"INSERT INTO RL_MR_PARAMETROS_CALCULO(PAC_ID,PAC_CODIGO,PAC_NOMBRE,PAC_DESCRIPCION,PAC_TIPO,PAC_ESTADO,PAC_FECHA_CREACION,PAC_USR_CREACION,PAC_VERSION_ROW) VALUES(:id,:code,:name,:description,:type,'ACTIVE',SYSDATE,:userId,1)",P("id",id),P("code",dto.Codigo),P("name",dto.Nombre),P("description",(object?)dto.Descripcion??DBNull.Value),P("type",type),P("userId",usuarioId));long versionId=await Next(c,tx,"SEQ_RL_MR_PARAMETRO_VERSIONES");await InsertParameterVersion(c,tx,id,1,dto.VersionInicial,type,versionId,usuarioId);await _auditoria.RegistrarAsync(c,tx,"RL_MR_PARAMETROS_CALCULO",id.ToString(),"INSERT",null,AuditJson(dto.Codigo,1,"DRAFT"),usuarioId,null,ip,Modulo);await tx.CommitAsync();return id;}catch{await tx.RollbackAsync();throw;}
    }

    public async Task<long> CrearParametroVersionAsync(long parametroId,CrearParametroVersionDto dto,long usuarioId,string? ip)
    {
        string type=CalculoConfiguracionValidation.ValidateParameterVersion(dto);await using var c=_db.CreateConnection();await c.OpenAsync();await using var tx=c.BeginTransaction();try{await Lock(c,tx,"SELECT PAC_ID FROM RL_MR_PARAMETROS_CALCULO WHERE PAC_ID=:id AND PAC_ESTADO='ACTIVE' FOR UPDATE",parametroId);int next=await NextVersion(c,tx,"PAV_VERSION","PAV_PARAMETRO_ID","RL_MR_PARAMETRO_VERSIONES",parametroId);long id=await Next(c,tx,"SEQ_RL_MR_PARAMETRO_VERSIONES");await InsertParameterVersion(c,tx,parametroId,next,dto,type,id,usuarioId);await _auditoria.RegistrarAsync(c,tx,"RL_MR_PARAMETRO_VERSIONES",id.ToString(),"INSERT",null,AuditJson(parametroId,next,"DRAFT"),usuarioId,null,ip,Modulo);await tx.CommitAsync();return id;}catch{await tx.RollbackAsync();throw;}
    }

    public async Task<bool> ActualizarParametroBorradorAsync(long versionId, ActualizarParametroBorradorDto dto, long usuarioId, string? ip)
    {
        string type = CalculoConfiguracionValidation.ValidateParameterVersion(dto);
        await using var c = _db.CreateConnection(); await c.OpenAsync(); await using var tx = c.BeginTransaction();
        try
        {
            string value = JsonSerializer.Serialize(new { type, dto.ValorEntero, dto.ValorDecimal, dto.ValorBooleano, dto.ValorTexto, dto.ValorFecha });
            const string sql = "UPDATE RL_MR_PARAMETRO_VERSIONES SET PAV_TIPO=:type,PAV_VALOR_ENTERO=:integerValue,PAV_VALOR_DECIMAL=:decimalValue,PAV_VALOR_BOOLEANO=:booleanValue,PAV_VALOR_TEXTO=:textValue,PAV_VALOR_FECHA=:dateValue,PAV_HASH=:hash,PAV_VERSION_ROW=PAV_VERSION_ROW+1 WHERE PAV_ID=:id AND PAV_ESTADO='DRAFT' AND PAV_VERSION_ROW=:row";
            int n = await Execute(c, tx, sql, P("type", type), P("integerValue", (object?)dto.ValorEntero ?? DBNull.Value), P("decimalValue", (object?)dto.ValorDecimal ?? DBNull.Value), P("booleanValue", dto.ValorBooleano.HasValue ? (dto.ValorBooleano.Value ? 1 : 0) : (object)DBNull.Value), P("textValue", (object?)dto.ValorTexto ?? DBNull.Value), P("dateValue", (object?)dto.ValorFecha ?? DBNull.Value), P("hash", CalculoConfiguracionValidation.Hash(value)), P("id", versionId), P("row", dto.VersionRow));
            if (n != 1) { await tx.RollbackAsync(); return false; }
            await _auditoria.RegistrarAsync(c, tx, "RL_MR_PARAMETRO_VERSIONES", versionId.ToString(), "UPDATE", null, AuditJson(versionId, dto.VersionRow + 1, "DRAFT"), usuarioId, null, ip, Modulo);
            await tx.CommitAsync(); return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<bool> CambiarEstadoParametroVersionAsync(long versionId,CambiarEstadoConfiguracionDto dto,long usuarioId,string? ip)=>await ChangeVersionState(VersionResource.Parameter,versionId,dto,usuarioId,ip);

    public async Task<IReadOnlyList<ParametroVersionDto>> ListarParametroVersionesAsync(long parametroId)
    {
        await using var c=_db.CreateConnection();await c.OpenAsync();await using var cmd=Command("SELECT PAV_ID,PAV_PARAMETRO_ID,PAV_VERSION,PAV_TIPO,PAV_VALOR_ENTERO,PAV_VALOR_DECIMAL,PAV_VALOR_BOOLEANO,PAV_VALOR_TEXTO,PAV_VALOR_FECHA,PAV_ESTADO,PAV_HASH,PAV_VERSION_ROW FROM RL_MR_PARAMETRO_VERSIONES WHERE PAV_PARAMETRO_ID=:id ORDER BY PAV_VERSION DESC",c);cmd.Parameters.Add(new OracleParameter("id",parametroId));var x=new List<ParametroVersionDto>();await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())x.Add(new ParametroVersionDto{Id=r.GetInt64(0),ParametroId=r.GetInt64(1),Version=r.GetInt32(2),Tipo=r.GetString(3),ValorEntero=NullableInt(r,4),ValorDecimal=NullableDecimal(r,5),ValorBooleano=NullableBool(r,6),ValorTexto=NullString(r,7),ValorFecha=NullableDate(r,8),Estado=r.GetString(9),Hash=r.GetString(10),VersionRow=r.GetInt32(11)});return x;
    }

    private async Task InsertFormulaVersion(OracleConnection c,OracleTransaction tx,long formulaId,int version,CrearFormulaVersionDto dto,long usuarioId,long? id=null)
    { id ??= await Next(c,tx,"SEQ_RL_MR_FORMULA_VERSIONES");await Execute(c,tx,"INSERT INTO RL_MR_FORMULA_VERSIONES(FOV_ID,FOV_FORMULA_ID,FOV_VERSION,FOV_EXPRESION,FOV_TIPO_RESULTADO,FOV_ESTADO,FOV_HASH,FOV_FECHA_CREACION,FOV_USR_CREACION,FOV_VERSION_ROW) VALUES(:id,:formulaId,:version,:expression,:resultType,'DRAFT',:hash,SYSDATE,:userId,1)",P("id",id.Value),P("formulaId",formulaId),P("version",version),PClob("expression",dto.Expresion),P("resultType",dto.TipoResultado.Trim().ToUpperInvariant()),P("hash",FormulaHash(dto.Expresion,dto.TipoResultado)),P("userId",usuarioId)); }

    private async Task InsertFunctionVersion(OracleConnection c,OracleTransaction tx,long functionId,int version,CrearFuncionVersionDto dto,(string Tipo,string? Handler,string? Dsl,string Signature) normalized,long id,long userId)
    { string hash=CalculoConfiguracionValidation.Hash(string.Join("|",normalized.Tipo,dto.TipoResultado.Trim().ToUpperInvariant(),normalized.Handler,normalized.Dsl,normalized.Signature));await Execute(c,tx,"INSERT INTO RL_MR_FUNCION_VERSIONES(FUV_ID,FUV_FUNCION_ID,FUV_VERSION,FUV_TIPO,FUV_TIPO_RESULTADO,FUV_SIGNATURE_JSON,FUV_DEFINICION_DSL,FUV_HANDLER_KEY,FUV_MIN_ARITY,FUV_MAX_ARITY,FUV_ESTADO,FUV_HASH,FUV_FECHA_CREACION,FUV_USR_CREACION,FUV_VERSION_ROW) VALUES(:id,:functionId,:version,:type,:resultType,:signature,:dsl,:handler,:minArity,:maxArity,'DRAFT',:hash,SYSDATE,:userId,1)",P("id",id),P("functionId",functionId),P("version",version),P("type",normalized.Tipo),P("resultType",dto.TipoResultado.Trim().ToUpperInvariant()),PClob("signature",normalized.Signature),PClob("dsl",(object?)normalized.Dsl??DBNull.Value),P("handler",(object?)normalized.Handler??DBNull.Value),P("minArity",dto.MinArity),P("maxArity",(object?)dto.MaxArity??DBNull.Value),P("hash",hash),P("userId",userId));foreach(var arg in dto.Argumentos){long argId=await Next(c,tx,"SEQ_RL_MR_FUNCION_ARGUMENTOS");await Execute(c,tx,"INSERT INTO RL_MR_FUNCION_ARGUMENTOS(FUA_ID,FUA_FUNCION_VERSION_ID,FUA_POSICION,FUA_CODIGO,FUA_NOMBRE,FUA_TIPO,FUA_REQUERIDO,FUA_VARIADIC,FUA_DEFAULT_JSON,FUA_DESCRIPCION) VALUES(:id,:versionId,:position,:code,:name,:type,:required,:variadic,:defaultJson,:description)",P("id",argId),P("versionId",id),P("position",arg.Posicion),P("code",arg.Codigo.Trim().ToUpperInvariant()),P("name",arg.Nombre),P("type",arg.Tipo.Trim().ToUpperInvariant()),P("required",arg.Requerido?1:0),P("variadic",arg.Variadic?1:0),PClob("defaultJson",(object?)arg.ValorDefaultJson??DBNull.Value),P("description",(object?)arg.Descripcion??DBNull.Value));}}

    private static async Task InsertParameterVersion(OracleConnection c,OracleTransaction tx,long parameterId,int version,CrearParametroVersionDto dto,string type,long id,long userId)
    {string value=JsonSerializer.Serialize(new{type, dto.ValorEntero,dto.ValorDecimal,dto.ValorBooleano,dto.ValorTexto,dto.ValorFecha});await Execute(c,tx,"INSERT INTO RL_MR_PARAMETRO_VERSIONES(PAV_ID,PAV_PARAMETRO_ID,PAV_VERSION,PAV_TIPO,PAV_VALOR_ENTERO,PAV_VALOR_DECIMAL,PAV_VALOR_BOOLEANO,PAV_VALOR_TEXTO,PAV_VALOR_FECHA,PAV_ESTADO,PAV_HASH,PAV_FECHA_CREACION,PAV_USR_CREACION,PAV_VERSION_ROW) VALUES(:id,:parameterId,:version,:type,:integerValue,:decimalValue,:booleanValue,:textValue,:dateValue,'DRAFT',:hash,SYSDATE,:userId,1)",P("id",id),P("parameterId",parameterId),P("version",version),P("type",type),P("integerValue",(object?)dto.ValorEntero??DBNull.Value),P("decimalValue",(object?)dto.ValorDecimal??DBNull.Value),P("booleanValue",dto.ValorBooleano.HasValue?(dto.ValorBooleano.Value?1:0):(object)DBNull.Value),P("textValue",(object?)dto.ValorTexto??DBNull.Value),P("dateValue",(object?)dto.ValorFecha??DBNull.Value),P("hash",CalculoConfiguracionValidation.Hash(value)),P("userId",userId));}

    private enum VersionResource { Function, Parameter }

    private async Task<bool> ChangeVersionState(VersionResource resource,long id,CambiarEstadoConfiguracionDto dto,long userId,string? ip)
    {
        (string table,string idColumn,string stateColumn,string rowColumn) = resource switch
        {
            VersionResource.Function => ("RL_MR_FUNCION_VERSIONES", "FUV_ID", "FUV_ESTADO", "FUV_VERSION_ROW"),
            VersionResource.Parameter => ("RL_MR_PARAMETRO_VERSIONES", "PAV_ID", "PAV_ESTADO", "PAV_VERSION_ROW"),
            _ => throw new ArgumentOutOfRangeException(nameof(resource))
        };

        await using var c=_db.CreateConnection();await c.OpenAsync();await using var tx=c.BeginTransaction();
        try
        {
            string select = $"SELECT {stateColumn},{rowColumn} FROM {table} WHERE {idColumn}=:id FOR UPDATE";
            await using var read=Command(select,c,tx);read.Parameters.Add(P("id",id));
            await using var reader=await read.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) { await tx.RollbackAsync(); return false; }
            string currentState=reader.GetString(0);
            int currentRow=reader.GetInt32(1);
            if (currentRow != dto.VersionRow) { await tx.RollbackAsync(); return false; }
            CalculoConfiguracionValidation.ValidateVersionTransition(currentState,dto.Estado);

            string targetState=dto.Estado.Trim().ToUpperInvariant();
            string update=$"UPDATE {table} SET {stateColumn}=:state,{rowColumn}={rowColumn}+1 WHERE {idColumn}=:id AND {rowColumn}=:row AND {stateColumn}=:currentState";
            int n=await Execute(c,tx,update,P("state",targetState),P("id",id),P("row",dto.VersionRow),P("currentState",currentState));
            if(n!=1){await tx.RollbackAsync();return false;}
            await _auditoria.RegistrarAsync(c,tx,table,id.ToString(),"UPDATE",null,AuditJson(id,dto.VersionRow+1,targetState),userId,null,ip,Modulo);
            await tx.CommitAsync();return true;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    private static async Task<IReadOnlyList<FormulaVersionDto>> ReadFormulaVersions(OracleCommand cmd){var x=new List<FormulaVersionDto>();await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())x.Add(new FormulaVersionDto{Id=r.GetInt64(0),FormulaId=r.GetInt64(1),Version=r.GetInt32(2),Expresion=r.GetString(3),TipoResultado=r.GetString(4),Estado=r.GetString(5),Hash=r.GetString(6),FechaInicio=NullableDate(r,7),FechaFin=NullableDate(r,8),FechaCreacion=r.GetDateTime(9),VersionRow=r.GetInt32(10)});return x;}
    private static FormulaDto ReadFormula(OracleDataReader r)=>new(){Id=r.GetInt64(0),Codigo=r.GetString(1),Nombre=r.GetString(2),Descripcion=NullString(r,3),Estado=r.GetString(4),FechaCreacion=r.GetDateTime(5),VersionRow=r.GetInt32(6)};
    private static FuncionVersionDto ReadFunctionVersion(OracleDataReader r)=>new(){Id=r.GetInt64(0),FuncionId=r.GetInt64(1),Version=r.GetInt32(2),Tipo=r.GetString(3),TipoResultado=r.GetString(4),SignatureJson=NullString(r,5),DefinicionDsl=NullString(r,6),HandlerKey=NullString(r,7),MinArity=r.GetInt32(8),MaxArity=NullableInt(r,9),Estado=r.GetString(10),Hash=r.GetString(11),VersionRow=r.GetInt32(12)};
    private static OracleCommand Command(string sql,OracleConnection c,OracleTransaction? tx=null){var cmd=c.CreateCommand();cmd.BindByName=true;cmd.CommandText=sql;cmd.Transaction=tx;return cmd;}
    private static OracleParameter P(string name,object value)=>new(name,value);
    private static OracleParameter PClob(string name,object value)=>new(name,OracleDbType.Clob){Value=value};
    private static async Task<int> Execute(OracleConnection c,OracleTransaction tx,string sql,params OracleParameter[] ps){await using var cmd=Command(sql,c,tx);cmd.Parameters.AddRange(ps);return await cmd.ExecuteNonQueryAsync();}
    private static async Task Lock(OracleConnection c,OracleTransaction tx,string sql,long id){await using var cmd=Command(sql,c,tx);cmd.Parameters.Add(P("id",id));await using var r=await cmd.ExecuteReaderAsync();if(!await r.ReadAsync())throw new KeyNotFoundException("Recurso no encontrado o inactivo.");}
    internal static string ResolveSequenceSql(string sequence) => sequence switch
    {
        "SEQ_RL_MR_FORMULAS" => "SELECT SEQ_RL_MR_FORMULAS.NEXTVAL FROM DUAL",
        "SEQ_RL_MR_FORMULA_VERSIONES" => "SELECT SEQ_RL_MR_FORMULA_VERSIONES.NEXTVAL FROM DUAL",
        "SEQ_RL_MR_FORMULA_USOS" => "SELECT SEQ_RL_MR_FORMULA_USOS.NEXTVAL FROM DUAL",
        "SEQ_RL_MR_FUNCIONES" => "SELECT SEQ_RL_MR_FUNCIONES.NEXTVAL FROM DUAL",
        "SEQ_RL_MR_FUNCION_VERSIONES" => "SELECT SEQ_RL_MR_FUNCION_VERSIONES.NEXTVAL FROM DUAL",
        "SEQ_RL_MR_FUNCION_ARGUMENTOS" => "SELECT SEQ_RL_MR_FUNCION_ARGUMENTOS.NEXTVAL FROM DUAL",
        "SEQ_RL_MR_PARAMETROS" => "SELECT SEQ_RL_MR_PARAMETROS.NEXTVAL FROM DUAL",
        "SEQ_RL_MR_PARAMETRO_VERSIONES" => "SELECT SEQ_RL_MR_PARAMETRO_VERSIONES.NEXTVAL FROM DUAL",
        _ => throw new ArgumentOutOfRangeException(nameof(sequence))
    };

    private static async Task<long> Next(OracleConnection c,OracleTransaction tx,string sequence){await using var cmd=Command(ResolveSequenceSql(sequence),c,tx);return Convert.ToInt64(await cmd.ExecuteScalarAsync());}
    private static async Task<int> NextVersion(OracleConnection c,OracleTransaction tx,string versionColumn,string foreignColumn,string table,long id){await using var cmd=Command($"SELECT NVL(MAX({versionColumn}),0)+1 FROM {table} WHERE {foreignColumn}=:id",c,tx);cmd.Parameters.Add(P("id",id));return Convert.ToInt32(await cmd.ExecuteScalarAsync());}
    private static string FormulaHash(string expression,string resultType)=>CalculoConfiguracionValidation.Hash(expression.Trim()+"|"+resultType.Trim().ToUpperInvariant());
    private static string AuditJson(object code,int version,string state)=>JsonSerializer.Serialize(new{code,version,state});
    private static string? NullString(DbDataReader r,int i)=>r.IsDBNull(i)?null:r.GetString(i);
    private static int? NullableInt(DbDataReader r,int i)=>r.IsDBNull(i)?null:Convert.ToInt32(r.GetValue(i));
    private static bool? NullableBool(DbDataReader r,int i)=>r.IsDBNull(i)?null:Convert.ToInt32(r.GetValue(i))==1;
    private static decimal? NullableDecimal(DbDataReader r,int i)=>r.IsDBNull(i)?null:Convert.ToDecimal(r.GetValue(i));
    private static DateTime? NullableDate(DbDataReader r,int i)=>r.IsDBNull(i)?null:r.GetDateTime(i);
}
