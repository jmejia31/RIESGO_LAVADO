using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Block5WorkbookFormulaOracle;

internal static class Program
{
    private const string OfficialWorkbookSha = "5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385";
    internal static readonly int[] FormulaFields = [12, 13, 22, 23, 26, 27, 30, 31, 33, 34, 35, 36, 37, 38, .. Enumerable.Range(50, 20)];

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 1 && args[0] == "--self-test") return SelfTest();
            if (args.Length != 2 || args[0] != "--workbook")
            {
                Console.Error.WriteLine("Usage: Block5WorkbookFormulaOracle --workbook <xlsx> [--self-test]");
                return 2;
            }

            string path = Path.GetFullPath(args[1]);
            string sha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
            if (!StringComparer.Ordinal.Equals(sha, OfficialWorkbookSha))
                throw new InvalidDataException($"Workbook SHA mismatch: {sha}");

            WorkbookFormulaOracle oracle = WorkbookFormulaOracle.Load(path);
            IReadOnlyList<WorkbookFormulaResult> results = oracle.EvaluateFormulaCells();
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                workbookSha256 = sha,
                formulaCellsEvaluated = results.Count,
                physicalFormulaCells = oracle.PhysicalFormulaCount,
                tableCalculatedFormulaCells = oracle.TableCalculatedFormulaCount,
                distinctFunctions = oracle.FunctionInventory,
                functionUsage = results.SelectMany(result => result.FunctionCalls).GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase),
                tables = oracle.TableLocations,
                tableValues = oracle.TableValues,
                results
            }, JsonOptions));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ORACLE_ERROR: {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static int SelfTest()
    {
        var checks = new (string Formula, Value Expected)[]
        {
            ("1+2*3", Value.Number(7)),
            ("IF(1=1,ROUND(2.5,0),0)", Value.Number(3)),
            ("ROUND(-2.5,0)", Value.Number(-3)),
            ("ROUNDDOWN(-2.9,0)", Value.Number(-2)),
            ("MOD(-2.5,1)", Value.Number(0.5d)),
            ("IFERROR(1/0,\"\")", Value.Text(string.Empty)),
            ("AND(TRUE,NOT(FALSE))", Value.Bool(true)),
            ("OR(FALSE,2>1)", Value.Bool(true))
        };
        foreach ((string formula, Value expected) in checks)
        {
            Value actual = new Evaluator().EvaluateExpression(formula);
            if (!actual.Equals(expected)) throw new InvalidOperationException($"{formula}: expected {expected}, got {actual}");
        }
        Console.WriteLine($"ORACLE_SELF_TESTS={checks.Length}/{checks.Length}");
        return 0;
    }
}

