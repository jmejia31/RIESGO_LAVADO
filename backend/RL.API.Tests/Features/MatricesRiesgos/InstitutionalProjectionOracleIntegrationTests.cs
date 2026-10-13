using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.MatricesRiesgos;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Database;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

/// <summary>Opt-in real Oracle integration test. CI runs it against a disposable Oracle XE service.</summary>
public sealed class InstitutionalProjectionOracleIntegrationTests
{
    [Fact]
    [Trait("Category", "Block6OracleIntegration")]
    public async Task GetProjection_ReadsOneOracleSnapshotAndSerializesAligned82Fields()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RL_BLOCK6_ORACLE_INTEGRATION_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase)) return;
        string connectionString = Environment.GetEnvironmentVariable("BLOCK6_ORACLE_CONNECTION_STRING")
            ?? throw new InvalidOperationException("BLOCK6_ORACLE_CONNECTION_STRING is required for the isolated Block 6 Oracle test.");
        await using var connection = CreateConnection(connectionString);
        await OpenWithRetryAsync(connection);
        await EnsureFixtureTablesAsync(connection);
        long stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1_000_000_000;
        long riskId=stamp+100, evaluationId=stamp+200, planA=stamp+300, planB=stamp+301, controlA=stamp+400, controlB=stamp+401, controlC=stamp+402;
        long activityA=stamp+500, activityB=stamp+501, evidenceLate=stamp+600, evidenceEarly=stamp+601, evidenceOther=stamp+602;
        const long userId = 987654;
        DateTime baseDate = new(2026, 1, 1);
        try
        {
            string answers = BuildAnswersJson();
            string calculations = BuildCalculationsJson();
            await ExecuteAsync(connection, "INSERT INTO RL_MR_RIESGOS VALUES (:id,'ROP-CUMP-59','Fixture','Fixture risk',1,:usr,SYSDATE)", ("id",riskId),("usr",userId));
            await ExecuteAsync(connection, "INSERT INTO RL_MR_EVALUACIONES_RIESGO VALUES (:id,:risk,1,:answers,:calc,:fixtureDate,:usr,1,1)", ("id",evaluationId),("risk",riskId),("answers",answers),("calc",calculations),("fixtureDate",baseDate),("usr",userId));
            // The legacy summary columns are VARCHAR2(20); canonical display labels come from EVA_CALCULOS_JSON.
            await ExecuteAsync(connection, "INSERT INTO RL_MR_PROYECCIONES_EVALUACION VALUES (:id,:eva,'ROP-CUMP-59','Fixture Area',3,1,'BAJO','NO SIGNIFICATIVO','Mitigar','BORRADOR','Owner',:fixtureDate)", ("id",stamp+201),("eva",evaluationId),("fixtureDate",baseDate));
            foreach (long id in new[] { controlA, controlB, controlC })
            {
                string type = id == controlC ? "DETECTIVO" : "PREVENTIVO";
                await ExecuteAsync(connection, "INSERT INTO RL_MR_CONTROLES_RIESGO VALUES (:id,:eva,:type,:description,'MANUAL','ACTIVO',:monState,85)", ("id",id),("eva",evaluationId),("type",type),("description",$"Control {id}"),("monState","VIGENTE"));
            }
            await ExecuteAsync(connection, "INSERT INTO RL_MR_PLANES VALUES (:id,:eva,'Plan A',0,100,:startDate,:finishDate,'PENDIENTE','Seguimiento A','Responsable A','Recursos A')", ("id",planA),("eva",evaluationId),("startDate",baseDate),("finishDate",baseDate.AddMonths(1)));
            await ExecuteAsync(connection, "INSERT INTO RL_MR_PLANES VALUES (:id,:eva,'Plan B',0,200,:startDate,:finishDate,'PENDIENTE','Seguimiento B',NULL,'Recursos B')", ("id",planB),("eva",evaluationId),("startDate",baseDate.AddMonths(2)),("finishDate",baseDate.AddMonths(3)));
            await ExecuteAsync(connection, "INSERT INTO RL_MR_ACTIVIDADES VALUES (:id,:plan,'Actividad A','Persona A',0,:startDate,:finishDate,'PENDIENTE')", ("id",activityA),("plan",planA),("startDate",baseDate),("finishDate",baseDate.AddDays(10)));
            await ExecuteAsync(connection, "INSERT INTO RL_MR_ACTIVIDADES VALUES (:id,:plan,'Actividad B','Persona B',0,:startDate,:finishDate,'PENDIENTE')", ("id",activityB),("plan",planB),("startDate",baseDate.AddMonths(2)),("finishDate",baseDate.AddMonths(2).AddDays(10)));
            await ExecuteAsync(connection, "INSERT INTO RL_MR_SENALES_ALERTA VALUES (:id,:eva,'ALERTA','Indicador fixture','ACTIVO',:fixtureDate)", ("id",stamp+550),("eva",evaluationId),("fixtureDate",baseDate));
            await ExecuteAsync(connection, "INSERT INTO RL_MR_AUTOMONITOREO VALUES (:id,:eva,'ALTO','VIGENTE','Fixture',:usr,'Area obs','UGR obs',:fixtureDate)", ("id",stamp+560),("eva",evaluationId),("usr",userId),("fixtureDate",baseDate));
            await InsertEvidenceAsync(connection,evidenceLate,"late-id-first",baseDate.AddDays(2),controlA);
            await InsertEvidenceAsync(connection,evidenceEarly,"early-id-later",baseDate.AddDays(1),controlA);
            await InsertEvidenceAsync(connection,evidenceOther,"other-control",baseDate,controlB);

            // Characterize the same Oracle snapshot transaction under a concurrent committed update.
            await using (var readerConnection = CreateConnection(connectionString))
            await using (var writerConnection = CreateConnection(connectionString))
            {
                await readerConnection.OpenAsync();
                await writerConnection.OpenAsync();
                await using OracleTransaction readerTransaction = readerConnection.BeginTransaction(System.Data.IsolationLevel.Serializable);
                string before = await ReadRiskNameAsync(readerConnection,readerTransaction,riskId);
                await using (OracleTransaction writerTransaction = writerConnection.BeginTransaction())
                {
                    await using var update = new OracleCommand("UPDATE RL_MR_RIESGOS SET RIE_NOMBRE='Concurrent mutation' WHERE RIE_ID=:id",writerConnection){BindByName=true,Transaction=writerTransaction};
                    update.Parameters.Add(new OracleParameter("id",riskId));
                    Assert.Equal(1,await update.ExecuteNonQueryAsync());
                    await writerTransaction.CommitAsync();
                }
                try { Assert.Equal(before,await ReadRiskNameAsync(readerConnection,readerTransaction,riskId)); }
                finally { await readerTransaction.RollbackAsync(); }
                await ExecuteAsync(writerConnection,"UPDATE RL_MR_RIESGOS SET RIE_NOMBRE='Fixture' WHERE RIE_ID=:id",("id",riskId));
            }

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            builder.Services.AddAuthentication("Block6Integration")
                .AddScheme<AuthenticationSchemeOptions, Block6IntegrationAuthenticationHandler>("Block6Integration", _ => { });
            builder.Services.AddAuthorization();
            builder.Services.AddControllers().AddApplicationPart(typeof(InstitutionalProjectionController).Assembly);
            builder.Services.AddSingleton(new OracleDbContext(connectionString));
            builder.Services.AddSingleton<InstitutionalProjectionMapper>();
            builder.Services.AddScoped<IInstitutionalProjectionReadRepository, InstitutionalProjectionReadRepository>();
            builder.Services.AddScoped<IInstitutionalProjectionService, InstitutionalProjectionService>();
            await using WebApplication app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();
            string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address) };
            using HttpResponseMessage httpResponse = await http.GetAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}/proyeccion-institucional");
            Assert.Equal(System.Net.HttpStatusCode.OK,httpResponse.StatusCode);
            string serialized = await httpResponse.Content.ReadAsStringAsync();
            using JsonDocument json = JsonDocument.Parse(serialized);
            JsonElement fields = json.RootElement.GetProperty("datos").GetProperty("fields");
            Assert.Equal(82,fields.GetArrayLength());
            Assert.Equal(Enumerable.Range(1,82),fields.EnumerateArray().Select(field=>field.GetProperty("fieldNumber").GetInt32()));
            Assert.Equal("Riesgo bajo", fields[12].GetProperty("value").GetString());
            Assert.Equal("Riesgo no significativo", fields[37].GetProperty("value").GetString());
            foreach (int fieldNumber in new[] { 40,44,45,46,47,48,49 })
            {
                JsonElement values = fields.EnumerateArray().Single(field=>field.GetProperty("fieldNumber").GetInt32()==fieldNumber).GetProperty("value");
                Assert.Equal(new[] { 1,2 },values.EnumerateArray().Select(value=>value.GetProperty("planOrdinalIndex").GetInt32()));
            }
            JsonElement responsible = fields[44].GetProperty("value");
            Assert.Equal("Responsable A",responsible[0].GetProperty("value").GetString());
            Assert.Equal("LEGIT_BLANK",responsible[1].GetProperty("state").GetString());
            JsonElement monitoredEvidence = fields[73].GetProperty("value");
            Assert.Contains("1. early-id-later",monitoredEvidence.GetString());
            Assert.True(serialized.Contains("early-id-later",StringComparison.Ordinal));
            await app.StopAsync();
        }
        finally
        {
            await CleanupAsync(connection,riskId,evaluationId,planA,planB,new[] { controlA,controlB,controlC },new[] { evidenceLate,evidenceEarly,evidenceOther },new[] { stamp+550,stamp+560 });
        }
    }

    private static async Task OpenWithRetryAsync(OracleConnection connection)
    {
        Exception? last=null;
        for (int attempt=0;attempt<12;attempt++)
        {
            try { await connection.OpenAsync(); return; }
            catch (OracleException ex) when (ex.Number is 1033 or 1034 or 1089 or 1090 or 12514 or 12541 or 12543 or 12545 or 12547 or 12528)
            {
                last=ex;
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
        throw new InvalidOperationException("Disposable Oracle XE did not become available for Block 6 integration.",last);
    }

    private static OracleConnection CreateConnection(string connectionString)
    {
        var connection = new OracleConnection(connectionString)
        {
            UseHourOffsetForUnsupportedTimezone = true
        };
        return connection;
    }

    private static async Task EnsureFixtureTablesAsync(OracleConnection connection)
    {
        string[] ddl = [
            "CREATE TABLE RL_MR_RIESGOS (RIE_ID NUMBER(15) PRIMARY KEY,RIE_CODIGO VARCHAR2(30),RIE_NOMBRE VARCHAR2(250),RIE_DESCRIPCION VARCHAR2(2000),RIE_ACTIVO NUMBER(1),RIE_USR_CREACION NUMBER(10),RIE_FECHA_CREACION DATE)",
            "CREATE TABLE RL_MR_EVALUACIONES_RIESGO (EVA_ID NUMBER(15) PRIMARY KEY,EVA_RIESGO_ID NUMBER(15),EVA_VERSION_ID NUMBER(15),EVA_DATOS_JSON CLOB,EVA_CALCULOS_JSON CLOB,EVA_FECHA_REGISTRO DATE,EVA_USR_REGISTRO NUMBER(10),EVA_VERSION_ROW NUMBER(10),EVA_ACTIVO NUMBER(1))",
            "CREATE TABLE RL_MR_PROYECCIONES_EVALUACION (PROY_ID NUMBER(15),PROY_EVALUACION_ID NUMBER(15),PROY_CODIGO_RIESGO VARCHAR2(30),PROY_AREA_PRINCIPAL VARCHAR2(100),PROY_VRI NUMBER(8,2),PROY_VRR NUMBER(8,2),PROY_NIVEL_INHERENTE VARCHAR2(20),PROY_NIVEL_RESIDUAL VARCHAR2(20),PROY_RESPUESTA_RIESGO VARCHAR2(50),PROY_ESTADO_EVALUACION VARCHAR2(30),PROY_DUENO_RIESGO VARCHAR2(150),PROY_FECHA_EVAL DATE)",
            "CREATE TABLE RL_MR_FLUJOS_EVALUACION (FLU_ID NUMBER(15),FLU_EVALUACION_ID NUMBER(15),FLU_ESTADO VARCHAR2(30),FLU_FECHA DATE)",
            "CREATE TABLE RL_MR_CONTROLES_RIESGO (CON_ID NUMBER(15) PRIMARY KEY,CON_EVALUACION_ID NUMBER(15),CON_TIPO VARCHAR2(30),CON_DESCRIPCION VARCHAR2(500),CON_AUTOMATIZACION VARCHAR2(30),CON_ESTADO VARCHAR2(20),CON_ESTADO_MONITOREO VARCHAR2(30),CON_EFECTIVIDAD_MONITOREO NUMBER(5,2))",
            "CREATE TABLE RL_MR_PLANES (PLA_ID NUMBER(15) PRIMARY KEY,PLA_EVALUACION_ID NUMBER(15),PLA_DESCRIPCION VARCHAR2(500),PLA_AVANCE NUMBER(5,2),PLA_PRESUPUESTO NUMBER(15,2),PLA_FECHA_INICIO DATE,PLA_FECHA_FIN DATE,PLA_ESTADO VARCHAR2(30),PLA_MONITOREO_SEGUIMIENTO VARCHAR2(1000),PLA_RESPONSABLES VARCHAR2(1000),PLA_RECURSOS VARCHAR2(1000))",
            "CREATE TABLE RL_MR_ACTIVIDADES (ACT_ID NUMBER(15) PRIMARY KEY,ACT_PLAN_ID NUMBER(15),ACT_DESCRIPCION VARCHAR2(500),ACT_RESPONSABLE VARCHAR2(150),ACT_AVANCE NUMBER(5,2),ACT_FECHA_INICIO DATE,ACT_FECHA_FIN DATE,ACT_ESTADO VARCHAR2(30))",
            "CREATE TABLE RL_MR_SENALES_ALERTA (ALE_ID NUMBER(15) PRIMARY KEY,ALE_EVALUACION_ID NUMBER(15),ALE_CODIGO VARCHAR2(50),ALE_INDICADOR VARCHAR2(150),ALE_ESTADO VARCHAR2(30),ALE_FECHA_DISPARO DATE)",
            "CREATE TABLE RL_MR_AUTOMONITOREO (MON_ID NUMBER(15) PRIMARY KEY,MON_EVALUACION_ID NUMBER(15),MON_ESTADO_RIESGO VARCHAR2(30),MON_ESTADO_CONTR VARCHAR2(30),MON_RESULTADO VARCHAR2(1000),MON_USR_ID NUMBER(10),MON_OBSERVACIONES_AREA VARCHAR2(2000),MON_OBSERVACIONES_UGR VARCHAR2(2000),MON_FECHA DATE)",
            "CREATE TABLE RL_MR_EVIDENCIAS (EVI_ID NUMBER(15) PRIMARY KEY,EVI_NOMBRE_ARCHIVO VARCHAR2(255),EVI_FECHA_CREACION DATE)",
            "CREATE TABLE RL_MR_EVIDENCIAS_VINCULOS (EVV_ID NUMBER(15) PRIMARY KEY,EVV_EVIDENCIA_ID NUMBER(15),EVV_TIPO_ENTIDAD VARCHAR2(30),EVV_ENTIDAD_ID NUMBER(15))",
            "CREATE TABLE RL_USUARIO_CAPACIDADES (UCP_USR_ID NUMBER(10),UCP_CAPACIDAD VARCHAR2(80),UCP_ACTIVO NUMBER(1))"
        ];
        foreach(string sql in ddl)
        {
            try { await ExecuteAsync(connection,sql); }
            catch(OracleException exception) when(exception.Number==955) { }
        }
    }

    private static async Task InsertEvidenceAsync(OracleConnection connection,long id,string name,DateTime date,long controlId)
    {
        await ExecuteAsync(connection,"INSERT INTO RL_MR_EVIDENCIAS VALUES (:id,:name,:fixtureDate)",("id",id),("name",name),("fixtureDate",date));
        await ExecuteAsync(connection,"INSERT INTO RL_MR_EVIDENCIAS_VINCULOS VALUES (:link,:evi,'CONTROL',:control)",("link",id+1000),("evi",id),("control",controlId));
    }

    private static async Task CleanupAsync(OracleConnection connection,long risk,long evaluation,long planA,long planB,long[] controls,long[] evidence,long[] monitoringIds)
    {
        if(connection.State!=System.Data.ConnectionState.Open) await connection.OpenAsync();
        foreach(long id in evidence) await ExecuteAsync(connection,"DELETE FROM RL_MR_EVIDENCIAS_VINCULOS WHERE EVV_EVIDENCIA_ID=:id",("id",id));
        foreach(long id in evidence) await ExecuteAsync(connection,"DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_ID=:id",("id",id));
        foreach(long id in controls) await ExecuteAsync(connection,"DELETE FROM RL_MR_CONTROLES_RIESGO WHERE CON_ID=:id",("id",id));
        foreach(long id in monitoringIds) await ExecuteAsync(connection,"DELETE FROM RL_MR_AUTOMONITOREO WHERE MON_ID=:id AND MON_EVALUACION_ID=:eva",("id",id),("eva",evaluation));
        await ExecuteAsync(connection,"DELETE FROM RL_MR_SENALES_ALERTA WHERE ALE_EVALUACION_ID=:id",("id",evaluation));
        await ExecuteAsync(connection,"DELETE FROM RL_MR_ACTIVIDADES WHERE ACT_PLAN_ID IN (:a,:b)",("a",planA),("b",planB));
        await ExecuteAsync(connection,"DELETE FROM RL_MR_PLANES WHERE PLA_ID IN (:a,:b)",("a",planA),("b",planB));
        await ExecuteAsync(connection,"DELETE FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID=:id",("id",evaluation));
        await ExecuteAsync(connection,"DELETE FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id",("id",evaluation));
        await ExecuteAsync(connection,"DELETE FROM RL_MR_RIESGOS WHERE RIE_ID=:id",("id",risk));
    }

    private static async Task ExecuteAsync(OracleConnection connection,string sql,params (string Name,object Value)[] parameters)
    {
        await using var command=new OracleCommand(sql,connection){BindByName=true};
        foreach(var (name,value) in parameters) command.Parameters.Add(new OracleParameter(name,value));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> ReadRiskNameAsync(OracleConnection connection,OracleTransaction transaction,long id)
    {
        await using var command=new OracleCommand("SELECT RIE_NOMBRE FROM RL_MR_RIESGOS WHERE RIE_ID=:id",connection){BindByName=true,Transaction=transaction};
        command.Parameters.Add(new OracleParameter("id",id));
        return Convert.ToString(await command.ExecuteScalarAsync(),System.Globalization.CultureInfo.InvariantCulture)!;
    }

    private static string BuildAnswersJson()
    {
        using JsonDocument manifest=JsonDocument.Parse(File.ReadAllText(FindRoot()+"/backend/RL.API/Features/MatricesRiesgos/Contracts/matriz_riesgos_82_campos_manifest.json"));
        var values=new Dictionary<string,object?>(StringComparer.OrdinalIgnoreCase);
        foreach(JsonElement field in manifest.RootElement.EnumerateArray())
        {
            int number=field.GetProperty("number").GetInt32();
            if(number is 1 or 2 or 8 or 9 or 12 or 13 or 20 or 22 or 23 or 24 or 26 or 27 or 28 or 30 or 31 or 33 or 34 or 35 or 36 or 37 or 38 or 40 or 41 or 42 or 43 or 44 or 45 or 46 or 47 or 48 or 49 or >=50 and <=69 or >=70 and <=82) continue;
            values[JsonKey(field)] = number switch { 5=>"GTIC", 21 or 25 or 29=>"Razonable",39=>"Mitigar",_=>$"Fixture F{number:00}" };
        }
        values["tipo_riesgo"]="GTIC";
        values["respuesta_riesgo"]="Mitigar";
        values["amenazas_gtic"]="Amenaza fixture";
        values["vulnerabilidades_gtic"]="Vulnerabilidad fixture";
        values["activos_informacion_gtic"]="Activo fixture";
        return JsonSerializer.Serialize(values);
    }

    private static string BuildCalculationsJson()
    {
        using JsonDocument manifest=JsonDocument.Parse(File.ReadAllText(FindRoot()+"/backend/RL.API/Features/MatricesRiesgos/Contracts/matriz_riesgos_82_campos_manifest.json"));
        var values=new Dictionary<string,object?>(StringComparer.OrdinalIgnoreCase);
        foreach(JsonElement field in manifest.RootElement.EnumerateArray())
        {
            int n=field.GetProperty("number").GetInt32();
            if(n is not (12 or 13 or 22 or 23 or 26 or 27 or 30 or 31 or 33 or >=34 and <=38 or >=50 and <=69)) continue;
            values[JsonKey(field)] = n switch { 13 or 34 or 38=>"Riesgo bajo",22 or 26 or 30=>"Razonable",12=>3,33=>0.63m,_=>1 };
        }
        return JsonSerializer.Serialize(values);
    }

    private static string JsonKey(JsonElement field)
    {
        string mapping=field.GetProperty("dbOrEntityMapping").GetString()!;
        int separator=mapping.LastIndexOf("->",StringComparison.Ordinal);
        return separator>=0?mapping[(separator+2)..].Trim():field.GetProperty("canonicalKey").GetString()!;
    }

    private static string FindRoot()
    {
        DirectoryInfo? directory=new(AppContext.BaseDirectory);
        while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"AGENTS.md"))) directory=directory.Parent;
        return directory?.FullName??throw new DirectoryNotFoundException("Repository root was not found for Block 6 Oracle fixture.");
    }

    private sealed class Block6IntegrationAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "987654"),
                new Claim("modulos", "10")
            ], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
