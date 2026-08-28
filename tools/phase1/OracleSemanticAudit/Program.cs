using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;

const string settingsPath = "backend/RL.API/appsettings.json";
var settings = JObject.Parse(File.ReadAllText(settingsPath));
var connectionString = (string?)settings["ConnectionStrings"]?["OracleDB"]
    ?? throw new InvalidOperationException("Oracle connection configuration is incomplete.");

connectionString += ";Connection Timeout=30";
await using var connection = new OracleConnection(connectionString);
await connection.OpenAsync();

var catalogCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var catalogIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var ruleKeys = new Dictionary<string, RuleInfo>(StringComparer.OrdinalIgnoreCase);
await LoadCatalogsAsync(connection, catalogCodes, catalogIds);
await LoadRulesAsync(connection, ruleKeys);

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
    command.CommandText = "SELECT VER_ID, VER_FAMILIA_ID, VER_VERSION, VER_JSON, VER_HASH, VER_ESTADO, VER_VIGENTE FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID IN (24,27,28,53) ORDER BY VER_ID";
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
            ValidateFormulas(formulas, fields, id, errors);
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

Console.WriteLine($"HASH_CHECKED_FULL={fullHash}");
Console.WriteLine($"HASH_INVALID={hashInvalid}");
Console.WriteLine($"HASH_UNCHECKABLE={hashUncheckable}");
Console.WriteLine($"VERSIONS_INSPECTED={versionCount}");
foreach (var key in new[] { "CATALOG_BROKEN_REFERENCE", "FORMULA_SYNTAX_INVALID", "FORMULA_OPERATOR_UNSUPPORTED", "FORMULA_FUNCTION_UNSUPPORTED", "FORMULA_UNKNOWN_REFERENCE", "FORMULA_CYCLE", "RULE_BROKEN_REFERENCE" })
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
static void ValidateFormulas(Dictionary<string,string> formulas, Dictionary<string,FieldInfo> fields, long id, HashSet<string> errors)
{
    foreach (var pair in formulas)
    {
        var expression = pair.Value;
        var functionMatches = System.Text.RegularExpressions.Regex.Matches(expression, @"\b[A-Za-z_]\w*\s*\(");
        if (functionMatches.Count > 0)
        {
            errors.Add("FORMULA_FUNCTION_UNSUPPORTED");
            Console.WriteLine($"FINDING=H3; VER_ID={id}; FIELD={pair.Key}; FORMULA={expression}; CLASSIFICATION=INCONSISTENCIA_DATOS; CAUSE=FUNCTION_NOT_SUPPORTED_BY_RUNTIME");
        }
        if (expression.Any(ch => "^%><=&|!".Contains(ch)))
        {
            errors.Add("FORMULA_OPERATOR_UNSUPPORTED");
            Console.WriteLine($"FINDING=H3; VER_ID={id}; FIELD={pair.Key}; FORMULA={expression}; CLASSIFICATION=INCONSISTENCIA_DATOS; CAUSE=OPERATOR_NOT_SUPPORTED_BY_RUNTIME");
        }
        if (functionMatches.Count > 0 || expression.Any(ch => "^%><=&|!".Contains(ch))) continue;
        var depth = 0; foreach (var ch in expression) { if (ch == '(') depth++; if (ch == ')' && --depth < 0) { errors.Add("FORMULA_SYNTAX_INVALID"); break; } } if (depth != 0) { errors.Add("FORMULA_SYNTAX_INVALID"); continue; }
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(expression, @"\b[A-Za-z_]\w*\b"))
        {
            var name = match.Value; if (new[] { "VRI", "VRR" }.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
            if (expression.IndexOf(name + "(", StringComparison.OrdinalIgnoreCase) >= 0) { errors.Add("FORMULA_FUNCTION_UNSUPPORTED"); continue; }
            if (!fields.ContainsKey(name)) errors.Add("FORMULA_UNKNOWN_REFERENCE");
        }
    }
    foreach (var key in formulas.Keys) if (HasCycle(key, formulas, new HashSet<string>(StringComparer.OrdinalIgnoreCase))) errors.Add("FORMULA_CYCLE");
}
static bool HasCycle(string key, Dictionary<string,string> formulas, HashSet<string> path)
{
    if (!path.Add(key)) return true; if (!formulas.TryGetValue(key, out var expression)) return false;
    foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(expression, @"\b[A-Za-z_]\w*\b")) if (formulas.ContainsKey(match.Value) && HasCycle(match.Value, formulas, new HashSet<string>(path, StringComparer.OrdinalIgnoreCase))) return true;
    return false;
}
static async Task LoadCatalogsAsync(OracleConnection c, HashSet<string> codes, HashSet<string> ids) { await using var cmd=c.CreateCommand();cmd.CommandTimeout=60;cmd.CommandText="SELECT CAT_ID,CAT_CODIGO FROM RL_MR_CATALOGOS";await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync()){ids.Add(Convert.ToString(r.GetValue(0))!);codes.Add(Convert.ToString(r.GetValue(1))!);} }
static async Task LoadRulesAsync(OracleConnection c, Dictionary<string,RuleInfo> rules) { await using var cmd=c.CreateCommand();cmd.CommandTimeout=60;cmd.CommandText="SELECT REG_CODIGO,REG_VERSION,REG_ALGORITMO_ID,REG_ACTIVA FROM RL_MR_REGLAS_CALCULO";await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync()){var code=Convert.ToString(r.GetValue(0))!;var version=Convert.ToString(r.GetValue(1))!;rules[$"{code}|{version}"]=new RuleInfo(Convert.ToString(r.GetValue(2))!,Convert.ToInt32(r.GetValue(3)));} }
record FieldInfo(string Key,string Type);
record RuleInfo(string Algorithm,int Active);
