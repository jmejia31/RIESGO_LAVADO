using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.MatricesRiesgos.Domain;

const string settingsPath = "backend/RL.API/appsettings.json";
var settings = JObject.Parse(File.ReadAllText(settingsPath));
var connectionString = (string?)settings["ConnectionStrings"]?["OracleDB"]
    ?? throw new InvalidOperationException("Oracle connection configuration is incomplete.");

connectionString += ";Connection Timeout=30";
await using var connection = new OracleConnection(connectionString);
try
{
    await connection.OpenAsync();
}
catch (OracleException ex)
{
    Console.Error.WriteLine($"ORACLE_SEMANTIC_AUDIT=EXTERNAL_BLOCKER; ERROR={ex.Number}; MESSAGE={ex.Message}");
    Environment.ExitCode = 2;
    return;
}

var catalogCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var catalogIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var ruleKeys = new Dictionary<string, RuleInfo>(StringComparer.OrdinalIgnoreCase);
await LoadCatalogsAsync(connection, catalogCodes, catalogIds);
await LoadRulesAsync(connection, ruleKeys);
await RunRefreshTokenPrecheckAsync(connection);

var supportedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "texto", "numero", "fecha", "texto-largo", "selector-catalogo",
    "radio", "catalogo-multiple", "checkbox", "formula"
};
var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["numerico"] = "numero", ["numérico"] = "numero", ["entero"] = "numero", ["decimal"] = "numero",
    ["textarea"] = "texto-largo", ["area-texto"] = "texto-largo",
    ["catalogo"] = "selector-catalogo", ["select"] = "selector-catalogo", ["seleccion"] = "selector-catalogo",
    ["opciones"] = "radio", ["multiselect"] = "catalogo-multiple", ["seleccion-multiple"] = "catalogo-multiple",
    ["sino"] = "checkbox", ["bool"] = "checkbox", ["booleano"] = "checkbox",
    ["calculado"] = "formula", ["calculo-sistema"] = "formula", ["texto-calculado"] = "formula"
};

var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
var affected = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);
var classes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    ["VALID"] = 0, ["INVALID"] = 0, ["LEGACY_COMPATIBLE"] = 0, ["REQUIRES_REVIEW"] = 0
};
var fullHash = 0;
var hashInvalid = 0;
var hashUncheckable = 0;
var versionCount = 0;