internal sealed class Workbook
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    public Dictionary<string, Sheet> Sheets { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Table> Tables { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string[][] ReadTableValues(Table table)
    {
        Match match = Regex.Match(table.Range, @"^(?<startColumn>[A-Z]+)(?<startRow>\d+):(?<endColumn>[A-Z]+)(?<endRow>\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success) throw new InvalidDataException($"Unsupported table range {table.Name}: {table.Range}");
        int startColumn = ColumnNumber(match.Groups["startColumn"].Value);
        int endColumn = ColumnNumber(match.Groups["endColumn"].Value);
        int startRow = int.Parse(match.Groups["startRow"].Value, CultureInfo.InvariantCulture);
        int endRow = int.Parse(match.Groups["endRow"].Value, CultureInfo.InvariantCulture);
        Sheet sheet = Sheets[table.Sheet];
        return Enumerable.Range(startRow, endRow - startRow + 1)
            .Select(row => Enumerable.Range(startColumn, endColumn - startColumn + 1).Select(column => sheet.Get(row, column).Display).ToArray())
            .ToArray();
    }

    private static int ColumnNumber(string name)
    {
        int value = 0;
        foreach (char character in name.ToUpperInvariant()) value = checked(value * 26 + character - 'A' + 1);
        return value;
    }

    public static Workbook Read(string path)
    {
        using ZipArchive archive = ZipFile.OpenRead(path);
        XDocument book = ReadXml(archive, "xl/workbook.xml");
        XDocument rels = ReadXml(archive, "xl/_rels/workbook.xml.rels");
        Dictionary<string, string> targets = rels.Root!.Elements().ToDictionary(item => (string)item.Attribute("Id")!, item => Resolve("xl/workbook.xml", (string)item.Attribute("Target")!));
        var shared = ReadXml(archive, "xl/sharedStrings.xml").Root!.Elements(Main + "si")
            .Select(item => string.Concat(item.Descendants(Main + "t").Select(text => text.Value))).ToArray();
        var workbook = new Workbook();
        foreach (XElement element in book.Root!.Element(Main + "sheets")!.Elements(Main + "sheet"))
        {
            string name = (string)element.Attribute("name")!;
            string target = targets[(string)element.Attribute(Rel + "id")!];
            XDocument doc = ReadXml(archive, target);
            var sheet = new Sheet(name);
            foreach (XElement rowElement in doc.Descendants(Main + "sheetData").Elements(Main + "row"))
            {
                int row = (int)rowElement.Attribute("r")!;
                foreach (XElement cellElement in rowElement.Elements(Main + "c"))
                {
                    string address = (string)cellElement.Attribute("r")!;
                    string type = (string?)cellElement.Attribute("t") ?? "n";
                    XElement? valueElement = cellElement.Element(Main + "v");
                    string raw = valueElement?.Value ?? string.Empty;
                    string? formula = cellElement.Element(Main + "f")?.Value;
                    object? value = type switch
                    {
                        "s" when raw.Length > 0 => shared[int.Parse(raw, CultureInfo.InvariantCulture)],
                        "inlineStr" => string.Concat(cellElement.Descendants(Main + "t").Select(text => text.Value)),
                        "b" => raw == "1",
                        _ when double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) => number,
                        _ => raw
                    };
                    sheet.Cells[address] = new Cell(address, formula, value, type);
                }
            }
            workbook.Sheets.Add(name, sheet);
        }

        foreach (ZipArchiveEntry entry in archive.Entries.Where(item => Regex.IsMatch(item.FullName, @"^xl/tables/table\d+\.xml$", RegexOptions.CultureInvariant)))
        {
            XDocument tableDoc = ReadXml(archive, entry.FullName);
            XElement root = tableDoc.Root!;
            string name = (string)root.Attribute("name")!;
            string range = (string)root.Attribute("ref")!;
            XElement[] columns = root.Element(Main + "tableColumns")!.Elements(Main + "tableColumn").ToArray();
            // Table-to-sheet relationship is established via worksheet tableParts; table IDs are globally unique.
            string? sheetName = FindSheetForTable(archive, workbook.Sheets.Keys, entry.FullName, (string)root.Attribute("id")!);
            if (sheetName is null) throw new InvalidDataException($"Could not resolve sheet for table {name}.");
            workbook.Tables.Add(name, new Table(name, sheetName, range,
                columns.Select(column => (string)column.Attribute("name")!).ToArray(),
                columns.Select(column => column.Element(Main + "calculatedColumnFormula")?.Value).ToArray()));
        }
        return workbook;
    }

    private static string? FindSheetForTable(ZipArchive archive, IEnumerable<string> sheetNames, string tablePath, string id)
    {
        string tableFile = Path.GetFileName(tablePath);
        string tableRelId = "";
        foreach (ZipArchiveEntry entry in archive.Entries.Where(item => Regex.IsMatch(item.FullName, @"^xl/worksheets/_rels/sheet\d+\.xml\.rels$", RegexOptions.CultureInvariant)))
        {
            XDocument rels = ReadXml(archive, entry.FullName);
            foreach (XElement rel in rels.Root!.Elements())
            {
                if (((string?)rel.Attribute("Target"))?.EndsWith(tableFile, StringComparison.OrdinalIgnoreCase) == true)
                    tableRelId = (string)rel.Attribute("Id")!;
            }
            if (tableRelId.Length == 0) continue;
            string sheetFile = entry.FullName.Replace("_rels/", "", StringComparison.Ordinal).Replace(".rels", "", StringComparison.Ordinal);
            XDocument sheet = ReadXml(archive, sheetFile);
            XElement? part = sheet.Descendants(Main + "tablePart").SingleOrDefault(item => (string?)item.Attribute(Rel + "id") == tableRelId);
            if (part is not null)
            {
                XDocument book = ReadXml(archive, "xl/workbook.xml");
                XDocument bookRels = ReadXml(archive, "xl/_rels/workbook.xml.rels");
                string rid = "rId" + Path.GetFileNameWithoutExtension(sheetFile).Replace("sheet", "", StringComparison.Ordinal);
                string? target = bookRels.Root!.Elements().FirstOrDefault(item => (string?)item.Attribute("Id") == rid)?.Attribute("Target")?.Value;
                string? normalizedTarget = target is null ? null : Resolve("xl/workbook.xml", target);
                XElement? sheetElement = book.Root!.Element(Main + "sheets")!.Elements(Main + "sheet")
                    .FirstOrDefault(item => normalizedTarget is not null && normalizedTarget.Equals(Resolve("xl/workbook.xml", bookRels.Root.Elements().First(rel => (string?)rel.Attribute("Id") == (string?)item.Attribute(Rel + "id")).Attribute("Target")!.Value), StringComparison.OrdinalIgnoreCase));
                if (sheetElement is not null) return (string)sheetElement.Attribute("name")!;
            }
        }
        return null;
    }

    private static string Resolve(string source, string target)
    {
        if (target.StartsWith('/')) return target.TrimStart('/');
        var parts = new List<string>();
        foreach (string part in (Path.GetDirectoryName(source)!.Replace('\\', '/') + "/" + target.Replace('\\', '/')).Split('/'))
        {
            if (part is "" or ".") continue;
            if (part == "..") { if (parts.Count > 0) parts.RemoveAt(parts.Count - 1); continue; }
            parts.Add(part);
        }
        return string.Join('/', parts);
    }

    private static XDocument ReadXml(ZipArchive archive, string path)
    {
        ZipArchiveEntry entry = archive.GetEntry(path) ?? throw new InvalidDataException($"Missing XLSX part {path}.");
        using Stream stream = entry.Open();
        return XDocument.Load(stream);
    }
}

internal sealed class Sheet(string name)
{
    public string Name { get; } = name;
    public Dictionary<string, Cell> Cells { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Cell Get(int row, int column) => Get($"{ColumnName(column)}{row}");
    public Cell Get(string address) => Cells.GetValueOrDefault(address) ?? new Cell(address, null, null, "blank");
    public static string ColumnName(int column)
    {
        var result = new StringBuilder();
        while (column > 0) { column--; result.Insert(0, (char)('A' + column % 26)); column /= 26; }
        return result.ToString();
    }
}

internal sealed record Cell(string Address, string? Formula, object? RawValue, string Type)
{
    public string Display => RawValue switch { null => "", decimal n => n.ToString(CultureInfo.InvariantCulture), bool b => b ? "TRUE" : "FALSE", _ => Convert.ToString(RawValue, CultureInfo.InvariantCulture) ?? "" };
}

internal sealed record Table(string Name, string Sheet, string Range, string[] Columns, string?[] ColumnFormulas);

internal enum ValueKind { Blank, Number, Text, Boolean, Error, Range, Table }

internal sealed class Value : IEquatable<Value>
{
    public ValueKind Kind { get; }
    public double Numeric { get; }
    public string String { get; }
    public bool Boolean { get; }
    public IReadOnlyList<IReadOnlyList<Value>>? Matrix { get; }
    private Value(ValueKind kind, double numeric = 0, string text = "", bool boolean = false, IReadOnlyList<IReadOnlyList<Value>>? matrix = null)
    { Kind = kind; Numeric = numeric; String = text; Boolean = boolean; Matrix = matrix; }
    public static Value Blank { get; } = new(ValueKind.Blank);
    public static Value Number(double number) => new(ValueKind.Number, number);
    public static Value Text(string text) => new(ValueKind.Text, text: text);
    public static Value Bool(bool value) => new(ValueKind.Boolean, boolean: value);
    public static Value Error(string error) => new(ValueKind.Error, text: error);
    public static Value Range(IReadOnlyList<IReadOnlyList<Value>> values) => new(ValueKind.Range, matrix: values);
    public static Value Table(string name) => new(ValueKind.Table, text: name);
    public bool IsError => Kind == ValueKind.Error;
    public bool IsBlankLike => Kind == ValueKind.Blank || Kind == ValueKind.Text && String.Length == 0;
    public double AsNumber() => Kind switch { ValueKind.Number => Numeric, ValueKind.Boolean => Boolean ? 1 : 0, ValueKind.Blank => 0, ValueKind.Text when double.TryParse(String, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) => n, _ => throw new FormulaException("#VALUE!") };
    public bool AsBoolean() => Kind switch { ValueKind.Boolean => Boolean, ValueKind.Number => Numeric != 0, ValueKind.Blank => false, ValueKind.Text => String.Length > 0, _ => throw new FormulaException("#VALUE!") };
    public string AsText() => Kind switch { ValueKind.Text => String, ValueKind.Blank => "", ValueKind.Number => Numeric.ToString("R", CultureInfo.InvariantCulture), ValueKind.Boolean => Boolean ? "TRUE" : "FALSE", _ => throw new FormulaException("#VALUE!") };
    public object? ToJson() => Kind switch { ValueKind.Blank => null, ValueKind.Number => Numeric, ValueKind.Text => String, ValueKind.Boolean => Boolean, ValueKind.Error => new { error = String }, _ => new { reference = String } };
    public bool Equals(Value? other) => other is not null && Kind == other.Kind && (Kind switch { ValueKind.Number => Numeric == other.Numeric, ValueKind.Text or ValueKind.Error or ValueKind.Table => String == other.String, ValueKind.Boolean => Boolean == other.Boolean, ValueKind.Blank => true, _ => false });
    public override bool Equals(object? obj) => obj is Value other && Equals(other);
    public override int GetHashCode() => Kind switch { ValueKind.Number => HashCode.Combine(Kind, Numeric), ValueKind.Text or ValueKind.Error or ValueKind.Table => HashCode.Combine(Kind, String), ValueKind.Boolean => HashCode.Combine(Kind, Boolean), _ => (int)Kind };
    public override string ToString() => Kind switch { ValueKind.Number => Numeric.ToString("R", CultureInfo.InvariantCulture), ValueKind.Text => $"\"{String}\"", ValueKind.Boolean => Boolean ? "TRUE" : "FALSE", ValueKind.Blank => "BLANK", _ => $"{Kind}:{String}" };
}

internal sealed class FormulaException(string error) : Exception(error);

public sealed record WorkbookFormulaResult(string RiskCode, string RiskNo, int FieldNumber, string FieldLabel, string Cell, string SourceKind, string Formula, object? CachedValue, object? ExpectedValue, bool IsError, IReadOnlyList<string> Functions, IReadOnlyList<string> FunctionCalls, IReadOnlyList<string> References, IReadOnlyDictionary<string, string> SourceInputs);

/// <summary>Independent OpenXML formula evaluator; it has no reference to the application/backend assemblies.</summary>
public sealed class WorkbookFormulaOracle
{
    private readonly Workbook _workbook;
    private readonly Sheet _matrix;
    private WorkbookFormulaOracle(Workbook workbook)
    {
        _workbook = workbook;
        _matrix = workbook.Sheets.Values.Single(sheet => sheet.Name == "Matriz Consolidada");
    }

    public static WorkbookFormulaOracle Load(string path) => new(Workbook.Read(Path.GetFullPath(path)));
    public string GetText(int row, int column) => _matrix.Get(row, column).Display;
    public object? GetRawValue(int row, int column) => _matrix.Get(row, column).RawValue;
    public bool HasFormula(int row, int column) => _matrix.Get(row, column).Formula is not null;
    public string? GetFormula(int row, int column) => _matrix.Get(row, column).Formula ?? _workbook.Tables.Values
        .Where(table => table.Name.Equals("Matriz_Riesgos", StringComparison.OrdinalIgnoreCase))
        .Select(table => table.ColumnFormulas.ElementAtOrDefault(column - 1)).FirstOrDefault(formula => formula is not null);
    public int PhysicalFormulaCount => Program.FormulaFields.Sum(field => Enumerable.Range(2, 59).Count(row => _matrix.Get(row, field).Formula is not null));
    public int TableCalculatedFormulaCount => Program.FormulaFields.Sum(field => Enumerable.Range(2, 59).Count(row => _matrix.Get(row, field).Formula is null && GetFormula(row, field) is not null));
    public IReadOnlyList<WorkbookFormulaResult> EvaluateFormulaCells()
    {
        var results = new List<WorkbookFormulaResult>();
        foreach (int field in Program.FormulaFields)
            for (int row = 2; row <= 60; row++)
            {
                Cell cell = _matrix.Get(row, field);
                string? formula = GetFormula(row, field);
                if (formula is null) continue;
                var evaluator = new Evaluator(_workbook, _matrix, row);
                Value expected = cell.Formula is null ? evaluator.EvaluateExpression(formula) : evaluator.EvaluateCell(_matrix.Name, cell.Address);
                results.Add(new(_matrix.Get(row, 2).Display, _matrix.Get(row, 1).Display, field, _matrix.Get(1, field).Display, cell.Address, cell.Formula is null ? "TABLE_CALCULATED_FORMULA" : "PHYSICAL_FORMULA", formula, cell.RawValue, expected.ToJson(), expected.IsError, evaluator.Functions.Order().ToArray(), ExtractFunctionCalls(formula), ExtractReferences(formula), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["frecuencia"] = _matrix.Get(row, 10).Display, ["impacto"] = _matrix.Get(row, 11).Display,
                    ["control_preventivo"] = _matrix.Get(row, 20).Display, ["escala_preventivo"] = _matrix.Get(row, 21).Display,
                    ["control_detectivo"] = _matrix.Get(row, 24).Display, ["escala_detectivo"] = _matrix.Get(row, 25).Display,
                    ["control_correctivo"] = _matrix.Get(row, 28).Display, ["escala_correctivo"] = _matrix.Get(row, 29).Display
                }));
            }
        return results;
    }
    public IReadOnlyDictionary<string, string> TableLocations => _workbook.Tables.Values.ToDictionary(table => table.Name, table => $"{table.Sheet}!{table.Range}", StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string[][]> TableValues => _workbook.Tables.Values.ToDictionary(
        table => table.Name,
        table => _workbook.ReadTableValues(table),
        StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<string> FunctionInventory => EvaluateFormulaCells().SelectMany(result => result.Functions).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
    private static string[] ExtractFunctionCalls(string formula)
    {
        string expression = Regex.Replace(formula, @"Matriz_Riesgos\[\[#This Row\],\[.*?\]\]", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return Regex.Matches(expression, @"\b([A-Z][A-Z0-9.]*)\s*\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            .Select(match => match.Groups[1].Value.ToUpperInvariant()).ToArray();
    }
    private static string[] ExtractReferences(string formula)
    {
        var refs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(formula, @"Matriz_Riesgos\[\[#This Row\],\[(.*?)\]\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            refs.Add("Matriz_Riesgos[" + match.Groups[1].Value + "]");
        string withoutStructuredRefs = Regex.Replace(formula, @"Matriz_Riesgos\[\[#This Row\],\[.*?\]\]", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        foreach (Match match in Regex.Matches(withoutStructuredRefs, @"(?:'[^']+'|[A-Za-z_][A-Za-z0-9 _]*)!\$?[A-Z]{1,3}\$?\d+(?::\$?[A-Z]{1,3}\$?\d+)?|\$?[A-Z]{1,3}\$?\d+(?::\$?[A-Z]{1,3}\$?\d+)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            refs.Add(match.Value);
        foreach (Match match in Regex.Matches(withoutStructuredRefs, @"\b[A-Z_][A-Z0-9_]*\[(?:#All)?\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            refs.Add(match.Value);
        return refs.ToArray();
    }
}

internal sealed class Evaluator
{
    private readonly Workbook? _workbook;
    private readonly Sheet? _defaultSheet;
    private readonly int _currentRow;
    private readonly HashSet<string> _active = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Functions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Evaluator() { }
    public Evaluator(Workbook workbook, Sheet sheet, int currentRow) { _workbook = workbook; _defaultSheet = sheet; _currentRow = currentRow; }
    public Value EvaluateExpression(string expression) => new Parser(this, expression).Parse();
    public Value EvaluateCell(string sheetName, string address)
    {
        if (_workbook is null) throw new InvalidOperationException("Workbook context is required.");
        string key = sheetName + "!" + address;
        if (!_active.Add(key)) throw new FormulaException("#CYCLE!");
        try
        {
            Sheet sheet = _workbook.Sheets[sheetName];
            Cell cell = sheet.Get(address);
            if (cell.Formula is not null)
            {
                try { return EvaluateExpression(cell.Formula); }
                catch (FormulaException ex) { throw new FormulaException($"{key} [{cell.Formula}]: {ex.Message}"); }
            }
            return FromCell(cell);
        }
        finally { _active.Remove(key); }
    }
    public Value ResolveReference(string reference)
    {
        if (_workbook is null || _defaultSheet is null) throw new FormulaException("#REF!");
        Match structured = Regex.Match(reference, @"^Matriz_Riesgos\[\[#This Row\],\[(.*)\]\]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (structured.Success)
        {
            string header = structured.Groups[1].Value.Replace("]", "", StringComparison.Ordinal);
            int col = FindHeader(header);
            return EvaluateCell(_defaultSheet.Name, Sheet.ColumnName(col) + _currentRow);
        }
        Match table = Regex.Match(reference, @"^(?<name>[A-Za-z0-9_]+)\[(?:#All)?\]$", RegexOptions.CultureInvariant);
        if (table.Success) return Value.Table(table.Groups["name"].Value);
        if (reference.Contains(':', StringComparison.Ordinal)) return ResolveRange(reference);
        Match cross = Regex.Match(reference, @"^(?:'(?<quoted>[^']+)'|(?<plain>[^!]+))!(?<cell>\$?[A-Z]{1,3}\$?\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        string sheet = cross.Success ? (cross.Groups["quoted"].Success ? cross.Groups["quoted"].Value : cross.Groups["plain"].Value) : _defaultSheet.Name;
        string address = cross.Success ? cross.Groups["cell"].Value.Replace("$", "", StringComparison.Ordinal) : reference.Replace("$", "", StringComparison.Ordinal);
        return EvaluateCell(sheet, address);
    }
    private Value ResolveRange(string reference)
    {
        Match crossRange = Regex.Match(reference, @"^(?:'(?<quoted>[^']+)'|(?<plain>[^!]+))!(?<first>\$?[A-Z]{1,3}\$?\d+):(?<last>\$?[A-Z]{1,3}\$?\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        string sheetName = crossRange.Success ? (crossRange.Groups["quoted"].Success ? crossRange.Groups["quoted"].Value : crossRange.Groups["plain"].Value) : _defaultSheet!.Name;
        string first = crossRange.Success ? crossRange.Groups["first"].Value : reference.Split(':')[0];
        string last = crossRange.Success ? crossRange.Groups["last"].Value : reference.Split(':')[1];
        (int r1, int c1) = ParseAddress(first);
        (int r2, int c2) = ParseAddress(last);
        var rows = new List<IReadOnlyList<Value>>();
        for (int row = Math.Min(r1, r2); row <= Math.Max(r1, r2); row++)
        {
            var values = new List<Value>();
            for (int col = Math.Min(c1, c2); col <= Math.Max(c1, c2); col++) values.Add(EvaluateCell(sheetName, Sheet.ColumnName(col) + row));
            rows.Add(values);
        }
        return Value.Range(rows);
    }
    public Value GetTableRange(string name)
    {
        if (_workbook is null || !_workbook.Tables.TryGetValue(name, out Table? table)) throw new FormulaException("#NAME?");
        (int r1, int c1, int r2, int c2) = ParseRange(table.Range);
        var rows = new List<IReadOnlyList<Value>>();
        for (int r = r1 + 1; r <= r2; r++)
        {
            var row = new List<Value>();
            for (int c = c1; c <= c2; c++) row.Add(EvaluateCell(table.Sheet, Sheet.ColumnName(c) + r));
            rows.Add(row);
        }
        return Value.Range(rows);
    }
    public Value Call(string name, IReadOnlyList<Value> args)
    {
        Functions.Add(name.ToUpperInvariant());
        try
        {
            return name.ToUpperInvariant() switch
            {
                "IF" when args.Count is 2 or 3 => args[0].AsBoolean() ? args[1] : args.Count == 3 ? args[2] : Value.Bool(false),
                "IFERROR" when args.Count == 2 => args[0].IsError ? args[1] : args[0],
                "AND" => Value.Bool(args.All(arg => arg.AsBoolean())),
                "OR" => Value.Bool(args.Any(arg => arg.AsBoolean())),
                "NOT" when args.Count == 1 => Value.Bool(!args[0].AsBoolean()),
                "MAX" when args.Count > 0 => Value.Number(args.Select(arg => arg.AsNumber()).Max()),
                "MIN" when args.Count > 0 => Value.Number(args.Select(arg => arg.AsNumber()).Min()),
                "MOD" when args.Count == 2 => Mod(args[0].AsNumber(), args[1].AsNumber()),
                "ROUND" when args.Count == 2 => ExcelRound(args[0].AsNumber(), args[1].AsNumber()),
                "ROUNDDOWN" when args.Count == 2 => TruncateDigits(args[0].AsNumber(), args[1].AsNumber()),
                "VLOOKUP" when args.Count == 4 => VLookup(args),
                _ => Value.Error("#NAME?")
            };
        }
        catch (FormulaException ex) { return Value.Error(ex.Message); }
        catch (DivideByZeroException) { return Value.Error("#DIV/0!"); }
        catch (OverflowException) { return Value.Error("#NUM!"); }
    }
    private Value VLookup(IReadOnlyList<Value> args)
    {
        Value lookup = args[0];
        Value range = args[1].Kind == ValueKind.Table ? GetTableRange(args[1].String) : args[1];
        if (range.Matrix is null) return Value.Error("#REF!");
        int index = checked((int)args[2].AsNumber()) - 1;
        bool approximate = args[3].AsBoolean();
        if (index < 0 || range.Matrix.Count == 0 || index >= range.Matrix[0].Count) return Value.Error("#REF!");
        foreach (IReadOnlyList<Value> row in range.Matrix)
        {
            if (Compare(row[0], lookup) == 0) return row[index];
        }
        if (approximate) return Value.Error("#N/A");
        return Value.Error("#N/A");
    }
    private int FindHeader(string header)
    {
        for (int column = 1; column <= 82; column++)
        {
            string existing = _defaultSheet!.Get(1, column).Display;
            if (StringComparer.OrdinalIgnoreCase.Equals(existing, header)) return column;
        }
        throw new FormulaException("#REF!");
    }
    private static Value FromCell(Cell cell) => cell.RawValue switch
    {
        null => Value.Blank,
        double number => Value.Number(number),
        bool boolean => Value.Bool(boolean),
        string text => Value.Text(text),
        _ => Value.Text(Convert.ToString(cell.RawValue, CultureInfo.InvariantCulture) ?? "")
    };
    private static (int Row, int Column, int EndRow, int EndColumn) ParseRange(string range)
    {
        string[] parts = range.Split(':');
        (int r1, int c1) = ParseAddress(parts[0]);
        (int r2, int c2) = ParseAddress(parts[^1]);
        return (r1, c1, r2, c2);
    }
    private static (int Row, int Column) ParseAddress(string address)
    {
        Match match = Regex.Match(address, @"^\$?([A-Z]+)\$?(\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success) throw new FormulaException("#REF!");
        int col = 0; foreach (char ch in match.Groups[1].Value.ToUpperInvariant()) col = col * 26 + ch - 'A' + 1;
        return (int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), col);
    }
    private static Value Mod(double a, double b)
    {
        if (b == 0) throw new DivideByZeroException();
        double result = a - b * Math.Floor(a / b);
        return Value.Number(result);
    }
    private static Value ExcelRound(double number, double digits)
    {
        int places = checked((int)digits);
        double factor = Math.Pow(10d, Math.Abs(places));
        double rounded = places >= 0
            ? Math.Round(number, places, MidpointRounding.AwayFromZero)
            : Math.Round(number / factor, 0, MidpointRounding.AwayFromZero) * factor;
        return Value.Number(rounded);
    }
    private static Value TruncateDigits(double number, double digits)
    {
        int places = checked((int)digits);
        double factor = Math.Pow(10d, Math.Abs(places));
        return Value.Number(places >= 0 ? Math.Truncate(number * factor) / factor : Math.Truncate(number / factor) * factor);
    }
    private static int Compare(Value left, Value right)
    {
        if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number) return left.Numeric.CompareTo(right.Numeric);
        if (left.IsBlankLike && right.IsBlankLike) return 0;
        if (left.Kind == ValueKind.Text && right.Kind == ValueKind.Text) return StringComparer.OrdinalIgnoreCase.Compare(left.String, right.String);
        return StringComparer.OrdinalIgnoreCase.Compare(left.AsText(), right.AsText());
    }
}

internal sealed class Parser
{
    private readonly Evaluator _evaluator;
    private readonly List<Token> _tokens;
    private int _position;
    public Parser(Evaluator evaluator, string formula) { _evaluator = evaluator; _tokens = Tokenize(formula); }
    public Value Parse() { Value value = ParseComparison(); Expect(TokenKind.End); return value; }
    private Value ParseComparison()
    {
        Value left = ParseConcat();
        while (Peek().Kind == TokenKind.Operator && Peek().Text is "=" or "<>" or ">" or "<" or ">=" or "<=")
        {
            string op = Take().Text; Value right = ParseConcat();
            if (left.IsError) return left;
            if (right.IsError) return right;
            int cmp = Compare(left, right);
            left = Value.Bool(op switch { "=" => cmp == 0, "<>" => cmp != 0, ">" => cmp > 0, "<" => cmp < 0, ">=" => cmp >= 0, _ => cmp <= 0 });
        }
        return left;
    }
    private Value ParseConcat() { Value value = ParseAdd(); while (Match(TokenKind.Operator, "&")) value = Value.Text(value.AsText() + ParseAdd().AsText()); return value; }
    private Value ParseAdd() { Value value = ParseMultiply(); while (Peek().Kind == TokenKind.Operator && Peek().Text is "+" or "-") { string op = Take().Text; Value right = ParseMultiply(); value = Binary(value, right, op); } return value; }
    private Value ParseMultiply() { Value value = ParseUnary(); while (Peek().Kind == TokenKind.Operator && Peek().Text is "*" or "/" or "^") { string op = Take().Text; Value right = ParseUnary(); value = Binary(value, right, op); } return value; }
    private Value ParseUnary() { if (Match(TokenKind.Operator, "+")) return Value.Number(ParseUnary().AsNumber()); if (Match(TokenKind.Operator, "-")) return Value.Number(-ParseUnary().AsNumber()); return ParsePrimary(); }
    private Value ParsePrimary()
    {
        Token token = Take();
        if (token.Kind == TokenKind.Number) return Value.Number(double.Parse(token.Text, CultureInfo.InvariantCulture));
        if (token.Kind == TokenKind.String) return Value.Text(token.Text);
        if (token.Kind == TokenKind.Reference) return _evaluator.ResolveReference(token.Text);
        if (token.Kind == TokenKind.Table) return Value.Table(token.Text[..token.Text.IndexOf('[', StringComparison.Ordinal)]);
        if (token.Kind == TokenKind.Identifier)
        {
            if (token.Text.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) return Value.Bool(true);
            if (token.Text.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) return Value.Bool(false);
            if (!Match(TokenKind.LeftParen)) throw new FormulaException("#NAME?");
            var args = new List<Value>();
            if (!Match(TokenKind.RightParen)) { do { args.Add(ParseComparison()); } while (Match(TokenKind.Comma)); Expect(TokenKind.RightParen); }
            return _evaluator.Call(token.Text, args);
        }
        if (token.Kind == TokenKind.LeftParen) { Value value = ParseComparison(); Expect(TokenKind.RightParen); return value; }
        throw new FormulaException("#VALUE!");
    }
    private static Value Binary(Value a, Value b, string op)
    {
        if (a.IsError) return a;
        if (b.IsError) return b;
        try
        {
            double x = a.AsNumber(), y = b.AsNumber();
            if (op == "/" && y == 0) throw new DivideByZeroException();
            double result = op switch { "+" => x + y, "-" => x - y, "*" => x * y, "/" => x / y, "^" => Math.Pow(x, y), _ => throw new FormulaException("#VALUE!") };
            return double.IsFinite(result) ? Value.Number(result) : Value.Error("#NUM!");
        }
        catch (DivideByZeroException) { return Value.Error("#DIV/0!"); }
        catch (FormulaException ex) { return Value.Error(ex.Message); }
    }
    private bool Match(TokenKind kind, string? text = null) { if (Peek().Kind != kind || text is not null && Peek().Text != text) return false; _position++; return true; }
    private Token Peek() => _tokens[_position];
    private Token Take() => _tokens[_position++];
    private void Expect(TokenKind kind) { if (!Match(kind)) throw new FormulaException("#VALUE!"); }
    private static int Compare(Value a, Value b)
    {
        if (a.IsError || b.IsError) throw new FormulaException("#VALUE!");
        if ((a.Kind is ValueKind.Number or ValueKind.Boolean or ValueKind.Blank) && (b.Kind is ValueKind.Number or ValueKind.Boolean or ValueKind.Blank)) return a.AsNumber().CompareTo(b.AsNumber());
        if (a.IsBlankLike && b.IsBlankLike) return 0;
        if (a.Kind == ValueKind.Text && b.Kind == ValueKind.Text) return StringComparer.OrdinalIgnoreCase.Compare(a.String, b.String);
        return StringComparer.OrdinalIgnoreCase.Compare(a.AsText(), b.AsText());
    }
    private static List<Token> Tokenize(string formula)
    {
        var tokens = new List<Token>(); int i = 0;
        while (i < formula.Length)
        {
            char c = formula[i]; if (char.IsWhiteSpace(c)) { i++; continue; }
            Match range = Regex.Match(formula[i..], @"^(?:'[^']+'|[A-Za-z_][A-Za-z0-9 _]*)!\$?[A-Z]{1,3}\$?\d+:\$?[A-Z]{1,3}\$?\d+|^\$?[A-Z]{1,3}\$?\d+:\$?[A-Z]{1,3}\$?\d+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (range.Success) { tokens.Add(new(TokenKind.Reference, range.Value)); i += range.Length; continue; }
            if (c == '"') { var text = new StringBuilder(); i++; while (i < formula.Length) { if (formula[i] == '"') { if (i + 1 < formula.Length && formula[i + 1] == '"') { text.Append('"'); i += 2; continue; } i++; break; } text.Append(formula[i++]); } tokens.Add(new(TokenKind.String, text.ToString())); continue; }
            if (c == '\'') { var text = new StringBuilder(); i++; while (i < formula.Length) { if (formula[i] == '\'') { if (i + 1 < formula.Length && formula[i + 1] == '\'') { text.Append('\''); i += 2; continue; } i++; break; } text.Append(formula[i++]); } if (i < formula.Length && formula[i] == '!') i++; int start = i; while (i < formula.Length && (char.IsLetterOrDigit(formula[i]) || formula[i] == '$')) i++; tokens.Add(new(TokenKind.Reference, "'" + text + "'!" + formula[start..i])); continue; }
            if (char.IsDigit(c) || c == '.') { int start = i++; while (i < formula.Length && (char.IsDigit(formula[i]) || formula[i] == '.')) i++; tokens.Add(new(TokenKind.Number, formula[start..i])); continue; }
            if (char.IsLetter(c) || c == '$')
            {
                int start = i;
                // Excel structured references include nested brackets and may contain spaces and punctuation.
                if (formula.AsSpan(i).StartsWith("Matriz_Riesgos[[#This Row],[", StringComparison.OrdinalIgnoreCase))
                { int end = formula.IndexOf("]]", i, StringComparison.Ordinal); if (end < 0) throw new FormulaException("#VALUE!"); i = end + 2; tokens.Add(new(TokenKind.Reference, formula[start..i])); continue; }
                while (i < formula.Length && (char.IsLetterOrDigit(formula[i]) || formula[i] is '_' or '.' or '$')) i++;
                if (i < formula.Length && formula[i] == '[') { int end = formula.IndexOf(']', i); if (end < 0) throw new FormulaException("#VALUE!"); while (end + 1 < formula.Length && formula[end + 1] == ']') { end = formula.IndexOf(']', end + 2); if (end < 0) throw new FormulaException("#VALUE!"); } i = end + 1; tokens.Add(new(TokenKind.Table, formula[start..i])); continue; }
                if (i < formula.Length && formula[i] == '!') { i++; int startAddress = i; while (i < formula.Length && (char.IsLetterOrDigit(formula[i]) || formula[i] == '$')) i++; tokens.Add(new(TokenKind.Reference, formula[start..i])); continue; }
                string word = formula[start..i];
                if (Regex.IsMatch(word, @"^\$?[A-Z]{1,3}\$?\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) tokens.Add(new(TokenKind.Reference, word));
                else tokens.Add(new(TokenKind.Identifier, word));
                continue;
            }
            if (c == ',') { tokens.Add(new(TokenKind.Comma, ",")); i++; continue; }
            if (c == '(') { tokens.Add(new(TokenKind.LeftParen, "(")); i++; continue; }
            if (c == ')') { tokens.Add(new(TokenKind.RightParen, ")")); i++; continue; }
            if (i + 1 < formula.Length && formula.Substring(i, 2) is "<>" or ">=" or "<=") { tokens.Add(new(TokenKind.Operator, formula.Substring(i, 2))); i += 2; continue; }
            if ("+-*/^&=<>".Contains(c, StringComparison.Ordinal)) { tokens.Add(new(TokenKind.Operator, c.ToString())); i++; continue; }
            throw new FormulaException("#VALUE!");
        }
        tokens.Add(new(TokenKind.End, "")); return tokens;
    }
    private sealed record Token(TokenKind Kind, string Text);
    private enum TokenKind { Number, String, Identifier, Reference, Table, Operator, LeftParen, RightParen, Comma, End }
}
