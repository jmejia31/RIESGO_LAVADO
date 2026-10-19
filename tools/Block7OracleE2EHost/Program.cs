using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Block5WorkbookFormulaOracle;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Database;

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
string riskCode = canonicalRisks[0].Code;
string ruleCode = $"B7-E2E-RULE-{stamp}";
string definition = CreateDefinition(ruleCode);
for (int index = 0; index < canonicalRisks.Length; index++)
{
    var risk = canonicalRisks[index];
    await ExecuteAsync(oracle, "INSERT INTO RL_MR_RIESGOS (RIE_ID,RIE_CODIGO,RIE_NOMBRE,RIE_DESCRIPCION,RIE_ACTIVO,RIE_USR_CREACION,RIE_FECHA_CREACION) VALUES (:id,:code,:name,'Block 12 disposable canonical identity',1,:usr,SYSDATE)",
        ("id", canonicalRiskIds[index]), ("code", risk.Code), ("name", risk.Name), ("usr", userId));
}
await ExecuteAsync(oracle, "INSERT INTO RL_MR_VERSIONES_FORMULARIO (VER_ID,VER_FAMILIA_ID,VER_CODIGO,VER_VERSION,VER_JSON,VER_HASH,VER_ESTADO,VER_VIGENTE,VER_FECHA_INICIO,VER_FECHA_FIN,VER_FECHA_CREACION,VER_USR_CREACION) VALUES (:id,1,'BLOCK7_E2E',1,:json,RPAD('B',64,'B'),'PUBLISHED',1,SYSDATE,NULL,SYSDATE,:usr)", ("id", versionId), ("json", definition), ("usr", userId));
var fullChainUiFixtures = new List<object>();
bool fullChainSeeded = false;
await ExecuteAsync(oracle, "INSERT INTO RL_MR_REGLAS_CALCULO (REG_ID,REG_CODIGO,REG_VERSION,REG_NOMBRE,REG_ALGORITMO_ID,REG_ACTIVA) VALUES (:id,:code,'1','Disposable browser rule','BLOCK7_LEGACY_TEST',1)", ("id", versionId), ("code", ruleCode));
foreach (long id in new[] { userId, areaOnlyUserId, ugrOnlyUserId, bothUserId, neitherUserId })
    await ExecuteAsync(oracle, "INSERT INTO RL_USUARIOS (USR_ID,USR_ACTIVO) VALUES (:id,1)", ("id", id));