await using (var command = connection.CreateCommand())
{
    command.BindByName = true;
    command.CommandTimeout = 60;
    command.CommandText = "SELECT VER_ID, VER_FAMILIA_ID, VER_VERSION, VER_JSON, VER_HASH, VER_ESTADO, VER_VIGENTE FROM RL_MR_VERSIONES_FORMULARIO ORDER BY VER_ID";
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var id = Convert.ToInt64(reader.GetValue(0));
        var familiaId = Convert.ToInt64(reader.GetValue(1));
        var versionNumber = Convert.ToInt32(reader.GetValue(2));
        var json = reader.GetString(3);
        var storedHash = Convert.ToString(reader.GetValue(4)) ?? string.Empty;
        var state = Convert.ToString(reader.GetValue(5)) ?? string.Empty;
        var vigente = Convert.ToInt32(reader.GetValue(6));
        versionCount++;

        var actualHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        var hashMatch = actualHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
        fullHash++;
        if (!hashMatch) { hashInvalid++; Add("HASH_INVALID", id); }

        var errors = new HashSet<string>(StringComparer.Ordinal);
        var legacy = false;
        JObject? root = null;
        try
        {
            root = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }
        catch (JsonException) { errors.Add("JSON_INVALID"); Add("JSON_INVALID", id); }

        var fields = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
        var formulas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root is null)
        {
            // Already classified as invalid.
        }
        else
        {
            var definition = root["definicionFormulario"] as JObject ?? root;
            if (root["definicionFormulario"] is JObject) legacy = true;
            var sections = definition["secciones"] as JArray;
            if (sections is null) { errors.Add("SECTIONS_INVALID"); Add("SECTIONS_INVALID", id); }
            else
            {
                var sectionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var expectedOrder = 1;
                foreach (var section in sections.OfType<JObject>())
                {
                    var sectionKey = Text(section["clave"]) ?? Text(section["identificador"]);
                    if (string.IsNullOrWhiteSpace(sectionKey) || !sectionIds.Add(sectionKey)) { errors.Add("SECTION_INVALID_OR_DUPLICATE"); Add("SECTION_INVALID_OR_DUPLICATE", id); }
                    if (Text(section["titulo"]) is null) { errors.Add("SECTION_TITLE_MISSING"); Add("SECTION_TITLE_MISSING", id); }
                    var order = Integer(section["orden"]);
                    if (order is not null && order != expectedOrder) { errors.Add("SECTION_ORDER_INVALID"); Add("SECTION_ORDER_INVALID", id); }
                    expectedOrder++;
                    var columns = Integer(section["columnasPorFila"]);
                    if (columns is not null && (columns < 1 || columns > 6)) { errors.Add("SECTION_COLUMNS_INVALID"); Add("SECTION_COLUMNS_INVALID", id); }
                    var fieldsArray = section["campos"] as JArray;
                    if (fieldsArray is null) { errors.Add("FIELDS_INVALID"); Add("FIELDS_INVALID", id); continue; }
                    foreach (var field in fieldsArray.OfType<JObject>())
                    {
                        var key = Text(field["clave"]) ?? Text(field["rutaDatos"]) ?? Text(field["identificador"]);
                        var label = Text(field["etiqueta"]);
                        var rawType = Text(field["tipo"]) ?? string.Empty;
                        var normalizedType = rawType.Trim().ToLowerInvariant().Replace('_', '-');
                        if (aliases.TryGetValue(normalizedType, out var aliasType)) { normalizedType = aliasType; legacy = true; }
                        if (string.IsNullOrWhiteSpace(key)) { errors.Add("FIELD_KEY_INVALID_OR_DUPLICATE"); Add("FIELD_KEY_INVALID_OR_DUPLICATE", id); continue; }
                        if (!fields.TryAdd(key, new FieldInfo(key, normalizedType))) { errors.Add("FIELD_KEY_INVALID_OR_DUPLICATE"); Add("FIELD_KEY_INVALID_OR_DUPLICATE", id); }
                        if (string.IsNullOrWhiteSpace(label)) { errors.Add("FIELD_LABEL_MISSING"); Add("FIELD_LABEL_MISSING", id); }
                        if (!supportedTypes.Contains(normalizedType)) { errors.Add("FIELD_TYPE_UNSUPPORTED"); Add("FIELD_TYPE_UNSUPPORTED", id); }
                        var width = Integer(field["anchoColumnas"]);
                        if (width is not null && (width < 1 || width > 6)) { errors.Add("FIELD_WIDTH_INVALID"); Add("FIELD_WIDTH_INVALID", id); }
                        var catalogRef = Text(field["codigoCatalogo"]);
                        if (!string.IsNullOrWhiteSpace(catalogRef))
                        {
                            var snapshot = SnapshotCatalogCodes(definition["catalogos"]);
                            if (!snapshot.Contains(catalogRef))
                            {
                                errors.Add("CATALOG_BROKEN_REFERENCE");
                                Add("CATALOG_BROKEN_REFERENCE", id);
                                Console.WriteLine($"FINDING=H2; VER_ID={id}; FAMILIA_ID={familiaId}; VER_VERSION={versionNumber}; FIELD={key}; CATALOG_REF={catalogRef}; CLASSIFICATION=INCONSISTENCIA_DATOS; CAUSE=CATALOG_SNAPSHOT_REFERENCE_UNRESOLVED");
                            }
                        }
                        var formula = Text(field["formula"]);
                        if (normalizedType.Equals("formula", StringComparison.OrdinalIgnoreCase) || formula is not null)
                        {
                            if (string.IsNullOrWhiteSpace(formula)) { errors.Add("FORMULA_SYNTAX_INVALID"); Add("FORMULA_SYNTAX_INVALID", id); }
                            else formulas[key] = formula;
                        }
                    }
                }
            }
            ValidateEmbeddedCatalogs(definition["catalogos"], id, errors);
            ValidateRules(definition, id, errors, ruleKeys);
            if (id is 24 or 53)
                Console.WriteLine($"FORMULA_FIELDS_METADATA; VER_ID={id}; FIELDS={string.Join(',', fields.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}");
            foreach (var diagnostic in new FormulaEngine().ValidateDefinition(json))
            {
                errors.Add(diagnostic.Code.ToString());
                if (id is 24 or 53)
                    Console.WriteLine($"FINDING=H3; VER_ID={id}; FIELD={diagnostic.Field}; CODE={diagnostic.Code}; DETAIL={diagnostic.Message}");
            }
        }

        foreach (var issue in errors.Where(error => error.StartsWith("FORMULA_", StringComparison.Ordinal)))
            Add(issue, id);
        foreach (var issue in errors.Where(error => error.StartsWith("RULE_", StringComparison.Ordinal)))
            Add(issue, id);

        var classification = errors.Count > 0 ? "INVALID" : legacy ? "LEGACY_COMPATIBLE" : "VALID";
        classes[classification]++;
        Console.WriteLine($"VER_ID={id}; FAMILIA_ID={familiaId}; VER_VERSION={versionNumber}; JSON_LENGTH={json.Length}; HASH_MATCH={hashMatch}; STATE={state}; VIGENTE={vigente}; CLASS={classification}; ERRORS={string.Join(',', errors.OrderBy(x => x))}");
    }
}

