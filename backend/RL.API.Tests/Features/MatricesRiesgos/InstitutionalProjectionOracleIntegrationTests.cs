using System.Security.Claims;
using System.Data;
using System.IO.Compression;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;
using System.Net.Http.Json;
using Block5WorkbookFormulaOracle;
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
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Shared.Results;
using RL.API.Infrastructure.Database;
using RL.API.Tests.Support;
using Xunit;
using Xunit.Abstractions;

namespace RL.API.Tests.Features.MatricesRiesgos;

/// <summary>Opt-in real Oracle integration test. CI runs it against a disposable Oracle XE service.</summary>
public sealed class InstitutionalProjectionOracleIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public InstitutionalProjectionOracleIntegrationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    [Trait("Category", "Block12OracleIntegration")]
    public async Task Canonical59RiskIdentitiesMatchOracleApiAndConsolidatedProjection()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RL_BLOCK12_ORACLE_CERTIFICATION_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase))
        {
            _output.WriteLine("BLOCK12_ORACLE_59_IDENTITY_EXECUTED=NO (Oracle disposable not configured)");
            return;
        }

        string connectionString = OracleTestDatabaseProfile.RequireLocalXeConnection(
            Environment.GetEnvironmentVariable("BLOCK12_ORACLE_CONNECTION_STRING"));
        OracleConnection.ClearAllPools();
        await using var seed = CreateConnection(connectionString);
        await OpenWithRetryAsync(seed);
        await EnsureFixtureTablesAsync(seed);
        await EnsureBlock7FixtureObjectsAsync(seed);

        WorkbookFormulaOracle workbook = WorkbookFormulaOracle.Load(FindOfficialWorkbook());
        var canonical = Enumerable.Range(2, 59)
            .Select(row => new { Ordinal = row - 1, Code = workbook.GetText(row, 2), Name = workbook.GetText(row, 3) })
            .ToArray();
        string ordinalMapPath = Path.Combine(FindRepositoryRoot(), "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "block6_risk_ordinal_map.json");
        using JsonDocument ordinalMap = JsonDocument.Parse(await File.ReadAllTextAsync(ordinalMapPath));
        var mappedCodes = ordinalMap.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => new { Ordinal = entry.GetProperty("riskNo").GetInt32(), Code = entry.GetProperty("riskCode").GetString()! })
            .OrderBy(entry => entry.Ordinal).ToArray();
        Assert.Equal(canonical.Select(risk => (risk.Ordinal, risk.Code)), mappedCodes.Select(entry => (entry.Ordinal, entry.Code)));
        Assert.Equal(59, canonical.Select(risk => risk.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.All(canonical, risk => Assert.False(string.IsNullOrWhiteSpace(risk.Code)));
        Assert.All(canonical, risk => Assert.False(string.IsNullOrWhiteSpace(risk.Name)));

        long stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 800_000_000 + 100_000_000;
        long[] riskIds = Enumerable.Range(1, 59).Select(offset => stamp + offset).ToArray();
        long[] evaluationIds = Enumerable.Range(101, 59).Select(offset => stamp + offset).ToArray();
        long[] flowIds = Enumerable.Range(201, 59).Select(offset => stamp + offset).ToArray();
        long evidenceId = stamp + 401;
        const long userId = 987654;
        DateTime approvedAt = new(2026, 8, 1);
        try
        {
            for (int index = 0; index < canonical.Length; index++)
            {
                var risk = canonical[index];
                await ExecuteAsync(seed,
                    "INSERT INTO RL_MR_RIESGOS VALUES (:id,:code,:name,'Block 12 disposable canonical identity',1,:usr,SYSDATE)",
                    ("id", riskIds[index]), ("code", risk.Code), ("name", risk.Name), ("usr", userId));
                await InsertConsolidatedFixtureAsync(seed, riskIds[index], evaluationIds[index], flowIds[index],
                    "APROBADA", approvedAt.AddMinutes(index), risk.Code, userId);
            }

            await using var app = await CreateBlock7ApiAsync(connectionString);
            string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address) };
            int matchedRiskApi = 0;
            int matchedConsolidated = 0;
            for (int index = 0; index < canonical.Length; index++)
            {
                var expected = canonical[index];
                string search = Uri.EscapeDataString(expected.Code);
                using HttpResponseMessage riskResponse = await http.GetAsync($"/api/matrices-riesgos/riesgos/paginado?pagina=1&tamanoPagina=10&buscar={search}");
                Assert.Equal(System.Net.HttpStatusCode.OK, riskResponse.StatusCode);
                using JsonDocument riskBody = JsonDocument.Parse(await riskResponse.Content.ReadAsStringAsync());
                JsonElement riskItems = riskBody.RootElement.GetProperty("datos").GetProperty("items");
                JsonElement apiRisk = Assert.Single(riskItems.EnumerateArray());
                Assert.Equal(riskIds[index], apiRisk.GetProperty("rieId").GetInt64());
                Assert.Equal(expected.Code, apiRisk.GetProperty("rieCodigo").GetString());
                Assert.Equal(expected.Name, apiRisk.GetProperty("rieNombre").GetString());
                matchedRiskApi++;

                using HttpResponseMessage consolidatedResponse = await http.GetAsync(
                    $"/api/matrices-riesgos/consolidado/paginado?pagina=1&tamanoPagina=10&buscar={search}");
                Assert.Equal(System.Net.HttpStatusCode.OK, consolidatedResponse.StatusCode);
                using JsonDocument consolidatedBody = JsonDocument.Parse(await consolidatedResponse.Content.ReadAsStringAsync());
                JsonElement consolidatedItems = consolidatedBody.RootElement.GetProperty("datos").GetProperty("items");
                JsonElement consolidatedRisk = Assert.Single(consolidatedItems.EnumerateArray());
                Assert.Equal(riskIds[index], consolidatedRisk.GetProperty("riesgoId").GetInt64());
                Assert.Equal(evaluationIds[index], consolidatedRisk.GetProperty("evaluacionId").GetInt64());
                Assert.Equal(expected.Code, consolidatedRisk.GetProperty("codigoRiesgo").GetString());
                Assert.Equal("APROBADA", consolidatedRisk.GetProperty("estadoEvaluacion").GetString());
                matchedConsolidated++;
            }

            Assert.Equal(59, matchedRiskApi);
            Assert.Equal(59, matchedConsolidated);
            _output.WriteLine("BLOCK12_ORACLE_59_IDENTITY_DB_API_CONSOLIDATED=59/59");

            await ExecuteAsync(seed,
                "INSERT INTO RL_MR_EVIDENCIAS (EVI_ID,EVI_NOMBRE_ARCHIVO,EVI_EXTENSION,EVI_TAMANO,EVI_HASH,EVI_RUTA,EVI_USR_CREACION) VALUES (:id,'Block12 fixture.pdf','pdf',1,'BLOCK12-FIXTURE','App_Data/Evidencias/block12-fixture.pdf',:usr)",
                ("id", evidenceId), ("usr", userId));
            IAuditoriaRepository audit = InterfaceStub.Create<IAuditoriaRepository>(out _);
            var evidenceRepository = new MatricesRiesgosRepository(new OracleDbContext(WithoutPooling(connectionString)), audit);
            ResultadoEliminacionEvidencia deleteResult = await evidenceRepository.EliminarEvidenciaSeguraAsync(
                evidenceId, () => Task.FromResult(false), userId, null);
            Assert.Equal(ResultadoEliminacionEvidencia.FalloDisco, deleteResult);
            Assert.Equal(1, await ScalarLongAsync(seed, "SELECT COUNT(*) FROM RL_MR_EVIDENCIAS WHERE EVI_ID=:id", ("id", evidenceId)));
            await ExecuteAsync(seed, "DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_ID=:id", ("id", evidenceId));
            _output.WriteLine("BLOCK12_ORACLE_FILESYSTEM_FAILURE_ROLLBACK=PASS");
        }
        finally
        {
            await ExecuteAsync(seed, "DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_ID=:id", ("id", evidenceId));
            foreach (long evaluationId in evaluationIds)
            {
                await ExecuteAsync(seed, "DELETE FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID=:id", ("id", evaluationId));
                await ExecuteAsync(seed, "DELETE FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID=:id", ("id", evaluationId));
                await ExecuteAsync(seed, "DELETE FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", evaluationId));
            }
            foreach (long riskId in riskIds)
                await ExecuteAsync(seed, "DELETE FROM RL_MR_RIESGOS WHERE RIE_ID=:id", ("id", riskId));
            _output.WriteLine("BLOCK12_ORACLE_59_IDENTITY_CLEANUP=PASS");
        }
    }

    [Fact]
    [Trait("Category", "Block12OracleIntegration")]
    public async Task WorkbookCalculationResultsSurviveOracleApiProjectionAndBothExportsForAllRisks()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RL_BLOCK12_ORACLE_CERTIFICATION_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase))
        {
            _output.WriteLine("BLOCK12_FULL_CHAIN_2006_EXECUTED=NO (Oracle disposable not configured)");
            return;
        }

        string connectionString = OracleTestDatabaseProfile.RequireLocalXeConnection(
            Environment.GetEnvironmentVariable("BLOCK12_ORACLE_CONNECTION_STRING"));
        OracleConnection.ClearAllPools();
        await using var seed = CreateConnection(connectionString);
        await OpenWithRetryAsync(seed);
        await EnsureFixtureTablesAsync(seed);
        await EnsureBlock7FixtureObjectsAsync(seed);

        WorkbookFormulaOracle workbook = WorkbookFormulaOracle.Load(FindOfficialWorkbook());
        IReadOnlyDictionary<(string RiskCode, int FieldNumber), object?> expected =
            Block5WorkbookFormulaParityTests.BuildIndependentProjectionExpectations(workbook);
        var canonical = Enumerable.Range(2, 59)
            .Select(row => new
            {
                WorkbookRow = row,
                RiskOrdinal = row - 1,
                Code = workbook.GetText(row, 2),
                Name = workbook.GetText(row, 3)
            }).ToArray();
        int[] formulaFields = expected.Keys.Select(key => key.FieldNumber).Distinct().Order().ToArray();
        Assert.Equal(34, formulaFields.Length);
        Assert.Equal(59 * 34, expected.Count);
        Assert.Equal(34, expected.Keys.Count(key => key.RiskCode == "ROP-CUMP-54"));
        IReadOnlyDictionary<int, string> calculationKeys = ReadCalculationKeysByField();

        long stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 700_000_000 + 200_000_000;
        long[] riskIds = Enumerable.Range(1, 59).Select(offset => stamp + offset).ToArray();
        long[] evaluationIds = Enumerable.Range(101, 59).Select(offset => stamp + offset).ToArray();
        long[] flowIds = Enumerable.Range(201, 59).Select(offset => stamp + offset).ToArray();
        const long userId = 987654;
        DateTime approvedAt = new(2026, 9, 1);
        try
        {
            for (int index = 0; index < canonical.Length; index++)
            {
                var risk = canonical[index];
                string calculationsJson = BuildExpectedCalculationsJson(risk.Code, formulaFields, calculationKeys, expected);
                InstitutionalProjectionSource projectionFixture = InstitutionalProjectionMapperTests.CreateSource();
                await ExecuteAsync(seed,
                    "INSERT INTO RL_MR_RIESGOS VALUES (:id,:code,:name,'Block 12 disposable calculation chain',1,:usr,SYSDATE)",
                    ("id", riskIds[index]), ("code", risk.Code), ("name", risk.Name), ("usr", userId));
                await InsertConsolidatedFixtureAsync(seed, riskIds[index], evaluationIds[index], flowIds[index],
                    "APROBADA", approvedAt.AddMinutes(index), risk.Code, userId,
                    projectionFixture.Evaluation.EvaDataJson, calculationsJson);

                string persisted = await ScalarTextAsync(seed,
                    "SELECT EVA_CALCULOS_JSON FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", evaluationIds[index]));
                using JsonDocument persistedJson = JsonDocument.Parse(persisted);
                foreach (int fieldNumber in formulaFields)
                {
                    string key = calculationKeys[fieldNumber];
                    object? actual = ReadJsonValue(persistedJson.RootElement.GetProperty(key));
                    AssertEquivalent(expected[(risk.Code, fieldNumber)], actual,
                        $"Persisted Oracle calculation mismatch for {risk.Code}/F{fieldNumber}.");
                }
            }

            await using var app = await CreateBlock7ApiAsync(connectionString);
            string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address) };
            var exportRows = new List<RiesgoReporteFilaDto>(59);
            int apiCalculationValues = 0;
            for (int index = 0; index < canonical.Length; index++)
            {
                var risk = canonical[index];
                using HttpResponseMessage response = await http.GetAsync(
                    $"/api/matrices-riesgos/evaluaciones/{evaluationIds[index]}/proyeccion-institucional");
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                JsonElement projection = body.RootElement.GetProperty("datos");
                Assert.Equal(evaluationIds[index], projection.GetProperty("evaluationId").GetInt64());
                Assert.Equal(riskIds[index], projection.GetProperty("riskId").GetInt64());
                JsonElement fields = projection.GetProperty("fields");
                Assert.Equal(82, fields.GetArrayLength());
                Assert.Equal(Enumerable.Range(1, 82), fields.EnumerateArray().Select(field => field.GetProperty("fieldNumber").GetInt32()));
                foreach (int fieldNumber in formulaFields)
                {
                    JsonElement projectedField = fields.EnumerateArray().Single(field => field.GetProperty("fieldNumber").GetInt32() == fieldNumber);
                    AssertEquivalent(expected[(risk.Code, fieldNumber)], ReadJsonValue(projectedField.GetProperty("value")),
                        $"API projection mismatch for {risk.Code}/F{fieldNumber}.");
                    apiCalculationValues++;
                }

                using var scope = app.Services.CreateScope();
                IMatricesRiesgosAppService appService = scope.ServiceProvider.GetRequiredService<IMatricesRiesgosAppService>();
                ServiceResult<IReadOnlyList<RiesgoReporteFilaDto>> selected = await appService.ObtenerConsolidadoParaExportacionAsync(
                    new FiltroReporteMatricesDto { Buscar = risk.Code, EstadoEvaluacion = "APROBADA" });
                Assert.True(selected.Success, selected.Message);
                RiesgoReporteFilaDto row = Assert.Single(selected.Data!);
                Assert.Equal(riskIds[index], row.RiesgoId);
                Assert.Equal(evaluationIds[index], row.EvaluacionId);
                Assert.Equal("APROBADA", row.EstadoEvaluacion);
                exportRows.Add(row);
            }

            Assert.Equal(59 * 34, apiCalculationValues);
            using (var scope = app.Services.CreateScope())
            {
                IInstitutionalProjectionService projectionService = scope.ServiceProvider.GetRequiredService<IInstitutionalProjectionService>();
                ServiceResult<IReadOnlyDictionary<long, InstitutionalEvaluationProjectionDto>> projections =
                    await projectionService.GetManyAsync(evaluationIds, userId, CancellationToken.None);
                Assert.True(projections.Success, projections.Message);
                Assert.Equal(evaluationIds.Order(), projections.Data!.Keys.Order());
                int projectionCalculationValues = 0;
                foreach (var risk in canonical)
                {
                    int index = risk.RiskOrdinal - 1;
                    InstitutionalEvaluationProjectionDto projection = projections.Data[evaluationIds[index]];
                    Assert.Equal(riskIds[index], projection.RiskId);
                    Assert.Equal(evaluationIds[index], projection.EvaluationId);
                    foreach (int fieldNumber in formulaFields)
                    {
                        InstitutionalProjectionFieldDto field = projection.Fields.Single(item => item.FieldNumber == fieldNumber);
                        AssertEquivalent(expected[(risk.Code, fieldNumber)], field.Value,
                            $"Batch projection mismatch for {risk.Code}/F{fieldNumber}.");
                        projectionCalculationValues++;
                    }
                }
                Assert.Equal(2006, projectionCalculationValues);

                var exporter = new MatricesRiesgosReportExportService();
                ArchivoReporteDto excel = exporter.CrearExcelMatrizCompleta(exportRows, projections.Data);
                using (var zip = new ZipArchive(new MemoryStream(excel.Contenido), ZipArchiveMode.Read))
                {
                    XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                    XDocument sheet = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
                    XElement header = sheet.Descendants(ns + "row").Single(row => (string?)row.Attribute("r") == "4");
                    Assert.Equal(82, header.Elements(ns + "c").Count());
                    XElement[] dataRows = sheet.Descendants(ns + "row")
                        .Where(row => int.TryParse((string?)row.Attribute("r"), out int number) && number >= 5).ToArray();
                    Assert.Equal(59, dataRows.Length);
                    for (int rowIndex = 0; rowIndex < exportRows.Count; rowIndex++)
                    {
                        InstitutionalEvaluationProjectionDto projection = projections.Data[exportRows[rowIndex].EvaluacionId];
                        XElement[] cells = dataRows[rowIndex].Elements(ns + "c").ToArray();
                        Assert.Equal(82, cells.Length);
                        foreach (int fieldNumber in formulaFields)
                        {
                            InstitutionalProjectionFieldDto field = projection.Fields[fieldNumber - 1];
                            Assert.Equal(ExpectedExcelValue(field), ReadExcelCell(cells[fieldNumber - 1], ns));
                        }
                    }
                }

                ArchivoReporteDto pdf = exporter.CrearPdfMatrizCompleta(exportRows, projections.Data);
                string pdfContent = System.Text.Encoding.Latin1.GetString(pdf.Contenido);
                Assert.StartsWith("%PDF-1.4", pdfContent, StringComparison.Ordinal);
                Assert.EndsWith("%%EOF", pdfContent, StringComparison.Ordinal);
                foreach (RiesgoReporteFilaDto row in exportRows)
                {
                    InstitutionalEvaluationProjectionDto projection = projections.Data[row.EvaluacionId];
                    Assert.Contains(projection.RiskCode, pdfContent, StringComparison.Ordinal);
                    foreach (int fieldNumber in formulaFields)
                    {
                        InstitutionalProjectionFieldDto field = projection.Fields[fieldNumber - 1];
                        string value = field.Value is null ? (field.State is "NOT_APPLICABLE" or "ABSENT_CONTROL_PROJECTED" ? "No aplica" : "Sin dato")
                            : Convert.ToString(field.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                        Assert.Contains(EscapePdfString(value), pdfContent, StringComparison.Ordinal);
                    }
                }
            }

            Assert.Equal(59, exportRows.Count);
            _output.WriteLine("BLOCK12_FORMULA_ORACLE_TO_ORACLE_PERSISTENCE=2006/2006");
            _output.WriteLine("BLOCK12_FORMULA_ORACLE_TO_API_PROJECTION=2006/2006");
            _output.WriteLine("BLOCK12_FORMULA_ORACLE_TO_XLSX=2006/2006");
            _output.WriteLine("BLOCK12_FORMULA_ORACLE_TO_PDF=2006/2006");
            _output.WriteLine("BLOCK12_CROSS_CHANNEL_EVALUATION_IDS=59/59");
            _output.WriteLine("BLOCK12_ROP_CUMP_54_FULL_CHAIN=34/34");
        }
        finally
        {
            foreach (long evaluationId in evaluationIds)
            {
                await ExecuteAsync(seed, "DELETE FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID=:id", ("id", evaluationId));
                await ExecuteAsync(seed, "DELETE FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID=:id", ("id", evaluationId));
                await ExecuteAsync(seed, "DELETE FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", evaluationId));
            }
            foreach (long riskId in riskIds)
                await ExecuteAsync(seed, "DELETE FROM RL_MR_RIESGOS WHERE RIE_ID=:id", ("id", riskId));
            _output.WriteLine("BLOCK12_FULL_CHAIN_ORACLE_CLEANUP=PASS");
        }
    }

    [Fact]
    [Trait("Category", "Block10OracleIntegration")]
    public async Task ConsolidatedApiAndReportSelectLatestApprovedPerRisk()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RL_BLOCK10_ORACLE_INTEGRATION_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase))
        {
            _output.WriteLine("BLOCK10_ORACLE_INTEGRATION_EXECUTED=NO (Oracle disposable not configured)");
            return;
        }
        _output.WriteLine("BLOCK10_ORACLE_INTEGRATION_EXECUTED=YES");
        string connectionString = OracleTestDatabaseProfile.RequireLocalXeConnection(
            Environment.GetEnvironmentVariable("BLOCK10_ORACLE_CONNECTION_STRING"));

        OracleConnection.ClearAllPools();
        await using var seed = CreateConnection(connectionString);
        await OpenWithRetryAsync(seed);
        await EnsureFixtureTablesAsync(seed);
        await EnsureBlock7FixtureObjectsAsync(seed);

        long stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 800_000_000 + 100_000_000;
        long[] riskIds = [stamp + 1, stamp + 2, stamp + 3, stamp + 4];
        long[] evaluationIds = Enumerable.Range(10, 9).Select(offset => stamp + offset).ToArray();
        long[] flowIds = Enumerable.Range(100, 10).Select(offset => stamp + offset).ToArray();
        DateTime t1 = new(2026, 1, 1), t2 = new(2026, 2, 1), t3 = new(2026, 3, 1), tie = new(2026, 4, 1);
        const long userId = 987654;
        try
        {
            foreach ((long riskId, int index) in riskIds.Select((id, index) => (id, index)))
                await ExecuteAsync(seed, "INSERT INTO RL_MR_RIESGOS VALUES (:id,:code,:name,'Fixture Block 10',1,:usr,SYSDATE)",
                    ("id", riskId), ("code", $"B10-{stamp}-{index}"), ("name", $"Block 10 risk {index}"), ("usr", userId));

            // Risk 1: separate evaluations across approval, draft, review and rejection states.
            await InsertConsolidatedFixtureAsync(seed, riskIds[0], evaluationIds[0], flowIds[0], "APROBADA", t1, $"B10-{stamp}", userId);
            await InsertConsolidatedFixtureAsync(seed, riskIds[0], evaluationIds[1], flowIds[1], "APROBADA", t2, $"B10-{stamp}", userId);
            await InsertConsolidatedFixtureAsync(seed, riskIds[0], evaluationIds[2], flowIds[2], "BORRADOR", t3, $"B10-{stamp}", userId);
            await InsertConsolidatedFixtureAsync(seed, riskIds[0], evaluationIds[3], flowIds[3], "RECHAZADA", t3.AddDays(1), $"B10-{stamp}", userId);
            // Risk 2: same approval timestamp; larger flow ID is the deterministic winner.
            await InsertConsolidatedFixtureAsync(seed, riskIds[1], evaluationIds[4], flowIds[4], "APROBADA", tie, $"B10-{stamp}", userId);
            await InsertConsolidatedFixtureAsync(seed, riskIds[1], evaluationIds[5], flowIds[5], "APROBADA", tie, $"B10-{stamp}", userId);
            // The last workflow event is authoritative for this evaluation.
            await InsertConsolidatedFixtureAsync(seed, riskIds[2], evaluationIds[6], flowIds[6], "APROBADA", t2, $"B10-{stamp}", userId);
            await ExecuteAsync(seed, "INSERT INTO RL_MR_FLUJOS_EVALUACION VALUES (:id,:eva,'CERRADA','test',:usr,:eventDate)",
                ("id", flowIds[7]), ("eva", evaluationIds[6]), ("usr", userId), ("eventDate", t3));
            await InsertDraftWithoutProjectionAsync(seed, riskIds[3], evaluationIds[7], t3.AddDays(2), userId);
            await InsertConsolidatedFixtureAsync(seed, riskIds[3], evaluationIds[8], flowIds[8], "EN_REVISION", t3.AddDays(3), $"B10-{stamp}", userId);

            await using var app = await CreateBlock7ApiAsync(connectionString);
            string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address) };
            string fixtureSearch = Uri.EscapeDataString($"B10-{stamp}");
            using HttpResponseMessage response = await http.GetAsync($"/api/matrices-riesgos/consolidado/paginado?pagina=1&tamanoPagina=20&buscar={fixtureSearch}");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement rows = body.RootElement.GetProperty("datos").GetProperty("items");
            var allIds = rows.EnumerateArray().Select(item => item.GetProperty("evaluacionId").GetInt64()).ToArray();
            Assert.Equal(new[] { evaluationIds[1], evaluationIds[5] }.Order(), allIds.Order());
            _output.WriteLine("BLOCK10_LATEST_APPROVED_TEST=PASS");
            Assert.Equal(2, body.RootElement.GetProperty("datos").GetProperty("totalRegistros").GetInt32());
            Assert.All(rows.EnumerateArray(), item => Assert.Equal("APROBADA", item.GetProperty("estadoEvaluacion").GetString()));
            Assert.Equal(1, rows.EnumerateArray().Count(item => item.GetProperty("riesgoId").GetInt64() == riskIds[0]));
            Assert.Equal(1, rows.EnumerateArray().Count(item => item.GetProperty("riesgoId").GetInt64() == riskIds[1]));
            Assert.DoesNotContain(rows.EnumerateArray(), item => item.GetProperty("riesgoId").GetInt64() == riskIds[2]);
            Assert.DoesNotContain(rows.EnumerateArray(), item => item.GetProperty("riesgoId").GetInt64() == riskIds[3]);

            async Task<long[]> GetIdsByStateAsync(string state)
            {
                using HttpResponseMessage filteredResponse = await http.GetAsync($"/api/matrices-riesgos/consolidado/paginado?pagina=1&tamanoPagina=20&estadoEvaluacion={state}&buscar={fixtureSearch}");
                Assert.Equal(System.Net.HttpStatusCode.OK, filteredResponse.StatusCode);
                using JsonDocument filteredBody = JsonDocument.Parse(await filteredResponse.Content.ReadAsStringAsync());
                return filteredBody.RootElement.GetProperty("datos").GetProperty("items")
                    .EnumerateArray().Select(item => item.GetProperty("evaluacionId").GetInt64()).ToArray();
            }

            Assert.Equal(new[] { evaluationIds[1], evaluationIds[5] }.Order(), (await GetIdsByStateAsync("APROBADA")).Order());
            Assert.Empty(await GetIdsByStateAsync("BORRADOR"));
            Assert.Empty(await GetIdsByStateAsync("RECHAZADA"));
            Assert.Empty(await GetIdsByStateAsync("CERRADA"));
            Assert.Empty(await GetIdsByStateAsync("EN_REVISION"));

            using var scope = app.Services.CreateScope();
            IMatricesRiesgosAppService service = scope.ServiceProvider.GetRequiredService<IMatricesRiesgosAppService>();
            var export = await service.ObtenerConsolidadoParaExportacionAsync(new FiltroReporteMatricesDto { Buscar = $"B10-{stamp}" });
            Assert.True(export.Success);
            Assert.Equal(allIds.Order(), export.Data!.Select(row => row.EvaluacionId).Order());
            IReadOnlyList<RiesgoReporteFilaDto> exportRows = export.Data!;

            IInstitutionalProjectionReadRepository projectionRepository = scope.ServiceProvider.GetRequiredService<IInstitutionalProjectionReadRepository>();
            IReadOnlyDictionary<long, InstitutionalProjectionSnapshot> batchSources =
                await projectionRepository.ReadManyAsync(exportRows.Select(row => row.EvaluacionId).ToArray(), userId, CancellationToken.None);
            Assert.Equal(exportRows.Count, batchSources.Count);
            Assert.All(batchSources.Values, snapshot => Assert.Equal(9, snapshot.QueryCount));

            IInstitutionalProjectionService projectionService = scope.ServiceProvider.GetRequiredService<IInstitutionalProjectionService>();
            ServiceResult<IReadOnlyDictionary<long, InstitutionalEvaluationProjectionDto>> batch =
                await projectionService.GetManyAsync(exportRows.Select(row => row.EvaluacionId).ToArray(), userId, CancellationToken.None);
            Assert.True(batch.Success, batch.Message);
            Assert.Equal(allIds.Order(), batch.Data!.Keys.Order());
            foreach (RiesgoReporteFilaDto row in exportRows)
            {
                ServiceResult<InstitutionalEvaluationProjectionDto> single = await projectionService.GetAsync(row.EvaluacionId, userId, CancellationToken.None);
                Assert.True(single.Success, single.Message);
                InstitutionalEvaluationProjectionDto batched = batch.Data[row.EvaluacionId];
                Assert.Equal(82, batched.Fields.Count);
                Assert.Equal(single.Data!.Fields.Select(field => field.FieldLabel), batched.Fields.Select(field => field.FieldLabel));
                Assert.Equal(single.Data.Fields.Select(field => field.Value), batched.Fields.Select(field => field.Value));
                Assert.Equal(single.Data.Fields.Select(field => field.State), batched.Fields.Select(field => field.State));
            }
            _output.WriteLine("BLOCK10_BATCH_82_PROJECTION=PASS");

            var exporter = new MatricesRiesgosReportExportService();
            ArchivoReporteDto excelFile = exporter.CrearExcelMatrizCompleta(exportRows, batch.Data);
            using (var zip = new ZipArchive(new MemoryStream(excelFile.Contenido), ZipArchiveMode.Read))
            {
                XDocument sheet = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
                XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                XElement headerRow = sheet.Descendants(spreadsheet + "row").Single(item => (string?)item.Attribute("r") == "4");
                string[] expectedHeaders = new InstitutionalProjectionMapper().GetFieldCatalog().Select(field => field.FieldLabel).ToArray();
                Assert.Equal(82, headerRow.Elements(spreadsheet + "c").Count());
                Assert.Equal(expectedHeaders, headerRow.Elements(spreadsheet + "c")
                    .Select(cell => (string?)cell.Element(spreadsheet + "is")?.Element(spreadsheet + "t") ?? string.Empty));
                XElement[] dataRows = sheet.Descendants(spreadsheet + "row")
                    .Where(item => int.TryParse((string?)item.Attribute("r"), out int rowNumber) && rowNumber >= 5).ToArray();
                Assert.Equal(exportRows.Count, dataRows.Length);
                for (int rowIndex = 0; rowIndex < exportRows.Count; rowIndex++)
                {
                    RiesgoReporteFilaDto selected = exportRows[rowIndex];
                    InstitutionalEvaluationProjectionDto source = batch.Data[selected.EvaluacionId];
                    XElement[] cells = dataRows[rowIndex].Elements(spreadsheet + "c").ToArray();
                    Assert.Equal(82, cells.Length);
                    Assert.Equal(selected.RiesgoId, source.RiskId);
                    Assert.Equal(selected.EvaluacionId, source.EvaluationId);
                    for (int fieldIndex = 0; fieldIndex < source.Fields.Count; fieldIndex++)
                        Assert.Equal(ExpectedExcelValue(source.Fields[fieldIndex]), ReadExcelCell(cells[fieldIndex], spreadsheet));
                }
            }
            ArchivoReporteDto pdfFile = exporter.CrearPdfMatrizCompleta(exportRows, batch.Data);
            string pdfContent = System.Text.Encoding.Latin1.GetString(pdfFile.Contenido);
            Assert.StartsWith("%PDF-1.4", pdfContent, StringComparison.Ordinal);
            foreach (RiesgoReporteFilaDto row in exportRows)
            {
                InstitutionalEvaluationProjectionDto source = batch.Data[row.EvaluacionId];
                Assert.Equal(row.RiesgoId, source.RiskId);
                Assert.Equal(row.EvaluacionId, source.EvaluationId);
                Assert.Contains(source.RiskCode, pdfContent, StringComparison.Ordinal);
            }
            Assert.Equal(82, new InstitutionalProjectionMapper().GetFieldCatalog().Count);
            Assert.EndsWith("%%EOF", pdfContent, StringComparison.Ordinal);
            _output.WriteLine("BLOCK10_EXPORT_SOURCE_PARITY=PASS");
        }
        finally
        {
            foreach (long evaluationId in evaluationIds)
            {
                await ExecuteAsync(seed, "DELETE FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID=:id", ("id", evaluationId));
                await ExecuteAsync(seed, "DELETE FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID=:id", ("id", evaluationId));
                await ExecuteAsync(seed, "DELETE FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", evaluationId));
            }
            foreach (long riskId in riskIds)
                await ExecuteAsync(seed, "DELETE FROM RL_MR_RIESGOS WHERE RIE_ID=:id", ("id", riskId));
            _output.WriteLine("BLOCK10_ORACLE_TEST_CLEANUP=PASS");
        }
    }

    private static object ExpectedExcelValue(InstitutionalProjectionFieldDto field)
    {
        if (field.Value is null)
            return field.State is "NOT_APPLICABLE" or "ABSENT_CONTROL_PROJECTED" ? "No aplica" : "Sin dato";
        if (field.Value is DateTime date) return date.ToOADate();
        if (field.Value is bool boolean) return boolean;
        if (field.Value is InstitutionalPlanProjectionValueDto[] plans)
            return string.Join(Environment.NewLine, plans.Select(plan => plan.Value is null
                ? $"{plan.PlanOrdinalIndex}. {(plan.State == "NOT_APPLICABLE" ? "No aplica" : "Sin dato")}" : $"{plan.PlanOrdinalIndex}. {plan.Value}"));
        if (field.Value is string[] strings) return string.Join(Environment.NewLine, strings);
        if (field.Value is System.Text.Json.JsonElement element) return element.ToString();
        if (field.Value is IConvertible convertible && field.Value is not string)
            return Convert.ToDouble(convertible, System.Globalization.CultureInfo.InvariantCulture);
        return Convert.ToString(field.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static IReadOnlyDictionary<int, string> ReadCalculationKeysByField()
    {
        string manifestPath = Path.Combine(FindRepositoryRoot(), "backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "matriz_riesgos_82_campos_manifest.json");
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

    private static string BuildExpectedCalculationsJson(
        string riskCode,
        IReadOnlyCollection<int> fieldNumbers,
        IReadOnlyDictionary<int, string> keys,
        IReadOnlyDictionary<(string RiskCode, int FieldNumber), object?> expected)
    {
        var calculations = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (int fieldNumber in fieldNumbers)
            calculations.Add(keys[fieldNumber], expected[(riskCode, fieldNumber)]);
        return JsonSerializer.Serialize(calculations);
    }

    private static object? ReadJsonValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.Clone()
    };

    private static void AssertEquivalent(object? expected, object? actual, string message)
    {
        if (expected is JsonElement expectedElement) expected = ReadJsonValue(expectedElement);
        if (actual is JsonElement actualElement) actual = ReadJsonValue(actualElement);
        if (expected is null || actual is null)
        {
            Assert.True(expected is null && actual is null, message);
            return;
        }
        try
        {
            bool expectedNumeric = expected is byte or short or int or long or float or double or decimal;
            bool actualNumeric = actual is byte or short or int or long or float or double or decimal;
            if (expectedNumeric && actualNumeric)
            {
                Assert.Equal(Convert.ToDouble(expected, System.Globalization.CultureInfo.InvariantCulture),
                    Convert.ToDouble(actual, System.Globalization.CultureInfo.InvariantCulture), 10);
                return;
            }
            Assert.Equal(Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToString(actual, System.Globalization.CultureInfo.InvariantCulture));
        }
        catch (Xunit.Sdk.XunitException exception)
        {
            throw new Xunit.Sdk.XunitException($"{message} {exception.Message}");
        }
    }

    private static string EscapePdfString(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);

    private static object ReadExcelCell(XElement cell, XNamespace ns)
    {
        string? type = (string?)cell.Attribute("t");
        if (type == "inlineStr") return (string?)cell.Element(ns + "is")?.Element(ns + "t") ?? string.Empty;
        string value = (string?)cell.Element(ns + "v") ?? string.Empty;
        if (type == "b") return value == "1";
        return double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double number)
            ? number : string.Empty;
    }

    private static Task InsertDraftWithoutProjectionAsync(OracleConnection connection, long riskId, long evaluationId, DateTime createdAt, long userId) =>
        ExecuteAsync(connection,
            "INSERT INTO RL_MR_EVALUACIONES_RIESGO (EVA_ID,EVA_RIESGO_ID,EVA_VERSION_ID,EVA_DATOS_JSON,EVA_CALCULOS_JSON,EVA_FECHA_REGISTRO,EVA_USR_REGISTRO,EVA_VERSION_ROW,EVA_ACTIVO) VALUES (:id,:risk,1,'{}','{}',:createdAt,:usr,1,1)",
            ("id", evaluationId), ("risk", riskId), ("createdAt", createdAt), ("usr", userId));

    private static async Task InsertConsolidatedFixtureAsync(
        OracleConnection connection, long riskId, long evaluationId, long flowId,
        string status, DateTime eventDate, string code, long userId,
        string? answersJson = null, string? calculationsJson = null)
    {
        InstitutionalProjectionSource projectionFixture = InstitutionalProjectionMapperTests.CreateSource();
        await ExecuteAsync(connection,
            "INSERT INTO RL_MR_EVALUACIONES_RIESGO (EVA_ID,EVA_RIESGO_ID,EVA_VERSION_ID,EVA_DATOS_JSON,EVA_CALCULOS_JSON,EVA_FECHA_REGISTRO,EVA_USR_REGISTRO,EVA_VERSION_ROW,EVA_ACTIVO) VALUES (:id,:risk,1,:answers,:calculations,:eventDate,:usr,1,1)",
            ("id", evaluationId), ("risk", riskId), ("answers", answersJson ?? projectionFixture.Evaluation.EvaDataJson), ("calculations", calculationsJson ?? projectionFixture.Evaluation.EvaDataCalcJson), ("eventDate", eventDate), ("usr", userId));
        await ExecuteAsync(connection,
            "INSERT INTO RL_MR_PROYECCIONES_EVALUACION (PROY_ID,PROY_EVALUACION_ID,PROY_CODIGO_RIESGO,PROY_AREA_PRINCIPAL,PROY_VRI,PROY_VRR,PROY_NIVEL_INHERENTE,PROY_NIVEL_RESIDUAL,PROY_RESPUESTA_RIESGO,PROY_ESTADO_EVALUACION,PROY_DUENO_RIESGO,PROY_FECHA_EVAL) VALUES (:id,:eva,:code,'Block8',3,2,'BAJO','BAJO','Mitigar',:status,'Fixture',:eventDate)",
            ("id", evaluationId + 1000), ("eva", evaluationId), ("code", code), ("status", status), ("eventDate", eventDate));
        await ExecuteAsync(connection,
            "INSERT INTO RL_MR_FLUJOS_EVALUACION VALUES (:id,:eva,:status,'test',:usr,:eventDate)",
            ("id", flowId), ("eva", evaluationId), ("status", status), ("usr", userId), ("eventDate", eventDate));
    }

    [Fact]
    [Trait("Category", "Block7OracleIntegration")]
    public async Task DraftEndpoints_CreateReadUpdatePreviewAndRejectStaleVersionAgainstOracle()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RL_BLOCK7_ORACLE_INTEGRATION_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase)) return;
        string connectionString = OracleTestDatabaseProfile.RequireLocalXeConnection(
            Environment.GetEnvironmentVariable("BLOCK7_ORACLE_CONNECTION_STRING"));

        // Start this opt-in fixture with isolated ODP.NET pools.
        OracleConnection.ClearAllPools();
        await using var seed = CreateConnection(connectionString);
        await OpenWithRetryAsync(seed);
        await EnsureFixtureTablesAsync(seed);
        await EnsureBlock7FixtureObjectsAsync(seed);

        long stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1_000_000_000;
        long riskId = stamp + 710;
        long versionId = stamp + 711;
        const long userId = 987654;
        const string ruleCode = "BLOCK7_ORACLE_FIXTURE";
        string definition = JsonSerializer.Serialize(new
        {
            codigoFormulario = "BLOCK7_ORACLE_FIXTURE",
            nombreFormulario = "Block 7 Oracle fixture",
            reglaCalculo = new { codigo = ruleCode, version = "1" },
            secciones = new[]
            {
                new
                {
                    clave = "captura",
                    titulo = "Captura de prueba",
                    campos = new object[]
                    {
                        new { clave = "area_principal", etiqueta = "Área", tipo = "texto", obligatorio = false },
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
        long evaluationId = 0;
        try
        {
            await ExecuteAsync(seed, "INSERT INTO RL_MR_RIESGOS VALUES (:id,:code,'Block 7 Oracle fixture','Disposable test risk',1,:usr,SYSDATE)", ("id", riskId), ("code", $"B7-{stamp}"), ("usr", userId));
            await ExecuteAsync(seed, "INSERT INTO RL_MR_VERSIONES_FORMULARIO (VER_ID,VER_FAMILIA_ID,VER_CODIGO,VER_VERSION,VER_JSON,VER_HASH,VER_ESTADO,VER_VIGENTE,VER_FECHA_INICIO,VER_FECHA_FIN,VER_FECHA_CREACION,VER_USR_CREACION) VALUES (:id,1,'BLOCK7_ORACLE_FIXTURE',1,:json,RPAD('A',64,'A'),'PUBLISHED',1,SYSDATE,NULL,SYSDATE,:usr)", ("id", versionId), ("json", definition), ("usr", userId));
            await ExecuteAsync(seed, "INSERT INTO RL_MR_REGLAS_CALCULO (REG_ID,REG_CODIGO,REG_VERSION,REG_NOMBRE,REG_ALGORITMO_ID,REG_ACTIVA) VALUES (:id,:code,'1','Block 7 Oracle fixture','BLOCK7_LEGACY_TEST',1)", ("id", versionId), ("code", ruleCode));

            await using var app = await CreateBlock7ApiAsync(connectionString);
            string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address) };

            var create = new EvaluacionRiesgoDto
            {
                EvaRiesgoId = riskId,
                EvaVersionId = versionId,
                EvaDataJson = "{}",
                EvaDataCalcJson = "{\"vri\":999,\"vrr\":999}",
                EvaVri = 999,
                EvaVrr = 999
            };
            using HttpResponseMessage created = await http.PostAsJsonAsync("/api/matrices-riesgos/evaluaciones/borrador", create);
            Assert.Equal(System.Net.HttpStatusCode.OK, created.StatusCode);
            using JsonDocument createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            evaluationId = createdBody.RootElement.GetProperty("datos").GetInt64();
            Assert.True(evaluationId > 0);
            Assert.Equal(1, await ScalarIntAsync(seed,
                "SELECT COUNT(*) FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id AND EVA_RIESGO_ID=:risk AND EVA_VERSION_ID=:version",
                ("id", evaluationId), ("risk", riskId), ("version", versionId)));

            const string historicalPreimage = "{\"ordinal\":87,\"rieCodigo\":\"R-LEGACY\",\"area_consolidada\":\"Área histórica\"}";
            await ExecuteAsync(seed,
                "UPDATE RL_MR_EVALUACIONES_RIESGO SET EVA_DATOS_JSON=:json WHERE EVA_ID=:id",
                ("json", historicalPreimage), ("id", evaluationId));

            using HttpResponseMessage readCreated = await http.GetAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}");
            Assert.Equal(System.Net.HttpStatusCode.OK, readCreated.StatusCode);
            EvaluacionRiesgoDto persisted = await ReadEvaluationAsync(readCreated);
            Assert.Equal(riskId, persisted.EvaRiesgoId);
            Assert.Equal(versionId, persisted.EvaVersionId);
            Assert.Equal(1, persisted.EvaVersionRow);
            Assert.Equal("BORRADOR", persisted.EvaEstado);
            AssertLegacyPreimagePreserved(persisted.EvaDataJson);
            using (JsonDocument persistedCalculations = JsonDocument.Parse(persisted.EvaDataCalcJson))
            {
                JsonElement root = persistedCalculations.RootElement;
                Assert.Equal("BLOCK7_ORACLE_FIXTURE", root.GetProperty("reglaCodigo").GetString());
                Assert.NotEqual(999, root.TryGetProperty("vri", out JsonElement vri) ? vri.GetInt32() : (int?)null);
                Assert.NotEqual(999, root.TryGetProperty("vrr", out JsonElement vrr) ? vrr.GetInt32() : (int?)null);
            }
            Assert.Null(persisted.EvaVri);
            Assert.Null(persisted.EvaVrr);
            Assert.DoesNotContain("amenazas_gtic", persisted.EvaDataJson, StringComparison.Ordinal);

            int auditBeforePreview = await ScalarIntAsync(seed, "SELECT COUNT(*) FROM RL_AUDITORIA WHERE AUD_REGISTRO_ID=:id", ("id", evaluationId.ToString()));
            long flowBeforePreview = await ScalarLongAsync(seed, "SELECT COUNT(*) FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID=:id", ("id", evaluationId));
            string answersBeforePreview = await ScalarTextAsync(seed, "SELECT EVA_DATOS_JSON FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", evaluationId));
            string calculationsBeforePreview = await ScalarTextAsync(seed, "SELECT EVA_CALCULOS_JSON FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", evaluationId));
            long projectionsBeforePreview = await ScalarLongAsync(seed, "SELECT COUNT(*) FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID=:id", ("id", evaluationId));
            int versionBeforePreview = await ScalarIntAsync(seed, "SELECT EVA_VERSION_ROW FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", evaluationId));

            var incompletePreview = new EvaluacionRiesgoDto { EvaId = evaluationId, EvaRiesgoId = riskId, EvaVersionId = versionId, EvaDataJson = "{}", EvaDataCalcJson = "{\"vri\":888}" };
            using HttpResponseMessage incomplete = await http.PostAsJsonAsync("/api/matrices-riesgos/evaluaciones/calculo-preview", incompletePreview);
            Assert.Equal(System.Net.HttpStatusCode.OK, incomplete.StatusCode);
            using JsonDocument incompleteBody = JsonDocument.Parse(await incomplete.Content.ReadAsStringAsync());
            Assert.False(incompleteBody.RootElement.GetProperty("datos").GetProperty("listo").GetBoolean());
            Assert.NotEmpty(incompleteBody.RootElement.GetProperty("datos").GetProperty("obligatoriosPendientes").EnumerateArray());

            var completeAnswers = new Dictionary<string, object>
            {
                ["area_principal"] = "Oracle test area",
                ["frecuencia_inherente"] = 3,
                ["impacto_inherente"] = 2,
                ["controles_preventivo"] = 0,
                ["controles_detectivo"] = 0,
                ["controles_correctivo"] = 0,
                ["frecuencia_residual"] = 3,
                ["impacto_residual"] = 2
            };
            var completePreview = new EvaluacionRiesgoDto { EvaId = evaluationId, EvaRiesgoId = riskId, EvaVersionId = versionId, EvaDataJson = JsonSerializer.Serialize(completeAnswers), EvaDataCalcJson = "{\"vri\":999,\"vrr\":999}", EvaVri = 999, EvaVrr = 999 };
            using HttpResponseMessage preview = await http.PostAsJsonAsync("/api/matrices-riesgos/evaluaciones/calculo-preview", completePreview);
            Assert.Equal(System.Net.HttpStatusCode.OK, preview.StatusCode);
            using JsonDocument previewBody = JsonDocument.Parse(await preview.Content.ReadAsStringAsync());
            JsonElement previewResult = previewBody.RootElement.GetProperty("datos");
            Assert.True(previewResult.GetProperty("listo").GetBoolean());
            Assert.Equal(4, previewResult.GetProperty("evaVri").GetInt32());
            Assert.Equal(4, previewResult.GetProperty("evaVrr").GetInt32());
            Assert.DoesNotContain("999", previewResult.GetProperty("evaDataCalcJson").GetString(), StringComparison.Ordinal);

            Assert.Equal(auditBeforePreview, await ScalarIntAsync(seed, "SELECT COUNT(*) FROM RL_AUDITORIA WHERE AUD_REGISTRO_ID=:id", ("id", evaluationId.ToString())));
            Assert.Equal(flowBeforePreview, await ScalarLongAsync(seed, "SELECT COUNT(*) FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID=:id", ("id", evaluationId)));
            Assert.Equal(answersBeforePreview, await ScalarTextAsync(seed, "SELECT EVA_DATOS_JSON FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", evaluationId)));
            Assert.Equal(calculationsBeforePreview, await ScalarTextAsync(seed, "SELECT EVA_CALCULOS_JSON FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", evaluationId)));
            Assert.Equal(projectionsBeforePreview, await ScalarLongAsync(seed, "SELECT COUNT(*) FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID=:id", ("id", evaluationId)));
            Assert.Equal(versionBeforePreview, await ScalarIntAsync(seed, "SELECT EVA_VERSION_ROW FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", evaluationId)));

            persisted.EvaDataJson = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["area_principal"] = "Cliente A"
            });
            using HttpResponseMessage updateA = await http.PutAsJsonAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}/borrador", persisted);
            Assert.Equal(System.Net.HttpStatusCode.OK, updateA.StatusCode);
            using HttpResponseMessage readA = await http.GetAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}");
            EvaluacionRiesgoDto afterA = await ReadEvaluationAsync(readA);
            Assert.Equal(2, afterA.EvaVersionRow);
            Assert.Contains("Cliente A", afterA.EvaDataJson, StringComparison.Ordinal);
            AssertLegacyPreimagePreserved(afterA.EvaDataJson);

            EvaluacionRiesgoDto staleClient = CopyEvaluation(persisted);
            staleClient.EvaDataJson = JsonSerializer.Serialize(new Dictionary<string, object> { ["area_principal"] = "Cliente B" });
            using HttpResponseMessage stale = await http.PutAsJsonAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}/borrador", staleClient);
            Assert.Equal(System.Net.HttpStatusCode.Conflict, stale.StatusCode);
            using HttpResponseMessage readAfterConflict = await http.GetAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}");
            EvaluacionRiesgoDto afterConflict = await ReadEvaluationAsync(readAfterConflict);
            Assert.Equal(2, afterConflict.EvaVersionRow);
            Assert.Contains("Cliente A", afterConflict.EvaDataJson, StringComparison.Ordinal);
            AssertLegacyPreimagePreserved(afterConflict.EvaDataJson);
            Assert.DoesNotContain("Cliente B", afterConflict.EvaDataJson, StringComparison.Ordinal);

            afterConflict.EvaDataJson = JsonSerializer.Serialize(new Dictionary<string, object> { ["area_principal"] = "Cliente B recargado" });
            using HttpResponseMessage updateB = await http.PutAsJsonAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}/borrador", afterConflict);
            Assert.Equal(System.Net.HttpStatusCode.OK, updateB.StatusCode);
            using HttpResponseMessage readB = await http.GetAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}");
            EvaluacionRiesgoDto afterB = await ReadEvaluationAsync(readB);
            Assert.Equal(3, afterB.EvaVersionRow);
            Assert.Contains("Cliente B recargado", afterB.EvaDataJson, StringComparison.Ordinal);
            AssertLegacyPreimagePreserved(afterB.EvaDataJson);

            EvaluacionRiesgoDto injected = CopyEvaluation(afterB);
            injected.EvaDataJson = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["area_principal"] = "Cliente B recargado",
                ["hack_field"] = "x"
            });
            using HttpResponseMessage injection = await http.PutAsJsonAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}/borrador", injected);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, injection.StatusCode);
            using HttpResponseMessage readAfterInjection = await http.GetAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}");
            EvaluacionRiesgoDto afterInjection = await ReadEvaluationAsync(readAfterInjection);
            Assert.Equal(3, afterInjection.EvaVersionRow);
            Assert.Contains("Cliente B recargado", afterInjection.EvaDataJson, StringComparison.Ordinal);
            Assert.DoesNotContain("hack_field", afterInjection.EvaDataJson, StringComparison.Ordinal);
            AssertLegacyPreimagePreserved(afterInjection.EvaDataJson);

            EvaluacionRiesgoDto derivedMutation = CopyEvaluation(afterInjection);
            derivedMutation.EvaDataJson = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["area_principal"] = "Cliente B recargado",
                ["ordinal"] = 88,
                ["rieCodigo"] = "R-LEGACY",
                ["area_consolidada"] = "Área histórica"
            });
            using HttpResponseMessage derived = await http.PutAsJsonAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}/borrador", derivedMutation);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, derived.StatusCode);
            using HttpResponseMessage readAfterDerived = await http.GetAsync($"/api/matrices-riesgos/evaluaciones/{evaluationId}");
            EvaluacionRiesgoDto afterDerived = await ReadEvaluationAsync(readAfterDerived);
            Assert.Equal(3, afterDerived.EvaVersionRow);
            AssertLegacyPreimagePreserved(afterDerived.EvaDataJson);

            EvaluacionRiesgoDto manipulatedIdentity = CopyEvaluation(completePreview);
            manipulatedIdentity.EvaRiesgoId = riskId + 1;
            using HttpResponseMessage invalidIdentity = await http.PostAsJsonAsync("/api/matrices-riesgos/evaluaciones/calculo-preview", manipulatedIdentity);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, invalidIdentity.StatusCode);
        }
        finally
        {
            if (evaluationId > 0)
            {
                await ExecuteAsync(seed, "DELETE FROM RL_AUDITORIA WHERE AUD_REGISTRO_ID=:id", ("id", evaluationId.ToString()));
                await ExecuteAsync(seed, "DELETE FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID=:id", ("id", evaluationId));
                await ExecuteAsync(seed, "DELETE FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID=:id", ("id", evaluationId));
                await ExecuteAsync(seed, "DELETE FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID=:id", ("id", evaluationId));
            }
            await ExecuteAsync(seed, "DELETE FROM RL_MR_RIESGOS WHERE RIE_ID=:id", ("id", riskId));
            await ExecuteAsync(seed, "DELETE FROM RL_MR_REGLAS_CALCULO WHERE REG_CODIGO=:code", ("code", ruleCode));
            await ExecuteAsync(seed, "DELETE FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID=:id", ("id", versionId));
        }
    }

    private static async Task<WebApplication> CreateBlock7ApiAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Services.AddAuthentication("Block7OracleIntegration")
            .AddScheme<AuthenticationSchemeOptions, Block6IntegrationAuthenticationHandler>("Block7OracleIntegration", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllers().AddApplicationPart(typeof(MatricesRiesgosController).Assembly);
        builder.Services.AddSingleton(new OracleDbContext(WithoutPooling(connectionString)));
        builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        builder.Services.AddScoped<IMatricesRiesgosRepository, MatricesRiesgosRepository>();
        builder.Services.AddScoped<IMatricesRiesgosGestionRepository, MatricesRiesgosGestionRepository>();
        builder.Services.AddScoped<IMatricesRiesgosGestionService, MatricesRiesgosGestionService>();
        builder.Services.AddScoped<IMatricesRiesgosMitigacionRepository, MatricesRiesgosMitigacionRepository>();
        builder.Services.AddScoped<InstitutionalProjectionMapper>();
        builder.Services.AddScoped<IInstitutionalProjectionReadRepository, InstitutionalProjectionReadRepository>();
        builder.Services.AddScoped<IInstitutionalProjectionService, InstitutionalProjectionService>();
        builder.Services.AddScoped<IFormularioValidador, FormularioValidador>();
        builder.Services.AddScoped<IMatricesRiesgoService, MatricesRiesgoService>();
        builder.Services.AddScoped(_ => DispatchProxy.Create<ICalculoConfiguracionRepository, EmptyCalculationConfigurationProxy>());
        builder.Services.AddScoped<DbDrivenCalculationRuntimeFactory>();
        builder.Services.AddScoped<VersionedCalculationRuntimeService>();
        builder.Services.AddScoped<MatricesRiesgosAppService>();
        builder.Services.AddScoped<IMatricesRiesgosAppService>(services => services.GetRequiredService<MatricesRiesgosAppService>());
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private static EvaluacionRiesgoDto CopyEvaluation(EvaluacionRiesgoDto source) => new()
    {
        EvaId = source.EvaId,
        EvaRiesgoId = source.EvaRiesgoId,
        EvaVersionId = source.EvaVersionId,
        EvaEstado = source.EvaEstado,
        EvaDataJson = source.EvaDataJson,
        EvaDataCalcJson = source.EvaDataCalcJson,
        EvaVri = source.EvaVri,
        EvaEtp = source.EvaEtp,
        EvaVrr = source.EvaVrr,
        EvaFechaEval = source.EvaFechaEval,
        EvaUsrEval = source.EvaUsrEval,
        EvaVersionRow = source.EvaVersionRow,
        EvaActivo = source.EvaActivo
    };

    private static void AssertLegacyPreimagePreserved(string payload)
    {
        using JsonDocument json = JsonDocument.Parse(payload);
        JsonElement root = json.RootElement;
        Assert.Equal(87, root.GetProperty("ordinal").GetInt32());
        Assert.Equal("R-LEGACY", root.GetProperty("rieCodigo").GetString());
        Assert.Equal("Área histórica", root.GetProperty("area_consolidada").GetString());
    }

    private static async Task<EvaluacionRiesgoDto> ReadEvaluationAsync(HttpResponseMessage response)
    {
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return JsonSerializer.Deserialize<EvaluacionRiesgoDto>(body.RootElement.GetProperty("datos").GetRawText(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Oracle API returned an empty evaluation response.");
    }

    private static async Task<int> ScalarIntAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters) =>
        checked((int)await ScalarLongAsync(connection, sql, parameters));

    private static async Task<long> ScalarLongAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = new OracleCommand(sql, connection) { BindByName = true };
        foreach ((string name, object value) in parameters) command.Parameters.Add(new OracleParameter(name, value));
        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<string> ScalarTextAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = new OracleCommand(sql, connection) { BindByName = true };
        foreach ((string name, object value) in parameters) command.Parameters.Add(new OracleParameter(name, value));
        return Convert.ToString(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static async Task EnsureBlock7FixtureObjectsAsync(OracleConnection connection)
    {
        string[] ddl =
        [
            "CREATE TABLE RL_MR_VERSIONES_FORMULARIO (VER_ID NUMBER(15) PRIMARY KEY,VER_FAMILIA_ID NUMBER(15),VER_CODIGO VARCHAR2(80),VER_VERSION NUMBER(10),VER_JSON CLOB,VER_HASH VARCHAR2(64),VER_ESTADO VARCHAR2(30),VER_VIGENTE NUMBER(1),VER_FECHA_INICIO DATE,VER_FECHA_FIN DATE,VER_FECHA_CREACION DATE,VER_USR_CREACION NUMBER(10))",
            "CREATE TABLE RL_MR_REGLAS_CALCULO (REG_ID NUMBER(15) PRIMARY KEY,REG_CODIGO VARCHAR2(80),REG_VERSION VARCHAR2(20),REG_NOMBRE VARCHAR2(250),REG_ALGORITMO_ID VARCHAR2(100),REG_ACTIVA NUMBER(1))",
            "CREATE TABLE RL_AUDITORIA (AUD_ID NUMBER(15) PRIMARY KEY,AUD_TABLA VARCHAR2(128),AUD_REGISTRO_ID VARCHAR2(100),AUD_ACCION VARCHAR2(80),AUD_DATOS_ANT CLOB,AUD_DATOS_NVO CLOB,AUD_USR_ID NUMBER(10),AUD_USR_EMAIL VARCHAR2(255),AUD_IP VARCHAR2(64),AUD_FECHA DATE,AUD_MODULO VARCHAR2(80))"
        ];
        foreach (string sql in ddl)
        {
            try { await ExecuteAsync(connection, sql); }
            catch (OracleException exception) when (exception.Number == 955) { }
        }

        foreach (string sequence in new[] { "SEQ_RL_MR_EVALUACIONES", "SEQ_RL_MR_PROYECCIONES", "SEQ_RL_MR_FLUJOS", "SEQ_RL_AUDITORIA" })
        {
            try { await ExecuteAsync(connection, $"CREATE SEQUENCE {sequence} START WITH 100000000 INCREMENT BY 1 NOCACHE"); }
            catch (OracleException exception) when (exception.Number == 955) { }
        }
    }

    public class EmptyCalculationConfigurationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ICalculoConfiguracionRepository.ListarFormulaBindingsPorVersionFormularioAsync))
                return Task.FromResult<IReadOnlyList<FormulaBindingDto>>(Array.Empty<FormulaBindingDto>());
            throw new NotSupportedException($"The Block 7 Oracle fixture does not configure calculation metadata: {targetMethod?.Name}.");
        }
    }

    [Fact]
    [Trait("Category", "Block6OracleIntegration")]
    public async Task GetProjection_ReadsOneOracleSnapshotAndSerializesAligned82Fields()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RL_BLOCK6_ORACLE_INTEGRATION_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase)) return;
        string connectionString = OracleTestDatabaseProfile.RequireLocalXeConnection(
            Environment.GetEnvironmentVariable("BLOCK6_ORACLE_CONNECTION_STRING"));
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
            await ExecuteAsync(connection, "INSERT INTO RL_MR_EVALUACIONES_RIESGO (EVA_ID,EVA_RIESGO_ID,EVA_VERSION_ID,EVA_DATOS_JSON,EVA_CALCULOS_JSON,EVA_FECHA_REGISTRO,EVA_USR_REGISTRO,EVA_VERSION_ROW,EVA_ACTIVO) VALUES (:id,:risk,1,:answers,:calc,:fixtureDate,:usr,1,1)", ("id",evaluationId),("risk",riskId),("answers",answers),("calc",calculations),("fixtureDate",baseDate),("usr",userId));
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
            builder.Services.AddSingleton(new OracleDbContext(WithoutPooling(connectionString)));
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
            try
            {
                // ODP.NET's async pool acquisition has timed out before any SQL ran in CI.
                // Keep this test-only bootstrap synchronous; application calls remain async.
                connection.Open();
                return;
            }
            catch (OracleException ex) when (ex.Number is 50000 or 1033 or 1034 or 1089 or 1090 or 12514 or 12541 or 12543 or 12545 or 12547 or 12528)
            {
                last=ex;
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
        throw new InvalidOperationException("Disposable Oracle XE did not become available for Block 6 integration.",last);
    }

    private static OracleConnection CreateConnection(string connectionString)
    {
        var connection = new OracleConnection(WithoutPooling(connectionString))
        {
            UseHourOffsetForUnsupportedTimezone = true
        };
        return connection;
    }

    private static string WithoutPooling(string connectionString)
    {
        var builder = new OracleConnectionStringBuilder(connectionString)
        {
            Pooling = false
        };
        return builder.ConnectionString;
    }

    private static string FindOfficialWorkbook()
    {
        return Path.Combine(FindRepositoryRoot(), "Matrices de Riesgos.xlsx");
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) return directory.FullName;
        throw new DirectoryNotFoundException("Repository root was not found for the Oracle integration test.");
    }

    private static async Task EnsureFixtureTablesAsync(OracleConnection connection)
    {
        string[] ddl = [
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
        await ExecuteAsync(connection,"INSERT INTO RL_MR_EVIDENCIAS (EVI_ID,EVI_NOMBRE_ARCHIVO,EVI_EXTENSION,EVI_TAMANO,EVI_HASH,EVI_RUTA,EVI_USR_CREACION,EVI_FECHA_CREACION) VALUES (:id,:name,'.pdf',1,'BLOCK6-FIXTURE','/block6-fixture',1,:fixtureDate)",("id",id),("name",name),("fixtureDate",date));
        await ExecuteAsync(connection,"INSERT INTO RL_MR_EVIDENCIAS_VINCULOS (EVV_ID,EVV_EVIDENCIA_ID,EVV_TIPO_ENTIDAD,EVV_ENTIDAD_ID) VALUES (:link,:evi,'CONTROL',:control)",("link",id+1000),("evi",id),("control",controlId));
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
            values[JsonKey(field)] = n switch { 13 or 34=>"Riesgo bajo",38=>"Riesgo no significativo",22 or 26 or 30=>"Razonable",12=>3,33=>0.63m,_=>1 };
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