await ExecuteAsync(oracle, "INSERT INTO RL_USUARIO_CAPACIDADES (UCP_USR_ID,UCP_CAPACIDAD,UCP_ACTIVO) VALUES (:id,'MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR',1)", ("id", areaOnlyUserId));
await ExecuteAsync(oracle, "INSERT INTO RL_USUARIO_CAPACIDADES (UCP_USR_ID,UCP_CAPACIDAD,UCP_ACTIVO) VALUES (:id,'MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR',1)", ("id", ugrOnlyUserId));
foreach (string capability in new[] { "MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR", "MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR" })
{
    await ExecuteAsync(oracle, "INSERT INTO RL_USUARIO_CAPACIDADES (UCP_USR_ID,UCP_CAPACIDAD,UCP_ACTIVO) VALUES (:id,:capability,1)", ("id", bothUserId), ("capability", capability));
    await ExecuteAsync(oracle, "INSERT INTO RL_USUARIO_CAPACIDADES (UCP_USR_ID,UCP_CAPACIDAD,UCP_ACTIVO) VALUES (:id,:capability,1)", ("id", userId), ("capability", capability));
}
await oracle.CloseAsync();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5043");
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins("http://127.0.0.1:4201").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication("Block7E2E")
    .AddScheme<AuthenticationSchemeOptions, E2EAuthenticationHandler>("Block7E2E", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers().AddApplicationPart(typeof(MatricesRiesgosController).Assembly);
builder.Services.AddSingleton(new OracleDbContext(connectionString));
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<IMatricesRiesgosRepository, MatricesRiesgosRepository>();
builder.Services.AddScoped<IMatricesRiesgosGestionRepository, MatricesRiesgosGestionRepository>();
builder.Services.AddScoped<IMatricesRiesgosGestionService, MatricesRiesgosGestionService>();
builder.Services.AddScoped<IMatricesRiesgosMitigacionRepository, MatricesRiesgosMitigacionRepository>();
builder.Services.AddScoped<IMatricesRiesgosMitigacionService, MatricesRiesgosMitigacionService>();
builder.Services.AddScoped<IMatricesRiesgosMonitoreoRepository, MatricesRiesgosMonitoreoRepository>();
builder.Services.AddScoped<IMatricesRiesgosMonitoreoService, MatricesRiesgosMonitoreoService>();
builder.Services.AddScoped<IFormularioValidador, FormularioValidador>();
builder.Services.AddScoped<IMatricesRiesgoService, MatricesRiesgoService>();
builder.Services.AddScoped(_ => DispatchProxy.Create<ICalculoConfiguracionRepository, EmptyCalculationConfigurationProxy>());
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
        "CREATE TABLE RL_USUARIOS (USR_ID NUMBER(10) PRIMARY KEY,USR_ACTIVO NUMBER(1))",
        "CREATE TABLE RL_MR_VERSIONES_FORMULARIO (VER_ID NUMBER(15) PRIMARY KEY,VER_FAMILIA_ID NUMBER(15),VER_CODIGO VARCHAR2(80),VER_VERSION NUMBER(10),VER_JSON CLOB,VER_HASH VARCHAR2(64),VER_ESTADO VARCHAR2(30),VER_VIGENTE NUMBER(1),VER_FECHA_INICIO DATE,VER_FECHA_FIN DATE,VER_FECHA_CREACION DATE,VER_USR_CREACION NUMBER(10))",
        "CREATE TABLE RL_MR_REGLAS_CALCULO (REG_ID NUMBER(15) PRIMARY KEY,REG_CODIGO VARCHAR2(80),REG_VERSION VARCHAR2(20),REG_NOMBRE VARCHAR2(250),REG_ALGORITMO_ID VARCHAR2(100),REG_ACTIVA NUMBER(1))",
        "CREATE TABLE RL_AUDITORIA (AUD_ID NUMBER(15) PRIMARY KEY,AUD_TABLA VARCHAR2(128),AUD_REGISTRO_ID VARCHAR2(100),AUD_ACCION VARCHAR2(80),AUD_DATOS_ANT CLOB,AUD_DATOS_NVO CLOB,AUD_USR_ID NUMBER(10),AUD_USR_EMAIL VARCHAR2(255),AUD_IP VARCHAR2(64),AUD_FECHA DATE,AUD_MODULO VARCHAR2(80))"
    ];
    foreach (string sql in ddl)
        try { await ExecuteAsync(connection, sql); } catch (OracleException exception) when (exception.Number == 955) { }
    foreach (string sequence in new[]
    {
        "SEQ_RL_MR_EVALUACIONES", "SEQ_RL_MR_PROYECCIONES", "SEQ_RL_MR_FLUJOS", "SEQ_RL_AUDITORIA",
        "SEQ_RL_MR_CONTROLES", "SEQ_RL_MR_PLANES", "SEQ_RL_MR_ACTIVIDADES", "SEQ_RL_MR_SENALES",
        "SEQ_RL_MR_AUTOMONITOREO", "SEQ_RL_MR_EVIDENCIAS", "SEQ_RL_MR_EVI_VINCULOS"
    })
        try { await ExecuteAsync(connection, $"CREATE SEQUENCE {sequence} START WITH 200000000 INCREMENT BY 1 NOCACHE"); } catch (OracleException exception) when (exception.Number == 955) { }
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
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId), new Claim("modulos", modules)], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

public class EmptyCalculationConfigurationProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod?.Name == nameof(ICalculoConfiguracionRepository.ListarFormulaBindingsPorVersionFormularioAsync))
            return Task.FromResult<IReadOnlyList<FormulaBindingDto>>(Array.Empty<FormulaBindingDto>());
        throw new NotSupportedException($"The disposable E2E schema has no calculation bindings: {targetMethod?.Name}.");
    }
}