await RunPostflightAsync(connection);

Console.WriteLine($"HASH_CHECKED_FULL={fullHash}");
Console.WriteLine($"HASH_INVALID={hashInvalid}");
Console.WriteLine($"HASH_UNCHECKABLE={hashUncheckable}");
Console.WriteLine($"VERSIONS_INSPECTED={versionCount}");
foreach (var key in new[] { "CATALOG_BROKEN_REFERENCE", "FORMULA_SYNTAX_INVALID", "FORMULA_OPERATOR_UNSUPPORTED", "FORMULA_FUNCTION_UNSUPPORTED", "FORMULA_REFERENCE_UNKNOWN", "FORMULA_SELF_REFERENCE", "FORMULA_CYCLE", "RULE_BROKEN_REFERENCE" })
    Console.WriteLine($"{key}={Get(key)}; AFFECTED_VER_IDS={Affected(key)}");
Console.WriteLine($"CLASS_VALID={classes["VALID"]}; CLASS_INVALID={classes["INVALID"]}; CLASS_LEGACY_COMPATIBLE={classes["LEGACY_COMPATIBLE"]}; CLASS_REQUIRES_REVIEW={classes["REQUIRES_REVIEW"]}");

void Add(string key, long id)
{
    if (!affected.TryGetValue(key, out var ids)) { ids = new HashSet<long>(); affected[key] = ids; }
    if (ids.Add(id)) counts[key] = counts.TryGetValue(key, out var value) ? value + 1 : 1;
}

int Get(string key) => counts.TryGetValue(key, out var value) ? value : 0;

string Affected(string key) => affected.TryGetValue(key, out var ids) ? string.Join(',', ids.OrderBy(x => x)) : string.Empty;

