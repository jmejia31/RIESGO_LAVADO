using System.Data;
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Application;

public sealed class CalculoConfiguracionService : ICalculoConfiguracionService
{
    private static readonly HashSet<string> VersionStates = new(StringComparer.OrdinalIgnoreCase)
    { "DRAFT", "IN_REVIEW", "APPROVED", "PUBLISHED", "RETIRED", "ARCHIVED" };

    private readonly ICalculoConfiguracionRepository _repository;
    private readonly FormulaEngine _formulaEngine = new();

    public CalculoConfiguracionService(ICalculoConfiguracionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ServiceResult<IReadOnlyList<FormulaDto>>> ListarFormulasAsync(bool incluirInactivas)
    {
        var formulas = await _repository.ListarFormulasAsync(incluirInactivas);
        return ServiceResult<IReadOnlyList<FormulaDto>>.Ok(formulas.Select(InstitutionalFormulaTraceabilityMapper.Enrich).ToArray());
    }

    public async Task<ServiceResult<FormulaDto>> ObtenerFormulaAsync(long id)
    {
        if (id <= 0) return ServiceResult<FormulaDto>.BadRequest("El ID de fórmula es inválido.");
        var formula = await _repository.ObtenerFormulaAsync(id);
        return formula is null
            ? ServiceResult<FormulaDto>.NotFound("No se encontró la fórmula.")
            : ServiceResult<FormulaDto>.Ok(InstitutionalFormulaTraceabilityMapper.Enrich(formula));
    }

    public async Task<ServiceResult<long>> CrearFormulaAsync(CrearFormulaDto dto, long usuarioId, string? ip)
    {
        try
        {
            dto.Codigo = CalculoConfiguracionValidation.NormalizeCode(dto.Codigo, "Código");
            CalculoConfiguracionValidation.ValidateFormulaVersion(dto.VersionInicial.Expresion, dto.VersionInicial.TipoResultado);
            return ServiceResult<long>.Ok(await _repository.CrearFormulaAsync(dto, usuarioId, ip));
        }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult<long>> CrearFormulaVersionAsync(long id, CrearFormulaVersionDto dto, long usuarioId, string? ip)
    {
        if (id <= 0) return ServiceResult<long>.BadRequest("El ID de fórmula es inválido.");
        try { CalculoConfiguracionValidation.ValidateFormulaVersion(dto.Expresion, dto.TipoResultado); return ServiceResult<long>.Ok(await _repository.CrearFormulaVersionAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult> ActualizarFormulaBorradorAsync(long id, ActualizarFormulaBorradorDto dto, long usuarioId, string? ip)
    {
        try { CalculoConfiguracionValidation.ValidateFormulaVersion(dto.Expresion, dto.TipoResultado); return await Changed(await _repository.ActualizarFormulaBorradorAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult> ActualizarFormulaMetadataAsync(long id, ActualizarFormulaMetadataDto dto, long usuarioId, string? ip)
    {
        if (id <= 0 || dto is null) return ServiceResult.BadRequest("La fórmula y sus metadatos son obligatorios.");
        try
        {
            dto.Nombre = CalculoConfiguracionValidation.NormalizeFormulaName(dto.Nombre);
            dto.Descripcion = CalculoConfiguracionValidation.NormalizeFormulaDescription(dto.Descripcion);
            return await Changed(await _repository.ActualizarFormulaMetadataAsync(id, dto, usuarioId, ip));
        }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult<IReadOnlyList<FormulaVersionDto>>> ListarFormulaVersionesAsync(long id) => id <= 0
        ? ServiceResult<IReadOnlyList<FormulaVersionDto>>.BadRequest("El ID de fórmula es inválido.")
        : ServiceResult<IReadOnlyList<FormulaVersionDto>>.Ok(await _repository.ListarFormulaVersionesAsync(id));

    public async Task<ServiceResult<IReadOnlyList<FormulaUsageDto>>> ListarFormulaUsagesAsync(long id) => id <= 0
        ? ServiceResult<IReadOnlyList<FormulaUsageDto>>.BadRequest("El ID de fórmula es inválido.")
        : ServiceResult<IReadOnlyList<FormulaUsageDto>>.Ok(await _repository.ListarFormulaUsagesAsync(id));

    public async Task<ServiceResult> CrearFormulaUsoAsync(CrearFormulaUsoDto dto, long usuarioId, string? ip)
    {
        if (dto.VersionFormularioId <= 0 || dto.FormulaVersionId <= 0 || string.IsNullOrWhiteSpace(dto.CampoClave))
            return ServiceResult.BadRequest("La versión de formulario, la versión de fórmula y el campo son obligatorios.");
        try { return await Changed(await _repository.CrearFormulaUsoAsync(dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult> ReemplazarFormulaUsosAsync(long versionFormularioId, ReemplazarFormulaUsosDto dto, long usuarioId, string? ip)
    {
        if (versionFormularioId <= 0 || dto is null) return ServiceResult.BadRequest("La versión de formulario y los usos son obligatorios.");
        var usos = dto.Usos ?? new List<CrearFormulaUsoDto>();
        var campos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var uso in usos)
        {
            if (uso.VersionFormularioId != 0 && uso.VersionFormularioId != versionFormularioId)
                return ServiceResult.BadRequest("Todos los usos deben pertenecer a la versión de formulario indicada.");
            if (uso.FormulaVersionId <= 0 || string.IsNullOrWhiteSpace(uso.CampoClave) || uso.CampoClave.Trim().Length > 150)
                return ServiceResult.BadRequest("Cada uso requiere una versión de fórmula y un campo contractual válidos.");
            if (!campos.Add(uso.CampoClave.Trim()))
                return ServiceResult.BadRequest("No se permiten dos fórmulas para el mismo campo contractual.");
        }
        try { return await Changed(await _repository.ReemplazarFormulaUsosAsync(versionFormularioId, usos, usuarioId, ip)); }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult<IReadOnlyList<FormulaManualRegistroDto>>> RegistrarFormulasManualesAsync(RegistrarFormulasManualesDto dto, long usuarioId, string? ip)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.DefinicionFormularioJson))
            return ServiceResult<IReadOnlyList<FormulaManualRegistroDto>>.BadRequest("La definición del formulario es obligatoria para registrar fórmulas manuales.");

        var formulas = dto.Formulas ?? new List<RegistrarFormulaManualDto>();
        if (formulas.Count == 0) return ServiceResult<IReadOnlyList<FormulaManualRegistroDto>>.Ok(Array.Empty<FormulaManualRegistroDto>());
        if (formulas.Count > 100) return ServiceResult<IReadOnlyList<FormulaManualRegistroDto>>.BadRequest("La cantidad de fórmulas manuales excede el límite permitido.");

        try
        {
            var normalizadas = new List<RegistrarFormulaManualDto>(formulas.Count);
            var claves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (RegistrarFormulaManualDto formula in formulas)
            {
                string clave = formula.CampoClave?.Trim() ?? string.Empty;
                string expresion = formula.Expresion?.Trim() ?? string.Empty;
                string nombre = formula.Nombre?.Trim() ?? string.Empty;
                string tipo = formula.TipoResultado?.Trim().ToUpperInvariant() ?? string.Empty;
                if (clave.Length == 0 || clave.Length > 150 || expresion.Length == 0 || nombre.Length == 0)
                    return ServiceResult<IReadOnlyList<FormulaManualRegistroDto>>.BadRequest("Cada fórmula manual requiere campo, nombre y expresión.");
                if (!claves.Add(clave))
                    return ServiceResult<IReadOnlyList<FormulaManualRegistroDto>>.BadRequest("No se permiten dos fórmulas manuales para el mismo campo.");
                CalculoConfiguracionValidation.ValidateFormulaVersion(expresion, tipo);
                normalizadas.Add(new RegistrarFormulaManualDto { CampoClave = clave, Expresion = expresion, Nombre = nombre, TipoResultado = tipo });
            }

            string? targetError = ValidarTargetsManuales(dto.DefinicionFormularioJson, normalizadas);
            if (targetError is not null)
                return ServiceResult<IReadOnlyList<FormulaManualRegistroDto>>.BadRequest(targetError);

            var expressions = normalizadas.ToDictionary(item => item.CampoClave, item => item.Expresion, StringComparer.OrdinalIgnoreCase);
            var resultTypes = normalizadas.ToDictionary(item => item.CampoClave, item => item.TipoResultado, StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<FormulaDiagnostic> diagnostics = _formulaEngine.ValidateManualDefinition(dto.DefinicionFormularioJson, expressions, resultTypes);
            if (diagnostics.Count > 0)
            {
                string detail = string.Join("; ", diagnostics.Select(error => $"{error.Field}: {error.Code}"));
                return ServiceResult<IReadOnlyList<FormulaManualRegistroDto>>.BadRequest($"La fórmula manual no supera la validación institucional: {detail}.");
            }

            return ServiceResult<IReadOnlyList<FormulaManualRegistroDto>>.Ok(
                await _repository.RegistrarFormulasManualesAsync(normalizadas, usuarioId, ip),
                "Fórmulas manuales validadas y registradas como borrador.");
        }
        catch (Exception ex) { return Failure<IReadOnlyList<FormulaManualRegistroDto>>(ex); }
    }

    public async Task<ServiceResult> CambiarEstadoFormulaAsync(long id, CambiarEstadoConfiguracionDto dto, long usuarioId, string? ip)
    {
        if (id <= 0 || dto is null || !IsMasterState(dto.Estado)) return ServiceResult.BadRequest("Estado de fórmula inválido.");
        try { return await Changed(await _repository.CambiarEstadoFormulaAsync(id, dto.Estado.Trim().ToUpperInvariant(), dto.VersionRow, usuarioId, ip)); }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult<IReadOnlyList<FuncionDto>>> ListarFuncionesAsync(bool incluirInactivas) =>
        ServiceResult<IReadOnlyList<FuncionDto>>.Ok(await _repository.ListarFuncionesAsync(incluirInactivas));

    public async Task<ServiceResult<FuncionDto>> ObtenerFuncionAsync(long id) => id <= 0
        ? ServiceResult<FuncionDto>.BadRequest("El ID de función es inválido.")
        : await Found(_repository.ObtenerFuncionAsync(id), "No se encontró la función.");

    public async Task<ServiceResult<long>> CrearFuncionAsync(CrearFuncionDto dto, long usuarioId, string? ip)
    {
        try { dto.Codigo = CalculoConfiguracionValidation.NormalizeCode(dto.Codigo, "Código"); CalculoConfiguracionValidation.ValidateFunctionVersion(dto.VersionInicial); return ServiceResult<long>.Ok(await _repository.CrearFuncionAsync(dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult<long>> CrearFuncionVersionAsync(long id, CrearFuncionVersionDto dto, long usuarioId, string? ip)
    {
        if (id <= 0) return ServiceResult<long>.BadRequest("El ID de función es inválido.");
        try { CalculoConfiguracionValidation.ValidateFunctionVersion(dto); return ServiceResult<long>.Ok(await _repository.CrearFuncionVersionAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult> ActualizarFuncionBorradorAsync(long id, ActualizarFuncionBorradorDto dto, long usuarioId, string? ip)
    {
        try { CalculoConfiguracionValidation.ValidateFunctionVersion(dto); return await Changed(await _repository.ActualizarFuncionBorradorAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult> CambiarEstadoFuncionVersionAsync(long id, CambiarEstadoConfiguracionDto dto, long usuarioId, string? ip) =>
        await ChangeVersion(id, dto, (version, state, row) => _repository.CambiarEstadoFuncionVersionAsync(version, new CambiarEstadoConfiguracionDto { Estado = state, VersionRow = row }, usuarioId, ip));

    public async Task<ServiceResult<IReadOnlyList<FuncionVersionDto>>> ListarFuncionVersionesAsync(long id) => id <= 0
        ? ServiceResult<IReadOnlyList<FuncionVersionDto>>.BadRequest("El ID de función es inválido.")
        : ServiceResult<IReadOnlyList<FuncionVersionDto>>.Ok(await _repository.ListarFuncionVersionesAsync(id));

    public async Task<ServiceResult<IReadOnlyList<FuncionArgumentoDto>>> ListarFuncionArgumentosAsync(long id) => id <= 0
        ? ServiceResult<IReadOnlyList<FuncionArgumentoDto>>.BadRequest("El ID de versión es inválido.")
        : ServiceResult<IReadOnlyList<FuncionArgumentoDto>>.Ok(await _repository.ListarFuncionArgumentosAsync(id));

    public async Task<ServiceResult<IReadOnlyList<ParametroDto>>> ListarParametrosAsync(bool incluirInactivas) =>
        ServiceResult<IReadOnlyList<ParametroDto>>.Ok(await _repository.ListarParametrosAsync(incluirInactivas));

    public async Task<ServiceResult<ParametroDto>> ObtenerParametroAsync(long id) => id <= 0
        ? ServiceResult<ParametroDto>.BadRequest("El ID de parámetro es inválido.")
        : await Found(_repository.ObtenerParametroAsync(id), "No se encontró el parámetro.");

    public async Task<ServiceResult<long>> CrearParametroAsync(CrearParametroDto dto, long usuarioId, string? ip)
    {
        try { dto.Codigo = CalculoConfiguracionValidation.NormalizeCode(dto.Codigo, "Código"); CalculoConfiguracionValidation.ValidateParameterVersion(dto.VersionInicial); return ServiceResult<long>.Ok(await _repository.CrearParametroAsync(dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult<long>> CrearParametroVersionAsync(long id, CrearParametroVersionDto dto, long usuarioId, string? ip)
    {
        if (id <= 0) return ServiceResult<long>.BadRequest("El ID de parámetro es inválido.");
        try { CalculoConfiguracionValidation.ValidateParameterVersion(dto); return ServiceResult<long>.Ok(await _repository.CrearParametroVersionAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure<long>(ex); }
    }

    public async Task<ServiceResult> ActualizarParametroBorradorAsync(long id, ActualizarParametroBorradorDto dto, long usuarioId, string? ip)
    {
        try { CalculoConfiguracionValidation.ValidateParameterVersion(dto); return await Changed(await _repository.ActualizarParametroBorradorAsync(id, dto, usuarioId, ip)); }
        catch (Exception ex) { return Failure(ex); }
    }

    public async Task<ServiceResult> CambiarEstadoParametroVersionAsync(long id, CambiarEstadoConfiguracionDto dto, long usuarioId, string? ip) =>
        await ChangeVersion(id, dto, (version, state, row) => _repository.CambiarEstadoParametroVersionAsync(version, new CambiarEstadoConfiguracionDto { Estado = state, VersionRow = row }, usuarioId, ip));

    public async Task<ServiceResult<IReadOnlyList<ParametroVersionDto>>> ListarParametroVersionesAsync(long id) => id <= 0
        ? ServiceResult<IReadOnlyList<ParametroVersionDto>>.BadRequest("El ID de parámetro es inválido.")
        : ServiceResult<IReadOnlyList<ParametroVersionDto>>.Ok(await _repository.ListarParametroVersionesAsync(id));

    private static bool IsMasterState(string? state) => state?.Trim().ToUpperInvariant() is "ACTIVE" or "INACTIVE" or "RETIRED";

    private static string? ValidarTargetsManuales(string json, IReadOnlyList<RegistrarFormulaManualDto> formulas)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("definicionFormulario", out JsonElement nested)) root = nested;
            var fields = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("secciones", out JsonElement sections) && sections.ValueKind == JsonValueKind.Array)
                foreach (JsonElement section in sections.EnumerateArray())
                    if (section.TryGetProperty("campos", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
                        foreach (JsonElement field in items.EnumerateArray())
                        {
                            string? key = JsonText(field, "clave") ?? JsonText(field, "rutaDatos") ?? JsonText(field, "identificador");
                            if (!string.IsNullOrWhiteSpace(key)) fields.TryAdd(key, field);
                        }

            foreach (RegistrarFormulaManualDto formula in formulas)
            {
                if (!fields.TryGetValue(formula.CampoClave, out JsonElement field))
                    return $"El campo '{formula.CampoClave}' no existe en la definición del formulario.";
                string type = (JsonText(field, "tipo") ?? string.Empty).Trim().ToLowerInvariant().Replace('_', '-');
                if (type is not ("formula" or "calculado" or "calculo-sistema" or "texto-calculado"))
                    return $"El campo '{formula.CampoClave}' no es un campo calculado compatible.";
                if (TieneMetadataCentralManual(field))
                    return $"El campo '{formula.CampoClave}' contiene identidad de fórmula central; desvincúlela antes de usar fórmula manual.";
            }
            return null;
        }
        catch (JsonException ex) { return $"La definición del formulario no es JSON válida: {ex.Message}"; }
    }

    private static string? JsonText(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;

    private static bool TieneMetadataCentralManual(JsonElement field)
    {
        foreach (string property in new[] { "formulaId", "formulaVersionId", "formulaCodigo", "formulaVersion" })
        {
            if (!field.TryGetProperty(property, out JsonElement value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                continue;

            if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())) return true;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal number) && number != 0) return true;
            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False) return true;
        }
        return false;
    }
    private static async Task<ServiceResult> ChangeVersion(long id, CambiarEstadoConfiguracionDto dto, Func<long, string, int, Task<bool>> change)
    {
        if (dto is null) return ServiceResult.BadRequest("El estado de versión es obligatorio.");
        if (dto.Estado?.Trim().Equals("PUBLISHED", StringComparison.OrdinalIgnoreCase) == true)
            return ServiceResult.BadRequest("La publicación requiere validación del Publication Gate único.");
        if (id <= 0 || string.IsNullOrWhiteSpace(dto.Estado) || !VersionStates.Contains(dto.Estado)) return ServiceResult.BadRequest("Estado de versión inválido.");
        try { return await Changed(await change(id, dto.Estado.Trim().ToUpperInvariant(), dto.VersionRow)); }
        catch (Exception ex) { return Failure(ex); }
    }
    private static async Task<ServiceResult<T>> Found<T>(Task<T?> task, string message) where T : class => (await task) is { } item
        ? ServiceResult<T>.Ok(item)
        : ServiceResult<T>.NotFound(message);
    private static Task<ServiceResult> Changed(bool changed) => Task.FromResult(changed ? ServiceResult.Ok() : ServiceResult.Conflict("El recurso no existe, no está en borrador o fue modificado por otra operación."));
    private static ServiceResult Failure(Exception ex) => ex switch
    {
        InvalidOperationException => ServiceResult.BadRequest(ex.Message),
        KeyNotFoundException => ServiceResult.NotFound(ex.Message),
        OracleException oracle when oracle.Number is 1 or 2291 or 2292 => ServiceResult.Conflict("La operación viola una regla de integridad o un recurso ya existe."),
        DBConcurrencyException => ServiceResult.Conflict("El recurso fue modificado por otra operación."),
        _ => throw ex
    };
    private static ServiceResult<T> Failure<T>(Exception ex) => ex switch
    {
        InvalidOperationException => ServiceResult<T>.BadRequest(ex.Message),
        KeyNotFoundException => ServiceResult<T>.NotFound(ex.Message),
        OracleException oracle when oracle.Number is 1 or 2291 or 2292 => ServiceResult<T>.Conflict("La operación viola una regla de integridad o un recurso ya existe."),
        DBConcurrencyException => ServiceResult<T>.Conflict("El recurso fue modificado por otra operación."),
        _ => throw ex
    };
}

public static class InstitutionalFormulaTraceabilityMapper
{
    public static FormulaDto Enrich(FormulaDto formula)
    {
        ArgumentNullException.ThrowIfNull(formula);
        var definition = InstitutionalFormulaDataset.All.SingleOrDefault(item =>
            string.Equals(item.Code, formula.Codigo, StringComparison.OrdinalIgnoreCase));
        formula.ReferenciaInstitucional = definition is null ? null : new ReferenciaFormulaInstitucionalDto
        {
            Numero = definition.Number,
            TargetField = definition.TargetField,
            SourceCell = definition.SourceCell,
            ExcelColumn = ExtractExcelColumn(definition.SourceCell)
        };
        return formula;
    }

    private static string ExtractExcelColumn(string sourceCell)
    {
        var separator = sourceCell.LastIndexOf('!');
        var address = separator >= 0 ? sourceCell[(separator + 1)..] : string.Empty;
        var column = new string(address.TakeWhile(char.IsLetter).ToArray());
        if (column.Length == 0 || !address.AsSpan(column.Length).ToString().All(char.IsDigit))
            throw new InvalidOperationException($"La celda fuente institucional '{sourceCell}' no tiene formato válido.");
        return column;
    }
}
