using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Block5WorkbookFormulaOracle;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using RL.API.Features.Auditoria.Application;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.Catalogos.Application;
using RL.API.Features.Catalogos.Contracts;
using CatalogoMatricesCatalogDto = RL.API.Features.Catalogos.Contracts.CatalogoMatricesDto;
using CatalogoModulo = RL.API.Features.Catalogos.Contracts.Modulo;
using RL.API.Features.Configuracion.Application;
using RL.API.Features.Configuracion.Contracts;
using RL.API.Features.MatricesRiesgos;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Database;
using RL.API.Infrastructure.Caching;
using RL.API.Core.Security;

const string requiredFlag = "RL_BLOCK7_REAL_E2E_REQUIRED";
if (!string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "E2E", StringComparison.Ordinal)
    || (!string.Equals(Environment.GetEnvironmentVariable(requiredFlag), "true", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(Environment.GetEnvironmentVariable("RL_BLOCK9_REAL_E2E_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase)))
    throw new InvalidOperationException("Block 7 test host requires ASPNETCORE_ENVIRONMENT=E2E and the explicit real E2E flag.");

string configuredConnection = Environment.GetEnvironmentVariable("ConnectionStrings__OracleDB_LocalXE")
    ?? throw new InvalidOperationException("The disposable LOCAL_XE connection is required.");
var guardConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    [DatabaseProfileResolver.ProfileConfigurationKey] = DatabaseProfileResolver.LocalXeProfileName,
    ["ConnectionStrings:OracleDB_LocalXE"] = configuredConnection
}).Build();
string connectionString = DatabaseEnvironmentGuard.ValidateAndResolveDevelopmentConnection(configuredConnection, guardConfiguration);

await using var oracle = await OpenOracleWithRetryAsync(connectionString);
await EnsureFixtureAsync(oracle);
WorkbookFormulaOracle workbook = WorkbookFormulaOracle.Load(FindOfficialWorkbook());
var canonicalRisks = Enumerable.Range(2, 59)
    .Select(row => new { Ordinal = row - 1, Code = workbook.GetText(row, 2), Name = workbook.GetText(row, 3) })
    .ToArray();
if (canonicalRisks.Length != 59
    || canonicalRisks.Any(risk => string.IsNullOrWhiteSpace(risk.Code) || string.IsNullOrWhiteSpace(risk.Name))
    || canonicalRisks.Select(risk => risk.Code).Distinct(StringComparer.Ordinal).Count() != 59)
    throw new InvalidOperationException("The official workbook must provide 59 unique, named risk identities for the Block 12 browser fixture.");
long stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 800_000_000;
long riskId = stamp + 700_000_000;
long[] canonicalRiskIds = Enumerable.Range(0, canonicalRisks.Length).Select(index => riskId + index).ToArray();
long versionId = stamp + 700_000_001;
long userId = 987654;
long areaOnlyUserId = stamp + 10;
long ugrOnlyUserId = stamp + 11;
long bothUserId = stamp + 12;
long neitherUserId = stamp + 13;
long lifecycleFamilyId = stamp + 900_000_000;
long lifecycleVersionId = stamp + 900_000_001;
long lifecycleEvaluationId = stamp + 900_000_002;
long lifecycleEvidenceId = stamp + 900_000_003;
long lifecycleEvidenceLinkId = stamp + 900_000_004;
string lifecycleFamilyCode = $"H08-FAMILIA-{stamp}";
string riskCode = canonicalRisks[0].Code;
string ruleCode = $"B7-E2E-RULE-{stamp}";
string lifecycleRuleCode = $"H08-LIFECYCLE-{stamp}";
string definition = CreateDefinition(ruleCode);
string lifecycleDefinition = CreateDefinition(lifecycleRuleCode);
string lifecycleDefinitionHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(lifecycleDefinition))).ToLowerInvariant();
string lifecycleEvidenceHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("h08-evidence"))).ToLowerInvariant();
for (int index = 0; index < canonicalRisks.Length; index++)
{
    var risk = canonicalRisks[index];
    await ExecuteAsync(oracle, "INSERT INTO RL_MR_RIESGOS (RIE_ID,RIE_CODIGO,RIE_NOMBRE,RIE_DESCRIPCION,RIE_ACTIVO,RIE_USR_CREACION,RIE_FECHA_CREACION) VALUES (:id,:code,:name,'Block 12 disposable canonical identity',1,:usr,SYSDATE)",
        ("id", canonicalRiskIds[index]), ("code", risk.Code), ("name", risk.Name), ("usr", userId));
}
await ExecuteAsync(oracle, "MERGE INTO RL_MR_FAMILIAS_FORMULARIO destino USING (SELECT 1 FAM_ID FROM DUAL) origen ON (destino.FAM_ID = origen.FAM_ID) WHEN MATCHED THEN UPDATE SET FAM_CODIGO='BLOCK7_FAMILIA',FAM_NOMBRE='Familia E2E Block 7',FAM_DESCRIPCION='Fixture desechable para certificacion',FAM_ACTIVO=1,FAM_ARCHIVADA=0,FAM_PREDETERMINADA=0 WHEN NOT MATCHED THEN INSERT (FAM_ID,FAM_CODIGO,FAM_NOMBRE,FAM_DESCRIPCION,FAM_ACTIVO,FAM_ARCHIVADA,FAM_PREDETERMINADA,FAM_FECHA_CREACION) VALUES (1,'BLOCK7_FAMILIA','Familia E2E Block 7','Fixture desechable para certificacion',1,0,0,SYSDATE)");
await ExecuteAsync(oracle, "INSERT INTO RL_MR_VERSIONES_FORMULARIO (VER_ID,VER_FAMILIA_ID,VER_CODIGO,VER_VERSION,VER_JSON,VER_HASH,VER_ESTADO,VER_VIGENTE,VER_FECHA_INICIO,VER_FECHA_FIN,VER_FECHA_CREACION,VER_USR_CREACION) VALUES (:id,1,'BLOCK7_E2E',1,:json,RPAD('B',64,'B'),'PUBLISHED',1,SYSDATE,NULL,SYSDATE,:usr)", ("id", versionId), ("json", definition), ("usr", userId));
var fullChainUiFixtures = new List<object>();
bool fullChainSeeded = false;
await ExecuteAsync(oracle, "INSERT INTO RL_MR_REGLAS_CALCULO (REG_ID,REG_CODIGO,REG_VERSION,REG_NOMBRE,REG_ALGORITMO_ID,REG_ACTIVA) VALUES (:id,:code,'1','Disposable browser rule','BLOCK7_LEGACY_TEST',1)", ("id", versionId), ("code", ruleCode));
await ExecuteAsync(oracle, "INSERT INTO RL_MR_REGLAS_CALCULO (REG_ID,REG_CODIGO,REG_VERSION,REG_NOMBRE,REG_ALGORITMO_ID,REG_ACTIVA) VALUES (:id,:code,'1','Disposable H08 lifecycle rule','BLOCK7_LEGACY_TEST',1)", ("id", lifecycleVersionId), ("code", lifecycleRuleCode));
foreach (long id in new[] { userId, areaOnlyUserId, ugrOnlyUserId, bothUserId, neitherUserId })
    await ExecuteAsync(oracle, "INSERT INTO RL_USUARIOS (USR_ID,USR_ACTIVO) VALUES (:id,1)", ("id", id));