static string? Text(JToken? token)
{
    var value = token?.Type == JTokenType.String ? token.Value<string>() : null;
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
static int? Integer(JToken? token) => token?.Type == JTokenType.Integer ? (int?)token.Value<int>() : null;
static HashSet<string> SnapshotCatalogCodes(JToken? token)
{
    var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (token is JArray array) foreach (var item in array.OfType<JObject>()) { var code = Text(item["codigo"]) ?? Text(item["identificador"]); if (code is not null) result.Add(code); }
    if (token is JObject map) foreach (var property in map.Properties()) { result.Add(property.Name); var code = Text(property.Value["codigo"]) ?? Text(property.Value["identificador"]); if (code is not null) result.Add(code); }
    return result;
}
static void ValidateEmbeddedCatalogs(JToken? token, long id, HashSet<string> errors)
{
    foreach (var catalog in (token is JArray a ? a.OfType<JObject>() : token is JObject o ? o.Properties().Select(p => p.Value).OfType<JObject>() : Enumerable.Empty<JObject>()))
    {
        var elements = catalog["elementos"] as JArray ?? catalog["elementosRespaldo"] as JArray;
        if (elements is null) continue;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in elements.OfType<JObject>()) { var code = Text(element["codigo"]); if (code is null || !seen.Add(code)) { errors.Add("CATALOG_ELEMENT_INVALID_OR_DUPLICATE"); } }
    }
}
static void ValidateRules(JObject definition, long id, HashSet<string> errors, Dictionary<string, RuleInfo> ruleKeys)
{
    var rules = definition["reglas"] as JArray ?? definition["reglasCalculo"] as JArray;
    if (rules is null) return;
    foreach (var rule in rules.OfType<JObject>())
    {
        var code = Text(rule["codigo"]);
        var version = Text(rule["version"]);
        var algorithm = Text(rule["algoritmoId"]);
        if (code is null || version is null || algorithm is null) { errors.Add("RULE_BROKEN_REFERENCE"); continue; }
        if (!ruleKeys.TryGetValue($"{code}|{version}", out var persisted)) { errors.Add("RULE_BROKEN_REFERENCE"); continue; }
        if (persisted.Active != 1 || !persisted.Algorithm.Equals(algorithm, StringComparison.OrdinalIgnoreCase)) errors.Add("RULE_BROKEN_REFERENCE");
    }
}
static async Task LoadCatalogsAsync(OracleConnection c, HashSet<string> codes, HashSet<string> ids) { await using var cmd=c.CreateCommand();cmd.CommandTimeout=60;cmd.CommandText="SELECT CAT_ID,CAT_CODIGO FROM RL_MR_CATALOGOS";await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync()){ids.Add(Convert.ToString(r.GetValue(0))!);codes.Add(Convert.ToString(r.GetValue(1))!);} }
static async Task LoadRulesAsync(OracleConnection c, Dictionary<string,RuleInfo> rules) { await using var cmd=c.CreateCommand();cmd.CommandTimeout=60;cmd.CommandText="SELECT REG_CODIGO,REG_VERSION,REG_ALGORITMO_ID,REG_ACTIVA FROM RL_MR_REGLAS_CALCULO";await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync()){var code=Convert.ToString(r.GetValue(0))!;var version=Convert.ToString(r.GetValue(1))!;rules[$"{code}|{version}"]=new RuleInfo(Convert.ToString(r.GetValue(2))!,Convert.ToInt32(r.GetValue(3)));} }
static async Task RunPostflightAsync(OracleConnection c)
{
    var checks = new Dictionary<string, string>
    {
        ["MULTIPLE_VIGENTE"] = "SELECT COUNT(*) FROM (SELECT VER_FAMILIA_ID FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_VIGENTE = 1 GROUP BY VER_FAMILIA_ID HAVING COUNT(*) > 1)",
        ["VIGENTE_NOT_PUBLISHED"] = "SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_VIGENTE = 1 AND VER_ESTADO <> 'PUBLISHED'",
        ["BAD_INTERVAL"] = "SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_FECHA_INICIO IS NOT NULL AND VER_FECHA_FIN IS NOT NULL AND VER_FECHA_FIN < VER_FECHA_INICIO",
        ["CURRENT_WITH_END_DATE"] = "SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_VIGENTE = 1 AND VER_FECHA_FIN IS NOT NULL",
        ["TEMPORAL_OVERLAPS"] = "SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO a JOIN RL_MR_VERSIONES_FORMULARIO b ON b.VER_FAMILIA_ID = a.VER_FAMILIA_ID AND b.VER_ID > a.VER_ID AND a.VER_ESTADO = 'PUBLISHED' AND b.VER_ESTADO = 'PUBLISHED' AND a.VER_FECHA_INICIO IS NOT NULL AND b.VER_FECHA_INICIO IS NOT NULL AND a.VER_FECHA_INICIO < NVL(b.VER_FECHA_FIN, TO_DATE('9999-12-31','YYYY-MM-DD')) AND b.VER_FECHA_INICIO < NVL(a.VER_FECHA_FIN, TO_DATE('9999-12-31','YYYY-MM-DD'))",
        ["HASH_FORMAT_INVALID"] = "SELECT COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_HASH IS NULL OR LENGTH(VER_HASH) <> 64 OR NOT REGEXP_LIKE(UPPER(VER_HASH), '^[0-9A-F]{64}$')",
        ["ORPHAN_VERSION"] = "SELECT COUNT(*) FROM RL_MR_EVALUACIONES_RIESGO e LEFT JOIN RL_MR_VERSIONES_FORMULARIO v ON v.VER_ID = e.EVA_VERSION_ID WHERE v.VER_ID IS NULL",
        ["BAD_VERSION_ROW"] = "SELECT COUNT(*) FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_VERSION_ROW IS NULL OR EVA_VERSION_ROW < 1",
        ["INVALID_OBJECTS"] = "SELECT COUNT(*) FROM USER_OBJECTS WHERE STATUS <> 'VALID'",
        ["DISABLED_CONSTRAINTS"] = "SELECT COUNT(*) FROM USER_CONSTRAINTS WHERE STATUS <> 'ENABLED'",
        ["CATALOG_DUPLICATE_CODES"] = "SELECT COUNT(*) FROM (SELECT CAT_CODIGO FROM RL_MR_CATALOGOS GROUP BY CAT_CODIGO HAVING COUNT(*) > 1)",
        ["CATALOG_ELEMENT_DUPLICATE_CODES"] = "SELECT COUNT(*) FROM (SELECT ELE_CATALOGO_ID, ELE_CODIGO FROM RL_MR_ELEMENTOS_CATALOGO GROUP BY ELE_CATALOGO_ID, ELE_CODIGO HAVING COUNT(*) > 1)",
        ["CATALOG_ORPHAN_REFERENCES"] = "SELECT COUNT(*) FROM RL_MR_ELEMENTOS_CATALOGO e LEFT JOIN RL_MR_CATALOGOS c ON c.CAT_ID = e.ELE_CATALOGO_ID WHERE c.CAT_ID IS NULL",
        ["CATALOG_INVALID_ELEMENTS"] = "SELECT COUNT(*) FROM RL_MR_ELEMENTOS_CATALOGO WHERE TRIM(ELE_CODIGO) IS NULL OR TRIM(ELE_VALOR) IS NULL OR ELE_ORDEN < 0"
    };
    foreach (var check in checks)
    {
        await using var command = c.CreateCommand();
        command.CommandTimeout = 60;
        command.CommandText = check.Value;
        Console.WriteLine($"POSTFLIGHT_{check.Key}={Convert.ToInt32(await command.ExecuteScalarAsync())}");
    }
}
static async Task RunRefreshTokenPrecheckAsync(OracleConnection c)
{
    var checks = new Dictionary<string, string>
    {
        ["TOTAL"] = "SELECT COUNT(*) FROM RL_REFRESH_TOKENS",
        ["PLAINTEXT"] = "SELECT COUNT(*) FROM RL_REFRESH_TOKENS WHERE LENGTH(RFT_TOKEN) <> 64 OR NOT REGEXP_LIKE(RFT_TOKEN, '^[0-9A-Fa-f]{64}$')",
        ["HASHED"] = "SELECT COUNT(*) FROM RL_REFRESH_TOKENS WHERE LENGTH(RFT_TOKEN) = 64 AND REGEXP_LIKE(RFT_TOKEN, '^[0-9A-Fa-f]{64}$')",
        ["EXPIRED"] = "SELECT COUNT(*) FROM RL_REFRESH_TOKENS WHERE RFT_EXPIRA <= SYSDATE",
        ["REVOKED"] = "SELECT COUNT(*) FROM RL_REFRESH_TOKENS WHERE RFT_REVOCADO = 1",
        ["ACTIVE"] = "SELECT COUNT(*) FROM RL_REFRESH_TOKENS WHERE RFT_REVOCADO = 0 AND RFT_EXPIRA > SYSDATE",
        ["REQUIRES_MIGRATION"] = "SELECT COUNT(*) FROM RL_REFRESH_TOKENS WHERE RFT_TOKEN IS NOT NULL AND (LENGTH(RFT_TOKEN) <> 64 OR NOT REGEXP_LIKE(RFT_TOKEN, '^[0-9A-Fa-f]{64}$'))"
    };
    foreach (var check in checks)
    {
        await using var command = c.CreateCommand();
        command.CommandTimeout = 60;
        command.CommandText = check.Value;
        Console.WriteLine($"REFRESH_TOKEN_PRECHECK_{check.Key}={Convert.ToInt32(await command.ExecuteScalarAsync())}");
    }
}
record FieldInfo(string Key,string Type);
record RuleInfo(string Algorithm,int Active);