await ExecuteAsync(oracle, "INSERT INTO RL_USUARIO_CAPACIDADES (UCP_USR_ID,UCP_CAPACIDAD,UCP_ACTIVO) VALUES (:id,'MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR',1)", ("id", areaOnlyUserId));
await ExecuteAsync(oracle, "INSERT INTO RL_USUARIO_CAPACIDADES (UCP_USR_ID,UCP_CAPACIDAD,UCP_ACTIVO) VALUES (:id,'MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR',1)", ("id", ugrOnlyUserId));
foreach (string capability in new[] { "MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR", "MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR" })
{
    await ExecuteAsync(oracle, "INSERT INTO RL_USUARIO_CAPACIDADES (UCP_USR_ID,UCP_CAPACIDAD,UCP_ACTIVO) VALUES (:id,:capability,1)", ("id", bothUserId), ("capability", capability));
    await ExecuteAsync(oracle, "INSERT INTO RL_USUARIO_CAPACIDADES (UCP_USR_ID,UCP_CAPACIDAD,UCP_ACTIVO) VALUES (:id,:capability,1)", ("id", userId), ("capability", capability));
}
await ExecuteAsync(oracle, "INSERT INTO RL_MR_FAMILIAS_FORMULARIO (FAM_ID,FAM_CODIGO,FAM_NOMBRE,FAM_DESCRIPCION,FAM_ACTIVO,FAM_ARCHIVADA,FAM_PREDETERMINADA,FAM_FECHA_CREACION) VALUES (:id,:code,'H08 ciclo de vida','Fixture desechable H08',1,0,0,SYSDATE)", ("id", lifecycleFamilyId), ("code", lifecycleFamilyCode));
await ExecuteAsync(oracle, "INSERT INTO RL_MR_VERSIONES_FORMULARIO (VER_ID,VER_FAMILIA_ID,VER_CODIGO,VER_VERSION,VER_JSON,VER_HASH,VER_ESTADO,VER_VIGENTE,VER_FECHA_CREACION,VER_USR_CREACION) VALUES (:id,:family,:code,1,:json,:hash,'DRAFT',0,SYSDATE,:usr)", ("id", lifecycleVersionId), ("family", lifecycleFamilyId), ("code", lifecycleFamilyCode), ("json", lifecycleDefinition), ("hash", lifecycleDefinitionHash), ("usr", userId));
await ExecuteAsync(oracle, "INSERT INTO RL_MR_EVALUACIONES_RIESGO (EVA_ID,EVA_RIESGO_ID,EVA_VERSION_ID,EVA_DATOS_JSON,EVA_CALCULOS_JSON,EVA_FECHA_REGISTRO,EVA_USR_REGISTRO,EVA_VERSION_ROW,EVA_ACTIVO) VALUES (:id,:risk,:version,'{}','{}',SYSDATE,:usr,1,1)", ("id", lifecycleEvaluationId), ("risk", riskId), ("version", lifecycleVersionId), ("usr", userId));
await ExecuteAsync(oracle, "INSERT INTO RL_MR_EVIDENCIAS (EVI_ID,EVI_NOMBRE_ARCHIVO,EVI_EXTENSION,EVI_TAMANO,EVI_HASH,EVI_RUTA,EVI_USR_CREACION) VALUES (:id,'h08-evidence.txt','txt',12,:hash,'h08/:id',:usr)", ("id", lifecycleEvidenceId), ("hash", lifecycleEvidenceHash), ("usr", userId));
await ExecuteAsync(oracle, "INSERT INTO RL_MR_EVIDENCIAS_VINCULOS (EVV_ID,EVV_EVIDENCIA_ID,EVV_TIPO_ENTIDAD,EVV_ENTIDAD_ID,EVV_USR_CREACION) VALUES (:id,:evidence,'EVALUACION',:evaluation,:usr)", ("id", lifecycleEvidenceLinkId), ("evidence", lifecycleEvidenceId), ("evaluation", lifecycleEvaluationId), ("usr", userId));
await oracle.CloseAsync();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5043");
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins("http://127.0.0.1:4201").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication("Block7E2E")
    .AddScheme<AuthenticationSchemeOptions, E2EAuthenticationHandler>("Block7E2E", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddControllers().AddApplicationPart(typeof(MatricesRiesgosController).Assembly);
builder.Services.AddSingleton(new OracleDbContext(connectionString));
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddSingleton<ICatalogoService, E2ECatalogoService>();
// The real Angular bootstrap reads these endpoints before opening the lifecycle UI.
// The disposable schema intentionally does not provision global configuration tables;
// return an empty read-only contract without masking any H08 lifecycle request.
builder.Services.AddSingleton<IConfiguracionService, E2EConfiguracionService>();
builder.Services.AddSingleton<IApplicationCache, ApplicationMemoryCache>();
builder.Services.AddSingleton(new ApplicationCacheSettings());
builder.Services.AddScoped<MatricesRiesgosRepository>();
builder.Services.AddScoped<IFamiliasFormularioLifecycleRepository, FamiliasFormularioLifecycleRepository>();
builder.Services.AddScoped<IMatricesRiesgosRepository, SafeMatricesRiesgosRepository>();
builder.Services.AddScoped<FamiliasFormularioLifecycleService>();
builder.Services.AddScoped<IMatricesRiesgosGestionRepository, MatricesRiesgosGestionRepository>();
builder.Services.AddScoped<IMatricesRiesgosGestionService, MatricesRiesgosGestionService>();
builder.Services.AddScoped<IMatricesRiesgosMitigacionRepository, MatricesRiesgosMitigacionRepository>();
builder.Services.AddScoped<IMatricesRiesgosMitigacionService, MatricesRiesgosMitigacionService>();
builder.Services.AddScoped<IMatricesRiesgosMonitoreoRepository, MatricesRiesgosMonitoreoRepository>();
builder.Services.AddScoped<IMatricesRiesgosMonitoreoService, MatricesRiesgosMonitoreoService>();
builder.Services.AddScoped<IFormularioValidador, FormularioValidador>();
builder.Services.AddScoped<IMatricesRiesgoService, MatricesRiesgoService>();
builder.Services.AddScoped<ICalculoConfiguracionRepository>(services =>
    EmptyCalculationConfigurationProxy.Create(
        services.GetRequiredService<OracleDbContext>(),
        services.GetRequiredService<IAuditoriaRepository>()));
builder.Services.AddScoped<ICalculoConfiguracionService, CalculoConfiguracionService>();
builder.Services.AddScoped<DbDrivenCalculationRuntimeFactory>();
builder.Services.AddScoped<VersionedCalculationRuntimeService>();
builder.Services.AddScoped<MatricesRiesgosAppService>();
builder.Services.AddScoped<IMatricesRiesgosAppService>(services => services.GetRequiredService<MatricesRiesgosAppService>());
builder.Services.AddSingleton<InstitutionalProjectionMapper>();
builder.Services.AddScoped<IInstitutionalProjectionReadRepository, InstitutionalProjectionReadRepository>();
builder.Services.AddScoped<IInstitutionalProjectionService, InstitutionalProjectionService>();

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/__e2e/readyz", async (OracleDbContext db, CancellationToken cancellationToken) =>
{
    await using var connection = db.CreateConnection();
    await connection.OpenAsync(cancellationToken);
    await using var command = new OracleCommand("SELECT 1 FROM DUAL", connection);
    _ = await command.ExecuteScalarAsync(cancellationToken);
    return Results.Ok(new { status = "ready" });
});
app.MapGet("/__e2e/fixture", () => Results.Ok(new { riskId, riskCode, versionId, versionCode = "BLOCK7_E2E", areaOnlyUserId, ugrOnlyUserId, bothUserId, neitherUserId, definition = JsonDocument.Parse(definition).RootElement.Clone() }));
app.MapGet("/__e2e/family-lifecycle-fixture", () => Results.Ok(new { lifecycleFamilyId, lifecycleFamilyCode, lifecycleVersionId, lifecycleEvaluationId, lifecycleEvidenceId, lifecycleEvidenceLinkId, lifecycleRiskId = riskId, lifecycleRiskCode = riskCode }));
app.MapGet("/__e2e/family-lifecycle-snapshot", async (OracleDbContext db, CancellationToken cancellationToken) =>
{
    await using var connection = db.CreateConnection();
    await connection.OpenAsync(cancellationToken);
    await using var command = new OracleCommand(@"
        SELECT f.FAM_ID,f.FAM_CODIGO,f.FAM_ACTIVO,f.FAM_ARCHIVADA,
               (SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO v WHERE v.VER_FAMILIA_ID=f.FAM_ID),
               (SELECT COUNT(*) FROM RL_MR_EVALUACIONES_RIESGO e JOIN RL_MR_VERSIONES_FORMULARIO v ON v.VER_ID=e.EVA_VERSION_ID WHERE v.VER_FAMILIA_ID=f.FAM_ID),
               (SELECT COUNT(*) FROM RL_MR_EVIDENCIAS_VINCULOS ev JOIN RL_MR_EVALUACIONES_RIESGO e ON ev.EVV_ENTIDAD_ID=e.EVA_ID AND ev.EVV_TIPO_ENTIDAD='EVALUACION' JOIN RL_MR_VERSIONES_FORMULARIO v ON v.VER_ID=e.EVA_VERSION_ID WHERE v.VER_FAMILIA_ID=f.FAM_ID),
               (SELECT COUNT(*) FROM RL_AUDITORIA a WHERE a.AUD_TABLA='RL_MR_FAMILIAS_FORMULARIO' AND a.AUD_REGISTRO_ID=TO_CHAR(f.FAM_ID))
          FROM RL_MR_FAMILIAS_FORMULARIO f WHERE f.FAM_ID=:id", connection) { BindByName = true };
    command.Parameters.Add(new OracleParameter("id", lifecycleFamilyId));
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    if (!await reader.ReadAsync(cancellationToken)) return Results.NotFound();
    return Results.Ok(new { familyId = reader.GetInt64(0), code = reader.GetString(1), active = reader.GetInt32(2) == 1, archived = reader.GetInt32(3) == 1, versions = reader.GetInt32(4), evaluations = reader.GetInt32(5), evidences = reader.GetInt32(6), audit = reader.GetInt32(7) });
});
app.MapGet("/__e2e/family-lifecycle-deep-snapshot", async (OracleDbContext db, CancellationToken cancellationToken) =>
{
    await using var connection = db.CreateConnection();
    await connection.OpenAsync(cancellationToken);

    async Task<List<Dictionary<string, object?>>> ReadAsync(string sql)
    {
        await using var command = new OracleCommand(sql, connection) { BindByName = true };
        command.Parameters.Add(new OracleParameter("id", lifecycleFamilyId));
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
            {
                object value = reader.GetValue(ordinal);
                row[reader.GetName(ordinal)] = value is OracleClob clob
                    ? ReadClobIntegrity(clob)
                    : value == DBNull.Value ? null : value.ToString();
            }
            rows.Add(row);
        }
        return rows;
    }

    var versions = await ReadAsync("SELECT VER_ID,VER_VERSION,VER_ESTADO,VER_VIGENTE,VER_HASH,VER_JSON FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_FAMILIA_ID=:id ORDER BY VER_ID");
    var evaluations = await ReadAsync("SELECT e.EVA_ID,e.EVA_RIESGO_ID,e.EVA_VERSION_ID,e.EVA_DATOS_JSON,e.EVA_CALCULOS_JSON FROM RL_MR_EVALUACIONES_RIESGO e JOIN RL_MR_VERSIONES_FORMULARIO v ON v.VER_ID=e.EVA_VERSION_ID WHERE v.VER_FAMILIA_ID=:id ORDER BY e.EVA_ID");
    var evidences = await ReadAsync("SELECT evi.EVI_ID,evi.EVI_HASH,evi.EVI_RUTA,evv.EVV_ID,evv.EVV_ENTIDAD_ID FROM RL_MR_EVIDENCIAS evi JOIN RL_MR_EVIDENCIAS_VINCULOS evv ON evv.EVV_EVIDENCIA_ID=evi.EVI_ID JOIN RL_MR_EVALUACIONES_RIESGO eva ON eva.EVA_ID=evv.EVV_ENTIDAD_ID JOIN RL_MR_VERSIONES_FORMULARIO v ON v.VER_ID=eva.EVA_VERSION_ID WHERE v.VER_FAMILIA_ID=:id ORDER BY evi.EVI_ID");
    async Task<List<Dictionary<string, object?>>> ReadAuditsAsync()
    {
        await using var command = new OracleCommand(@"
            SELECT AUD_ID,AUD_TABLA,AUD_REGISTRO_ID,AUD_ACCION,AUD_USR_ID,
                   TO_CHAR(AUD_FECHA,'YYYY-MM-DD HH24:MI:SS') AS AUD_FECHA,
                   AUD_DATOS_NVO
              FROM RL_AUDITORIA
             WHERE (AUD_TABLA='RL_MR_FAMILIAS_FORMULARIO' AND AUD_REGISTRO_ID=TO_CHAR(:id))
                OR (AUD_TABLA='RL_MR_VERSIONES_FORMULARIO' AND AUD_REGISTRO_ID IN (SELECT TO_CHAR(VER_ID) FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_FAMILIA_ID=:id))
                OR (AUD_TABLA='RL_MR_EVALUACIONES_RIESGO' AND AUD_REGISTRO_ID IN (SELECT TO_CHAR(e.EVA_ID) FROM RL_MR_EVALUACIONES_RIESGO e JOIN RL_MR_VERSIONES_FORMULARIO v ON v.VER_ID=e.EVA_VERSION_ID WHERE v.VER_FAMILIA_ID=:id))
             ORDER BY AUD_FECHA,AUD_ID", connection) { BindByName = true };
        command.Parameters.Add(new OracleParameter("id", lifecycleFamilyId));
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
            {
                string name = reader.GetName(ordinal);
                if (string.Equals(name, "AUD_DATOS_NVO", StringComparison.OrdinalIgnoreCase))
                {
                    (string? auditEvent, string? justification) = reader.IsDBNull(ordinal)
                        ? (null, null)
                        : ReadAuditJson(reader.GetOracleClob(ordinal));
                    row["AUD_EVENT"] = auditEvent;
                    row["AUD_JUSTIFICATION"] = justification;
                    continue;
                }

                object value = reader.GetValue(ordinal);
                row[name] = value == DBNull.Value ? null : value.ToString();
            }
            rows.Add(row);
        }
        return rows;
    }

    var audits = await ReadAuditsAsync();
    return Results.Ok(new { versions, evaluations, evidences, audits });
});
app.MapGet("/__e2e/family-lifecycle-clob-negative", async (OracleDbContext db, CancellationToken cancellationToken) =>
{
    string prefix = new('A', 4_100);
    string original = $"{{\"payload\":\"{prefix}\"}}";
    string changed = $"{{\"payload\":\"{new string('A', 4_099)}B\"}}";
    async Task<ClobIntegrity> ReadProbeAsync(OracleConnection connection, string payload)
    {
        await using var command = new OracleCommand("SELECT TO_CLOB(:payload) FROM DUAL", connection) { BindByName = true };
        command.Parameters.Add(new OracleParameter("payload", OracleDbType.Clob) { Value = payload });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("The disposable CLOB probe returned no row.");
        return ReadClobIntegrity(reader.GetOracleClob(0));
    }

    await using var connection = db.CreateConnection();
    await connection.OpenAsync(cancellationToken);
    ClobIntegrity before = await ReadProbeAsync(connection, original);
    ClobIntegrity after = await ReadProbeAsync(connection, changed);
    return Results.Ok(new { before, after, prefixLength = 4_000, prefixEqual = original[..4_000] == changed[..4_000], detectedAfterPrefix = before.Sha256 != after.Sha256 && before.CharLength == after.CharLength });
});
app.MapGet("/__e2e/risk-identities", () => Results.Ok(canonicalRisks.Select((risk, index) => new
{
    ordinal = risk.Ordinal,
    riskId = canonicalRiskIds[index],
    riskCode = risk.Code,
    riskName = risk.Name
})));
app.MapPost("/__e2e/seed-full-chain-calculations", async () =>
{
    if (!string.Equals(Environment.GetEnvironmentVariable("RL_BLOCK12_FULL_CHAIN_UI_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase))
        return Results.NotFound();
    if (!fullChainSeeded)
    {
        int[] projectedFields = [12, 13, 22, 23, 26, 27, 30, 31, 33, 34, 35, 36, 37, 38, .. Enumerable.Range(50, 20)];
        IReadOnlyDictionary<int, string> calculationKeys = ReadCalculationKeysByField();
        var formulaResults = workbook.EvaluateFormulaCells().ToDictionary(result => (result.RiskCode, result.FieldNumber));
        if (formulaResults.Count != 2006) throw new InvalidOperationException("The independent workbook formula source must provide all 2006 expected values.");
        await using var fullChainSeed = await OpenOracleWithRetryAsync(connectionString);
        long firstEvaluationId = riskId + 800_000_100;
        long lastEvaluationId = firstEvaluationId + canonicalRisks.Length - 1;
        await ExecuteAsync(fullChainSeed, "DELETE FROM RL_MR_CONTROLES_RIESGO WHERE CON_EVALUACION_ID BETWEEN :first AND :last", ("first", firstEvaluationId), ("last", lastEvaluationId));
        await ExecuteAsync(fullChainSeed, "DELETE FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID BETWEEN :first AND :last", ("first", firstEvaluationId), ("last", lastEvaluationId));
        await ExecuteAsync(fullChainSeed, "DELETE FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID BETWEEN :first AND :last", ("first", firstEvaluationId), ("last", lastEvaluationId));
        await ExecuteAsync(fullChainSeed, "DELETE FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID BETWEEN :first AND :last", ("first", firstEvaluationId), ("last", lastEvaluationId));
        fullChainUiFixtures.Clear();
        for (int index = 0; index < canonicalRisks.Length; index++)
        {
            var risk = canonicalRisks[index];
            long evaluationId = riskId + 800_000_100 + index;
            long flowId = riskId + 850_000_100 + index;
            var calculatedValues = new Dictionary<int, object?>();
            foreach (int fieldNumber in projectedFields)
            {
                WorkbookFormulaResult formula = formulaResults[(risk.Code, fieldNumber)];
                if (formula.IsError) throw new InvalidOperationException($"Independent workbook formula error at {risk.Code}/F{fieldNumber}.");
                calculatedValues[fieldNumber] = NoControlExpected(workbook, risk.Ordinal + 1, fieldNumber) ?? formula.ExpectedValue;
            }
            var calculations = calculatedValues.ToDictionary(pair => calculationKeys[pair.Key], pair => pair.Value, StringComparer.Ordinal);
            string calculationsJson = JsonSerializer.Serialize(calculations);
            string evaluationDataJson = BuildFullChainEvaluationDataJson();
            DateTime evaluatedAt = new DateTime(2026, 9, 1).AddMinutes(index);
            await ExecuteAsync(fullChainSeed,
                "INSERT INTO RL_MR_EVALUACIONES_RIESGO (EVA_ID,EVA_RIESGO_ID,EVA_VERSION_ID,EVA_DATOS_JSON,EVA_CALCULOS_JSON,EVA_FECHA_REGISTRO,EVA_USR_REGISTRO,EVA_VERSION_ROW,EVA_ACTIVO) VALUES (:id,:risk,:version,:data,:calculations,:eventDate,:usr,1,1)",
                ("id", evaluationId), ("risk", canonicalRiskIds[index]), ("version", versionId), ("data", evaluationDataJson), ("calculations", calculationsJson), ("eventDate", evaluatedAt), ("usr", userId));
            await ExecuteAsync(fullChainSeed,
                "INSERT INTO RL_MR_PROYECCIONES_EVALUACION (PROY_ID,PROY_EVALUACION_ID,PROY_CODIGO_RIESGO,PROY_AREA_PRINCIPAL,PROY_VRI,PROY_VRR,PROY_NIVEL_INHERENTE,PROY_NIVEL_RESIDUAL,PROY_RESPUESTA_RIESGO,PROY_ESTADO_EVALUACION,PROY_DUENO_RIESGO,PROY_FECHA_EVAL) VALUES (:id,:eva,:code,'Block12',3,1,'BAJO','NO SIGNIFICATIVO','Mitigar','APROBADA','Fixture',:eventDate)",
                ("id", evaluationId + 100_000_000), ("eva", evaluationId), ("code", risk.Code), ("eventDate", evaluatedAt));
            await ExecuteAsync(fullChainSeed,
                "INSERT INTO RL_MR_FLUJOS_EVALUACION VALUES (:id,:eva,'APROBADA','Block 12 disposable full-chain fixture',:usr,:eventDate)",
                ("id", flowId), ("eva", evaluationId), ("usr", userId), ("eventDate", evaluatedAt));
            (string Type, int DescriptionColumn)[] controlSources = [("PREVENTIVO", 20), ("DETECTIVO", 24), ("CORRECTIVO", 28)];
            for (int controlIndex = 0; controlIndex < controlSources.Length; controlIndex++)
            {
                (string controlType, int descriptionColumn) = controlSources[controlIndex];
                string description = workbook.GetText(risk.Ordinal + 1, descriptionColumn).Trim();
                if (description.Length == 0 || new[] { "NO HAY", "INEXISTENTE", "0", "0%" }.Contains(description, StringComparer.OrdinalIgnoreCase)) continue;
                await ExecuteAsync(fullChainSeed,
                    "INSERT INTO RL_MR_CONTROLES_RIESGO (CON_ID,CON_EVALUACION_ID,CON_TIPO,CON_DESCRIPCION,CON_AUTOMATIZACION,CON_ESTADO) VALUES (:id,:evaluation,:type,:description,'MANUAL','ACTIVO')",
                    ("id", evaluationId + 200_000_000 + index * controlSources.Length + controlIndex), ("evaluation", evaluationId), ("type", controlType), ("description", "Block 12 disposable canonical control fixture"));
            }
            fullChainUiFixtures.Add(new
            {
                ordinal = risk.Ordinal,
                riskId = canonicalRiskIds[index],
                evaluationId,
                riskCode = risk.Code,
                expected = calculatedValues
            });
        }
        fullChainSeeded = true;
    }
    return Results.Ok(fullChainUiFixtures);
});
app.MapGet("/__e2e/audit", async (OracleDbContext db, CancellationToken cancellationToken) =>
{
    await using var connection = db.CreateConnection();
    await connection.OpenAsync(cancellationToken);
    await using var command = new OracleCommand(@"SELECT AUD_TABLA, AUD_ACCION, COUNT(*)
        FROM RL_AUDITORIA
        WHERE AUD_USR_ID IN (:mainUser,:areaUser,:ugrUser,:bothUser,:neitherUser)
          AND AUD_MODULO='MatricesRiesgos'
        GROUP BY AUD_TABLA,AUD_ACCION ORDER BY AUD_TABLA,AUD_ACCION", connection) { BindByName = true };
    command.Parameters.Add(new OracleParameter("mainUser", userId));
    command.Parameters.Add(new OracleParameter("areaUser", areaOnlyUserId));
    command.Parameters.Add(new OracleParameter("ugrUser", ugrOnlyUserId));
    command.Parameters.Add(new OracleParameter("bothUser", bothUserId));
    command.Parameters.Add(new OracleParameter("neitherUser", neitherUserId));
    var entries = new List<object>();
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken)) entries.Add(new { table = reader.GetString(0), action = reader.GetString(1), count = Convert.ToInt32(reader.GetValue(2)) });
    return Results.Ok(entries);
});
try
{
    await app.RunAsync();
}
finally
{
    await using var cleanup = await OpenOracleWithRetryAsync(connectionString);
    await ExecuteAsync(cleanup, @"DELETE FROM RL_MR_EVIDENCIAS_VINCULOS
        WHERE (EVV_ENTIDAD_ID IN (
            SELECT CON_ID FROM RL_MR_CONTROLES_RIESGO
             WHERE CON_EVALUACION_ID IN (
                SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_RIESGO_ID=:riskId))
          AND EVV_TIPO_ENTIDAD='control')
           OR EVV_EVIDENCIA_ID IN (
            SELECT EVI_ID FROM RL_MR_EVIDENCIAS
             WHERE EVI_USR_CREACION=:userId AND EVI_NOMBRE_ARCHIVO LIKE 'block9-%')",
        ("riskId", riskId), ("userId", userId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_USR_CREACION=:userId AND EVI_NOMBRE_ARCHIVO LIKE 'block9-%'", ("userId", userId));
    await ExecuteAsync(cleanup, @"DELETE FROM RL_MR_ACTIVIDADES
        WHERE ACT_PLAN_ID IN (
            SELECT PLA_ID FROM RL_MR_PLANES WHERE PLA_EVALUACION_ID IN (
                SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_RIESGO_ID=:riskId))", ("riskId", riskId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_PLANES WHERE PLA_EVALUACION_ID IN (SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_RIESGO_ID=:riskId)", ("riskId", riskId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_CONTROLES_RIESGO WHERE CON_EVALUACION_ID IN (SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_RIESGO_ID=:riskId)", ("riskId", riskId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_SENALES_ALERTA WHERE ALE_EVALUACION_ID IN (SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_RIESGO_ID=:riskId)", ("riskId", riskId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_AUTOMONITOREO WHERE MON_EVALUACION_ID IN (SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_RIESGO_ID=:riskId)", ("riskId", riskId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID IN (SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_RIESGO_ID=:riskId)", ("riskId", riskId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID IN (SELECT EVA_ID FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_RIESGO_ID=:riskId)", ("riskId", riskId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_RIESGO_ID=:riskId", ("riskId", riskId));
    foreach (long id in canonicalRiskIds)
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_RIESGOS WHERE RIE_ID=:id", ("id", id));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_EVIDENCIAS_VINCULOS WHERE EVV_ID=:id", ("id", lifecycleEvidenceLinkId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_ID=:id", ("id", lifecycleEvidenceId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", lifecycleEvaluationId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID=:id", ("id", lifecycleVersionId));
    await ExecuteAsync(cleanup, "DELETE FROM RL_MR_FAMILIAS_FORMULARIO WHERE FAM_ID=:id", ("id", lifecycleFamilyId));
}

static string FindOfficialWorkbook()
{
    DirectoryInfo? directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null)
    {
        string candidate = Path.Combine(directory.FullName, "Matrices de Riesgos.xlsx");
        if (File.Exists(candidate)) return candidate;
        directory = directory.Parent;
    }

    throw new FileNotFoundException("The official risk workbook was not found from the repository working directory.");
}

static IReadOnlyDictionary<int, string> ReadCalculationKeysByField()
{
    DirectoryInfo? directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
    string root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found for the full-chain fixture.");
    string manifestPath = Path.Combine(root, "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_82_campos_manifest.json");
    using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
    return manifest.RootElement.EnumerateArray().ToDictionary(
        field => field.GetProperty("number").GetInt32(),
        field =>
        {
            string mapping = field.GetProperty("dbOrEntityMapping").GetString()!;
            int separator = mapping.LastIndexOf("->", StringComparison.Ordinal);
            return separator >= 0 ? mapping[(separator + 2)..].Trim() : field.GetProperty("canonicalKey").GetString()!;
    });
}

static string BuildFullChainEvaluationDataJson()
{
    DirectoryInfo? directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
    string root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found for the full-chain fixture.");
    string manifestPath = Path.Combine(root, "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_82_campos_manifest.json");
    using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
    var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    foreach (JsonElement field in manifest.RootElement.EnumerateArray())
    {
        int number = field.GetProperty("number").GetInt32();
        if (number is 1 or 2 or 8 or 9 or 12 or 13 or 20 or 22 or 23 or 24 or 26 or 27 or 28 or 30 or 31 or 33 or 34 or 35 or 36 or 37 or 38 or 40 or 41 or 42 or 43 or 44 or 45 or 46 or 47 or 48 or 49 or >= 50 and <= 82) continue;
        string mapping = field.GetProperty("dbOrEntityMapping").GetString()!;
        int separator = mapping.LastIndexOf("->", StringComparison.Ordinal);
        string key = separator >= 0 ? mapping[(separator + 2)..].Trim() : field.GetProperty("canonicalKey").GetString()!;
        values[key] = number switch { 5 => "GTIC", 21 or 25 or 29 => "Razonable", 39 => "Mitigar", _ => $"Fixture F{number:00}" };
    }
    values["tipo_riesgo"] = "GTIC";
    values["respuesta_riesgo"] = "Mitigar";
    values["amenazas_gtic"] = "Amenaza fixture";
    values["vulnerabilidades_gtic"] = "Vulnerabilidad fixture";
    values["activos_informacion_gtic"] = "Activo fixture";
    return JsonSerializer.Serialize(values);
}

static object? NoControlExpected(WorkbookFormulaOracle oracle, int row, int field)
{
    int descriptionColumn = field switch { 22 or 23 => 20, 26 or 27 => 24, 30 or 31 => 28, _ => 0 };
    if (descriptionColumn == 0) return null;
    string description = oracle.GetText(row, descriptionColumn).Trim();
    int scaleColumn = field switch { 22 or 23 => 21, 26 or 27 => 25, 30 or 31 => 29, _ => 0 };
    string scale = oracle.GetText(row, scaleColumn).Trim();
    bool absent = description.Length == 0 || new[] { "NO HAY", "INEXISTENTE", "0", "0%" }.Contains(description, StringComparer.OrdinalIgnoreCase);
    bool absentScale = scale.Length == 0 || scale.Equals("Inexistente", StringComparison.OrdinalIgnoreCase);
    return absent && absentScale ? 0m : null;
}

static async Task<OracleConnection> OpenOracleWithRetryAsync(string connectionString)
{
    OracleException? lastError = null;
    for (int attempt = 1; attempt <= 6; attempt++)
    {
        var connection = new OracleConnection(connectionString)
        {
            UseHourOffsetForUnsupportedTimezone = true
        };
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch (OracleException ex) when (attempt < 6)
        {
            lastError = ex;
            await connection.DisposeAsync();
            await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
        }
    }

    throw new InvalidOperationException("Disposable Oracle XE did not accept the Block 7 browser fixture connection.", lastError);
}

static string CreateDefinition(string ruleCode) => JsonSerializer.Serialize(new
{
    codigoFormulario = "BLOCK7_E2E",
    nombreFormulario = "Block 7 Oracle E2E",
    reglaCalculo = new { codigo = ruleCode, version = "1" },
    secciones = new[]
    {
        new
        {
            clave = "identificacion",
            titulo = "Identificación del riesgo",
            campos = new object[]
            {
                new { clave = "area_principal", etiqueta = "Área principal", tipo = "texto", obligatorio = false },
                new { clave = "dueno_riesgo", etiqueta = "Dueño del riesgo", tipo = "texto", obligatorio = false },
                new { clave = "amenazas_gtic", etiqueta = "Amenazas", tipo = "texto-largo", obligatorio = false },
                new { clave = "vulnerabilidades_gtic", etiqueta = "Vulnerabilidades", tipo = "texto-largo", obligatorio = false },
                new { clave = "activos_informacion_gtic", etiqueta = "Activos de información", tipo = "texto-largo", obligatorio = false },
                new { clave = "frecuencia_inherente", etiqueta = "Frecuencia inherente", tipo = "numero", obligatorio = true },
                new { clave = "impacto_inherente", etiqueta = "Impacto inherente", tipo = "numero", obligatorio = true },
                new { clave = "controles_preventivo", etiqueta = "Efectividad preventiva", tipo = "numero", obligatorio = true },
                new { clave = "controles_detectivo", etiqueta = "Efectividad detectiva", tipo = "numero", obligatorio = true },
                new { clave = "controles_correctivo", etiqueta = "Efectividad correctiva", tipo = "numero", obligatorio = true },
                new { clave = "frecuencia_residual", etiqueta = "Frecuencia residual", tipo = "numero", obligatorio = true },
                new { clave = "impacto_residual", etiqueta = "Impacto residual", tipo = "numero", obligatorio = true }
            }
        }
    }
});

static async Task EnsureFixtureAsync(OracleConnection connection)
{
    string[] ddl =
    [
        "CREATE TABLE RL_MR_RIESGOS (RIE_ID NUMBER(15) PRIMARY KEY,RIE_CODIGO VARCHAR2(30),RIE_NOMBRE VARCHAR2(250),RIE_DESCRIPCION VARCHAR2(2000),RIE_ACTIVO NUMBER(1),RIE_USR_CREACION NUMBER(10),RIE_FECHA_CREACION DATE)",
        "CREATE TABLE RL_MR_EVALUACIONES_RIESGO (EVA_ID NUMBER(15) PRIMARY KEY,EVA_RIESGO_ID NUMBER(15),EVA_VERSION_ID NUMBER(15),EVA_DATOS_JSON CLOB,EVA_CALCULOS_JSON CLOB,EVA_FECHA_REGISTRO DATE,EVA_USR_REGISTRO NUMBER(10),EVA_VERSION_ROW NUMBER(10),EVA_ACTIVO NUMBER(1))",
        "CREATE TABLE RL_MR_PROYECCIONES_EVALUACION (PROY_ID NUMBER(15),PROY_EVALUACION_ID NUMBER(15),PROY_CODIGO_RIESGO VARCHAR2(30),PROY_AREA_PRINCIPAL VARCHAR2(100),PROY_VRI NUMBER(8,2),PROY_VRR NUMBER(8,2),PROY_NIVEL_INHERENTE VARCHAR2(20),PROY_NIVEL_RESIDUAL VARCHAR2(20),PROY_RESPUESTA_RIESGO VARCHAR2(50),PROY_ESTADO_EVALUACION VARCHAR2(30),PROY_DUENO_RIESGO VARCHAR2(150),PROY_FECHA_EVAL DATE)",
        "CREATE TABLE RL_MR_FLUJOS_EVALUACION (FLU_ID NUMBER(15),FLU_EVALUACION_ID NUMBER(15),FLU_ESTADO VARCHAR2(30),FLU_MOTIVO VARCHAR2(1000),FLU_USR_ID NUMBER(10),FLU_FECHA DATE)",
        "CREATE TABLE RL_MR_CONTROLES_RIESGO (CON_ID NUMBER(15) PRIMARY KEY,CON_EVALUACION_ID NUMBER(15),CON_TIPO VARCHAR2(30),CON_DESCRIPCION VARCHAR2(500),CON_AUTOMATIZACION VARCHAR2(30),CON_ESTADO VARCHAR2(20),CON_ESTADO_MONITOREO VARCHAR2(30),CON_EFECTIVIDAD_MONITOREO NUMBER(5,2))",
        "CREATE TABLE RL_MR_PLANES (PLA_ID NUMBER(15) PRIMARY KEY,PLA_EVALUACION_ID NUMBER(15),PLA_DESCRIPCION VARCHAR2(500),PLA_AVANCE NUMBER(5,2),PLA_PRESUPUESTO NUMBER(15,2),PLA_FECHA_INICIO DATE,PLA_FECHA_FIN DATE,PLA_ESTADO VARCHAR2(30),PLA_MONITOREO_SEGUIMIENTO VARCHAR2(1000),PLA_RESPONSABLES VARCHAR2(1000),PLA_RECURSOS VARCHAR2(1000))",
        "CREATE TABLE RL_MR_ACTIVIDADES (ACT_ID NUMBER(15) PRIMARY KEY,ACT_PLAN_ID NUMBER(15),ACT_DESCRIPCION VARCHAR2(500),ACT_RESPONSABLE VARCHAR2(150),ACT_AVANCE NUMBER(5,2),ACT_FECHA_INICIO DATE,ACT_FECHA_FIN DATE,ACT_ESTADO VARCHAR2(30))",
        "CREATE TABLE RL_MR_SENALES_ALERTA (ALE_ID NUMBER(15) PRIMARY KEY,ALE_EVALUACION_ID NUMBER(15),ALE_CODIGO VARCHAR2(50),ALE_INDICADOR VARCHAR2(150),ALE_ESTADO VARCHAR2(30),ALE_FECHA_DISPARO DATE)",
        "CREATE TABLE RL_MR_AUTOMONITOREO (MON_ID NUMBER(15) PRIMARY KEY,MON_EVALUACION_ID NUMBER(15),MON_ESTADO_RIESGO VARCHAR2(30),MON_ESTADO_CONTR VARCHAR2(30),MON_RESULTADO VARCHAR2(1000),MON_USR_ID NUMBER(10),MON_OBSERVACIONES_AREA VARCHAR2(2000),MON_OBSERVACIONES_UGR VARCHAR2(2000),MON_FECHA DATE)",
        "CREATE TABLE RL_MR_EVIDENCIAS (EVI_ID NUMBER(15) PRIMARY KEY,EVI_NOMBRE_ARCHIVO VARCHAR2(255) NOT NULL,EVI_EXTENSION VARCHAR2(10) NOT NULL,EVI_TAMANO NUMBER(15) NOT NULL,EVI_HASH VARCHAR2(64) NOT NULL,EVI_RUTA VARCHAR2(500) NOT NULL,EVI_USR_CREACION NUMBER(10) NOT NULL,EVI_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL)",
        "CREATE TABLE RL_MR_EVIDENCIAS_VINCULOS (EVV_ID NUMBER(15) PRIMARY KEY,EVV_EVIDENCIA_ID NUMBER(15),EVV_TIPO_ENTIDAD VARCHAR2(30),EVV_ENTIDAD_ID NUMBER(15),EVV_USR_CREACION NUMBER(10),EVV_FECHA_CREACION DATE DEFAULT SYSDATE)",
        "CREATE TABLE RL_USUARIO_CAPACIDADES (UCP_USR_ID NUMBER(10),UCP_CAPACIDAD VARCHAR2(80),UCP_ACTIVO NUMBER(1))",
        "CREATE TABLE RL_USUARIOS (USR_ID NUMBER(10) PRIMARY KEY,USR_ACTIVO NUMBER(1),USR_NOMBRE VARCHAR2(120),USR_APELLIDO VARCHAR2(120),USR_EMAIL VARCHAR2(255))",
        "CREATE TABLE RL_MR_FAMILIAS_FORMULARIO (FAM_ID NUMBER(15) PRIMARY KEY,FAM_CODIGO VARCHAR2(80),FAM_NOMBRE VARCHAR2(250),FAM_DESCRIPCION VARCHAR2(2000),FAM_ACTIVO NUMBER(1),FAM_ARCHIVADA NUMBER(1),FAM_PREDETERMINADA NUMBER(1),FAM_FECHA_CREACION DATE,FAM_FECHA_ARCHIVADO DATE,FAM_USR_ARCHIVADO NUMBER(10),FAM_MOTIVO_ARCHIVADO VARCHAR2(500))",
        "CREATE TABLE RL_MR_VERSIONES_FORMULARIO (VER_ID NUMBER(15) PRIMARY KEY,VER_FAMILIA_ID NUMBER(15),VER_CODIGO VARCHAR2(80),VER_VERSION NUMBER(10),VER_JSON CLOB,VER_HASH VARCHAR2(64),VER_ESTADO VARCHAR2(30),VER_VIGENTE NUMBER(1),VER_FECHA_INICIO DATE,VER_FECHA_FIN DATE,VER_FECHA_CREACION DATE,VER_USR_CREACION NUMBER(10))",
        "CREATE TABLE RL_MR_REGLAS_CALCULO (REG_ID NUMBER(15) PRIMARY KEY,REG_CODIGO VARCHAR2(80),REG_VERSION VARCHAR2(20),REG_NOMBRE VARCHAR2(250),REG_ALGORITMO_ID VARCHAR2(100),REG_ACTIVA NUMBER(1))",
        "CREATE TABLE RL_AUDITORIA (AUD_ID NUMBER(15) PRIMARY KEY,AUD_TABLA VARCHAR2(128),AUD_REGISTRO_ID VARCHAR2(100),AUD_ACCION VARCHAR2(80),AUD_DATOS_ANT CLOB,AUD_DATOS_NVO CLOB,AUD_USR_ID NUMBER(10),AUD_USR_EMAIL VARCHAR2(255),AUD_IP VARCHAR2(64),AUD_FECHA DATE,AUD_MODULO VARCHAR2(80))",
        "CREATE TABLE RL_MR_FORMULA_USOS (FUS_ID NUMBER(15) PRIMARY KEY,FUS_VERSION_FORMULARIO_ID NUMBER(15),FUS_CAMPO_CLAVE VARCHAR2(100),FUS_FORMULA_VERSION_ID NUMBER(15),FUS_FECHA_CREACION DATE,FUS_USR_CREACION NUMBER(10))",
        "CREATE TABLE RL_MR_AUDITORIA (AUD_ID NUMBER(15) PRIMARY KEY,AUD_TABLA VARCHAR2(128),AUD_REGISTRO_ID VARCHAR2(100),AUD_ACCION VARCHAR2(80),AUD_DATOS_ANT CLOB,AUD_DATOS_NVO CLOB,AUD_USR_ID NUMBER(10),AUD_USR_EMAIL VARCHAR2(255),AUD_IP VARCHAR2(64),AUD_FECHA DATE,AUD_MODULO VARCHAR2(80))"
    ];
    foreach (string sql in ddl)
        try { await ExecuteAsync(connection, sql); } catch (OracleException exception) when (exception.Number == 955) { }
    foreach (string sql in new[]
    {
        "ALTER TABLE RL_MR_FAMILIAS_FORMULARIO ADD (FAM_PREDETERMINADA NUMBER(1))",
        "ALTER TABLE RL_MR_FAMILIAS_FORMULARIO ADD (FAM_FECHA_ARCHIVADO DATE)",
        "ALTER TABLE RL_MR_FAMILIAS_FORMULARIO ADD (FAM_USR_ARCHIVADO NUMBER(10))",
        "ALTER TABLE RL_MR_FAMILIAS_FORMULARIO ADD (FAM_MOTIVO_ARCHIVADO VARCHAR2(500))"
    })
        try { await ExecuteAsync(connection, sql); } catch (OracleException exception) when (exception.Number == 1430) { }
    foreach (string sequence in new[]
    {
        "SEQ_RL_MR_EVALUACIONES", "SEQ_RL_MR_PROYECCIONES", "SEQ_RL_MR_FLUJOS", "SEQ_RL_AUDITORIA",
        "SEQ_RL_MR_CONTROLES", "SEQ_RL_MR_PLANES", "SEQ_RL_MR_ACTIVIDADES", "SEQ_RL_MR_SENALES",
        "SEQ_RL_MR_AUTOMONITOREO", "SEQ_RL_MR_EVIDENCIAS", "SEQ_RL_MR_EVI_VINCULOS",
        "SEQ_RL_MR_FORMULA_USOS"
    })
        try { await ExecuteAsync(connection, $"CREATE SEQUENCE {sequence} START WITH 200000000 INCREMENT BY 1 NOCACHE"); } catch (OracleException exception) when (exception.Number == 955) { }
}

static ClobIntegrity ReadClobIntegrity(OracleClob clob)
{
    if (clob.IsNull) return new ClobIntegrity("NULL", 0, null);
    string value = clob.Value;
    if (value.Length == 0) return new ClobIntegrity("EMPTY", 0, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant());
    return new ClobIntegrity("VALUE", value.Length, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant());
}

static (string? AuditEvent, string? Justification) ReadAuditJson(OracleClob clob)
{
    if (clob.IsNull) return (null, null);

    string json = clob.Value;
    if (json.Length == 0) throw new InvalidOperationException("An audit CLOB was empty and could not be interpreted as JSON.");

    using JsonDocument document = JsonDocument.Parse(json);
    if (document.RootElement.ValueKind != JsonValueKind.Object)
        throw new InvalidOperationException("An audit CLOB did not contain a JSON object.");

    return (ReadAuditStringProperty(document.RootElement, "Evento"), ReadAuditStringProperty(document.RootElement, "Justification"));
}

static string? ReadAuditStringProperty(JsonElement root, string propertyName)
{
    if (!root.TryGetProperty(propertyName, out JsonElement property)) return null;
    if (property.ValueKind is JsonValueKind.Null) return null;
    if (property.ValueKind is not JsonValueKind.String)
        throw new InvalidOperationException($"Audit JSON property '{propertyName}' must be a JSON string.");
    return property.GetString();
}

static async Task ExecuteAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
{
    await using var command = new OracleCommand(sql, connection) { BindByName = true };
    foreach ((string name, object value) in parameters) command.Parameters.Add(new OracleParameter(name, value));
    await command.ExecuteNonQueryAsync();
}

public sealed class E2EAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        bool block9Enabled = string.Equals(Environment.GetEnvironmentVariable("RL_BLOCK9_REAL_E2E_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase);
        if (block9Enabled && string.Equals(Request.Headers["X-E2E-Unauthenticated"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());
        string userId = "987654";
        if (block9Enabled
            && long.TryParse(Request.Headers["X-E2E-User"].FirstOrDefault(), out long requestedUserId)
            && requestedUserId > 0)
            userId = requestedUserId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string modules = block9Enabled && !string.IsNullOrWhiteSpace(Request.Headers["X-E2E-Modules"].FirstOrDefault())
            ? Request.Headers["X-E2E-Modules"].FirstOrDefault()!
            : "10";
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, SystemRoles.Administrador), new Claim("modulos", modules)], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

public class EmptyCalculationConfigurationProxy : DispatchProxy
{
    private DisposableCalculationConfigurationMutationHandler? _mutationHandler;

    public static ICalculoConfiguracionRepository Create(OracleDbContext db, IAuditoriaRepository auditoria)
    {
        var proxy = DispatchProxy.Create<ICalculoConfiguracionRepository, EmptyCalculationConfigurationProxy>();
        ((EmptyCalculationConfigurationProxy)(object)proxy)._mutationHandler =
            new DisposableCalculationConfigurationMutationHandler(db, auditoria);
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        switch (targetMethod?.Name)
        {
            case nameof(ICalculoConfiguracionRepository.ListarFormulasAsync):
                return Task.FromResult<IReadOnlyList<FormulaDto>>(Array.Empty<FormulaDto>());
            case nameof(ICalculoConfiguracionRepository.ListarFuncionesAsync):
                return Task.FromResult<IReadOnlyList<FuncionDto>>(Array.Empty<FuncionDto>());
            case nameof(ICalculoConfiguracionRepository.ListarParametrosAsync):
                return Task.FromResult<IReadOnlyList<ParametroDto>>(Array.Empty<ParametroDto>());
            case nameof(ICalculoConfiguracionRepository.ListarFormulaBindingsPorVersionFormularioAsync):
                return Task.FromResult<IReadOnlyList<FormulaBindingDto>>(Array.Empty<FormulaBindingDto>());
            case nameof(ICalculoConfiguracionRepository.ReemplazarFormulaUsosAsync):
                if (_mutationHandler is null || args is null || args.Length < 4)
                    throw new InvalidOperationException("The disposable E2E calculation configuration handler was not initialized.");
                return _mutationHandler.ReemplazarFormulaUsosAsync(
                    (long)args[0]!,
                    (IReadOnlyList<CrearFormulaUsoDto>)args[1]!,
                    (long)args[2]!,
                    (string?)args[3]);
        }
        throw new NotSupportedException($"The disposable E2E schema has no calculation bindings: {targetMethod?.Name}.");
    }
}

sealed class DisposableCalculationConfigurationMutationHandler
{
    private const string Module = "MATRICES_RIESGO";
    private readonly OracleDbContext _db;
    private readonly IAuditoriaRepository _auditoria;

    public DisposableCalculationConfigurationMutationHandler(OracleDbContext db, IAuditoriaRepository auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<bool> ReemplazarFormulaUsosAsync(long versionFormularioId, IReadOnlyList<CrearFormulaUsoDto> usos, long usuarioId, string? ip)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();
        try
        {
            string? estado;
            await using (var command = CreateCommand(connection, transaction,
                "SELECT VER_ESTADO FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID=:id FOR UPDATE"))
            {
                command.Parameters.Add(new OracleParameter("id", versionFormularioId));
                estado = (await command.ExecuteScalarAsync())?.ToString();
            }

            if (estado is null)
                throw new KeyNotFoundException("La versión de formulario no existe.");
            if (!estado.Equals("DRAFT", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Los usos de fórmula solo pueden modificarse en una versión de formulario en borrador.");

            var existingIds = new List<long>();
            await using (var command = CreateCommand(connection, transaction,
                "SELECT FUS_ID FROM RL_MR_FORMULA_USOS WHERE FUS_VERSION_FORMULARIO_ID=:id FOR UPDATE"))
            {
                command.Parameters.Add(new OracleParameter("id", versionFormularioId));
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) existingIds.Add(reader.GetInt64(0));
            }

            await using (var command = CreateCommand(connection, transaction,
                "DELETE FROM RL_MR_FORMULA_USOS WHERE FUS_VERSION_FORMULARIO_ID=:id"))
            {
                command.Parameters.Add(new OracleParameter("id", versionFormularioId));
                await command.ExecuteNonQueryAsync();
            }

            foreach (long existingId in existingIds)
                await _auditoria.RegistrarAsync(connection, transaction, "RL_MR_FORMULA_USOS", existingId.ToString(), "DELETE", null,
                    JsonSerializer.Serialize(new { versionFormularioId, state = "REMOVED" }), usuarioId, null, ip, Module);

            foreach (var uso in usos)
            {
                long id;
                await using (var command = CreateCommand(connection, transaction,
                    "SELECT SEQ_RL_MR_FORMULA_USOS.NEXTVAL FROM DUAL"))
                    id = Convert.ToInt64(await command.ExecuteScalarAsync());

                await using (var command = CreateCommand(connection, transaction, @"
                    INSERT INTO RL_MR_FORMULA_USOS
                        (FUS_ID,FUS_VERSION_FORMULARIO_ID,FUS_CAMPO_CLAVE,FUS_FORMULA_VERSION_ID,FUS_FECHA_CREACION,FUS_USR_CREACION)
                    VALUES (:id,:versionFormularioId,:campoClave,:formulaVersionId,SYSDATE,:usuarioId)"))
                {
                    command.Parameters.Add(new OracleParameter("id", id));
                    command.Parameters.Add(new OracleParameter("versionFormularioId", versionFormularioId));
                    command.Parameters.Add(new OracleParameter("campoClave", uso.CampoClave.Trim()));
                    command.Parameters.Add(new OracleParameter("formulaVersionId", uso.FormulaVersionId));
                    command.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
                    await command.ExecuteNonQueryAsync();
                }

                await _auditoria.RegistrarAsync(connection, transaction, "RL_MR_FORMULA_USOS", id.ToString(), "INSERT", null,
                    JsonSerializer.Serialize(new { campoClave = uso.CampoClave.Trim(), state = "ACTIVE" }), usuarioId, null, ip, Module);
            }

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static OracleCommand CreateCommand(OracleConnection connection, OracleTransaction transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.BindByName = true;
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }
}

sealed class E2EConfiguracionService : IConfiguracionService
{
    public Task<ConfigSistema?> ObtenerConfigSistemaAsync() => Task.FromResult<ConfigSistema?>(null);
    public Task<List<LoginSlide>> ObtenerSlidesAsync() => Task.FromResult(new List<LoginSlide>());
    public Task<List<LoginSlide>> ObtenerTodosSlidesAsync() => Task.FromResult(new List<LoginSlide>());
    public Task<bool> GuardarConfigSistemaAsync(ConfigSistema config, long usuarioId, string? ip) => Task.FromResult(false);
    public Task<bool> CrearSlideAsync(LoginSlide slide, long usuarioId, string? ip) => Task.FromResult(false);
    public Task<bool> ActualizarSlideAsync(int id, LoginSlide slide, long usuarioId, string? ip) => Task.FromResult(false);
    public Task<bool> EliminarSlideAsync(int id, long usuarioId, string? ip) => Task.FromResult(false);
    public Task RegistrarCargaImagenAsync(string nombreOriginal, string nombreGuardado, string url, string tipoMime, long tamanioBytes, long usuarioId, string? ip) => Task.CompletedTask;
}

sealed class E2ECatalogoService : ICatalogoService
{
    public Task<List<KeyValuePair<int, string>>> ObtenerRolesAsync() => Task.FromResult(new List<KeyValuePair<int, string>>());
    public Task<List<KeyValuePair<int, string>>> ObtenerDominiosAsync() => Task.FromResult(new List<KeyValuePair<int, string>>());
    public Task<List<CatalogoModulo>> ObtenerModulosAsync() => Task.FromResult(new List<CatalogoModulo>());
    public Task<List<CatalogoMatricesCatalogDto>> ListarMatricesAsync(bool incluirInactivos) => Task.FromResult(new List<CatalogoMatricesCatalogDto>());
    public Task<long> CrearCatalogoMatricesAsync(CrearCatalogoMatricesDto dto) => Task.FromResult(0L);
    public Task<bool> ActualizarCatalogoMatricesAsync(long id, ActualizarCatalogoMatricesDto dto) => Task.FromResult(false);
    public Task<long> CrearElementoCatalogoMatricesAsync(long catalogoId, CrearElementoCatalogoMatricesDto dto) => Task.FromResult(0L);
    public Task<bool> ActualizarElementoCatalogoMatricesAsync(long id, ActualizarElementoCatalogoMatricesDto dto) => Task.FromResult(false);
}

public sealed record ClobIntegrity(string State, int CharLength, string? Sha256);
