using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Domain;

/// <summary>
/// Motor puro de cálculo de Matrices de Riesgos.
/// La metodología se recibe como datos para evitar pesos, escalas o mitigaciones fijas en código.
/// </summary>
public sealed class MatricesRiesgoService : IMatricesRiesgoService
{
    private const string VersionCalculo = "F6-1.1";
    private const string TipoCalculoGlobal = "GLOBAL";
    private const string TipoCalculoFactor = "FACTOR";
    private const decimal ToleranciaTotal = 0.01m;

    // Proceso principal del motor: valida la metodología vigente, calcula cada factor,
    // consolida riesgo inherente/residual y devuelve niveles sin persistir datos.
    public ServiceResult<MatrizCalculoResultadoDto> Calcular(MatrizCalculoRequestDto request)
    {
        var errores = ValidarRequest(request);
        if (errores.Count > 0)
            return ServiceResult<MatrizCalculoResultadoDto>.BadRequest(string.Join(" ", errores));

        var metodologia = request.Metodologia!;
        var resultado = new MatrizCalculoResultadoDto
        {
            VersionCalculo = VersionCalculo,
            VersionMetodologia = metodologia.Version
        };

        foreach (var factor in request.Factores)
        {
            var resultadoFactor = CalcularFactor(factor, metodologia);
            resultado.Factores.Add(resultadoFactor);
        }

        resultado.PuntajeInherente = CalcularPuntajeConsolidado(resultado.Factores, request.TipoCalculo, metodologia, f => f.PuntajeInherente);
        resultado.PuntajeResidual = CalcularPuntajeConsolidado(resultado.Factores, request.TipoCalculo, metodologia, f => f.PuntajeResidual);
        resultado.MitigacionPct = CalcularMitigacionGlobal(resultado.PuntajeInherente, resultado.PuntajeResidual, metodologia);

        var nivelInherente = ObtenerNivel(resultado.PuntajeInherente, metodologia);
        resultado.NivelInherente = nivelInherente.Nivel;
        resultado.ColorInherente = nivelInherente.Color;

        var nivelResidual = ObtenerNivel(resultado.PuntajeResidual, metodologia);
        resultado.NivelResidual = nivelResidual.Nivel;
        resultado.ColorResidual = nivelResidual.Color;
        resultado.RequierePlanAccion = nivelResidual.RequierePlanAccion;
        resultado.Explicacion = CrearExplicacion(resultado);

        return ServiceResult<MatrizCalculoResultadoDto>.Ok(resultado, "Cálculo de matriz generado correctamente.");
    }

    // Proceso por factor: aplica variables internas, mitigación por controles válidos
    // y clasificación de riesgo usando rangos configurados desde la metodología.
    private static FactorCalculoResultadoDto CalcularFactor(FactorCalculoDto factor, MetodologiaCalculoDto metodologia)
    {
        var resultado = new FactorCalculoResultadoDto
        {
            Codigo = factor.Codigo.Trim(),
            Nombre = factor.Nombre.Trim(),
            PesoInstitucional = Redondear(factor.PesoInstitucional, metodologia)
        };

        foreach (var variable in factor.Variables)
        {
            var puntaje = variable.Puntaje!.Value;
            var ponderado = Redondear(puntaje * variable.PesoInterno / 100m, metodologia);
            var nivelVariable = ObtenerNivel(puntaje, metodologia);

            resultado.Variables.Add(new VariableCalculoResultadoDto
            {
                Codigo = variable.Codigo.Trim(),
                Nombre = variable.Nombre.Trim(),
                PesoInterno = Redondear(variable.PesoInterno, metodologia),
                Puntaje = Redondear(puntaje, metodologia),
                PuntajePonderado = ponderado,
                Nivel = nivelVariable.Nivel,
                Color = nivelVariable.Color
            });
        }

        resultado.PuntajeInherente = Redondear(resultado.Variables.Sum(v => v.PuntajePonderado), metodologia);
        var nivelInherente = ObtenerNivel(resultado.PuntajeInherente, metodologia);
        resultado.NivelInherente = nivelInherente.Nivel;
        resultado.ColorInherente = nivelInherente.Color;

        resultado.MitigacionPct = CalcularMitigacionFactor(factor.Controles, metodologia);
        resultado.PuntajeResidual = CalcularResidual(resultado.PuntajeInherente, resultado.MitigacionPct, metodologia);

        var nivelResidual = ObtenerNivel(resultado.PuntajeResidual, metodologia);
        resultado.NivelResidual = nivelResidual.Nivel;
        resultado.ColorResidual = nivelResidual.Color;
        resultado.RequierePlanAccion = nivelResidual.RequierePlanAccion;

        return resultado;
    }

    // Solo controles activos y con evidencia reducen el riesgo; la mitigación nunca supera el tope metodológico.
    private static decimal CalcularMitigacionFactor(IEnumerable<ControlCalculoDto> controles, MetodologiaCalculoDto metodologia)
    {
        var mitigacion = controles
            .Where(c => c.Activo && c.TieneEvidencia)
            .Select(c => c.MitigacionPct)
            .DefaultIfEmpty(0m)
            .Max();

        return Redondear(Math.Min(mitigacion, metodologia.MitigacionMaximaPct), metodologia);
    }

    private static decimal CalcularResidual(decimal inherente, decimal mitigacionPct, MetodologiaCalculoDto metodologia)
    {
        var residual = inherente * (1m - mitigacionPct / 100m);
        return Redondear(Math.Clamp(residual, metodologia.PuntajeMinimo, metodologia.PuntajeMaximo), metodologia);
    }

    private static decimal CalcularMitigacionGlobal(decimal inherente, decimal residual, MetodologiaCalculoDto metodologia)
    {
        if (inherente <= 0)
            return 0m;

        var mitigacion = (1m - residual / inherente) * 100m;
        return Redondear(Math.Clamp(mitigacion, 0m, metodologia.MitigacionMaximaPct), metodologia);
    }

    // Validación crítica: protege pesos institucionales, variables obligatorias,
    // tipo de cálculo y coherencia completa antes de ejecutar fórmulas.
    private static List<string> ValidarRequest(MatrizCalculoRequestDto? request)
    {
        var errores = new List<string>();
        if (request == null)
        {
            errores.Add("La solicitud de cálculo es obligatoria.");
            return errores;
        }

        ValidarMetodologia(request.Metodologia, errores);

        if (request.EsRecalculo && string.IsNullOrWhiteSpace(request.MotivoCalculo))
            errores.Add("El motivo de recálculo es obligatorio.");

        if (request.Factores.Count == 0)
        {
            errores.Add("Debe incluir al menos un factor para calcular la matriz.");
            return errores;
        }

        if (!EsTipoCalculoValido(request.TipoCalculo))
            errores.Add("El tipo de cálculo debe ser GLOBAL o FACTOR.");

        if (request.Metodologia != null)
        {
            if (EsCalculoGlobal(request.TipoCalculo))
                ValidarFactoresGlobales(request.Factores, request.Metodologia, errores);

            foreach (var factor in request.Factores)
                ValidarFactor(factor, request.Metodologia, errores);
        }

        return errores;
    }

    private static void ValidarMetodologia(MetodologiaCalculoDto? metodologia, List<string> errores)
    {
        if (metodologia == null)
        {
            errores.Add("La metodología de cálculo es obligatoria.");
            return;
        }

        if (string.IsNullOrWhiteSpace(metodologia.Version))
            errores.Add("La versión de metodología es obligatoria.");

        if (metodologia.PesoTotalEsperado <= 0)
            errores.Add("El peso total esperado de la metodología debe ser mayor que cero.");

        if (metodologia.PuntajeMinimo >= metodologia.PuntajeMaximo)
            errores.Add("El puntaje mínimo debe ser menor que el puntaje máximo.");

        if (metodologia.MitigacionMaximaPct < 0 || metodologia.MitigacionMaximaPct > 100)
            errores.Add("La mitigación máxima debe estar entre 0% y 100%.");

        if (metodologia.DecimalesCalculo < 0 || metodologia.DecimalesCalculo > 8)
            errores.Add("Los decimales de cálculo deben estar entre 0 y 8.");

        if (metodologia.DecimalesVisualizacion < 0 || metodologia.DecimalesVisualizacion > metodologia.DecimalesCalculo)
            errores.Add("Los decimales de visualización no pueden superar los decimales de cálculo.");

        if (metodologia.FactoresInstitucionales.Count == 0)
            errores.Add("La metodología debe incluir factores institucionales.");

        var pesoTotal = metodologia.FactoresInstitucionales.Sum(f => f.PesoInstitucional);
        if (!EsTotalValido(pesoTotal, metodologia.PesoTotalEsperado))
            errores.Add("Los factores institucionales de la metodología deben totalizar el peso esperado.");

        if (metodologia.EscalasRiesgo.Count == 0)
            errores.Add("La metodología debe incluir escalas de riesgo.");

        if (metodologia.MitigacionesPermitidas.Count == 0)
            errores.Add("La metodología debe incluir mitigaciones permitidas.");

        foreach (var escala in metodologia.EscalasRiesgo)
        {
            if (string.IsNullOrWhiteSpace(escala.Nivel))
                errores.Add("Cada escala de riesgo debe tener nivel.");

            if (escala.ValorMinimo > escala.ValorMaximo)
                errores.Add($"La escala {escala.Nivel} tiene rango inválido.");
        }
    }

    private static void ValidarFactoresGlobales(List<FactorCalculoDto> factores, MetodologiaCalculoDto metodologia, List<string> errores)
    {
        var pesoInstitucionalTotal = factores.Sum(f => f.PesoInstitucional);
        if (!EsTotalValido(pesoInstitucionalTotal, metodologia.PesoTotalEsperado))
            errores.Add("La ponderación institucional global debe totalizar el peso esperado.");

        foreach (var factorRequerido in metodologia.FactoresInstitucionales.Where(f => f.ObligatorioGlobal))
        {
            if (!factores.Any(f => string.Equals(f.Codigo, factorRequerido.Codigo, StringComparison.OrdinalIgnoreCase)))
                errores.Add($"El cálculo global debe incluir el factor institucional {factorRequerido.Codigo}.");
        }
    }

    private static void ValidarFactor(FactorCalculoDto factor, MetodologiaCalculoDto metodologia, List<string> errores)
    {
        if (string.IsNullOrWhiteSpace(factor.Codigo))
            errores.Add("Cada factor debe tener código.");

        var factorMetodologia = metodologia.FactoresInstitucionales
            .FirstOrDefault(f => string.Equals(f.Codigo, factor.Codigo, StringComparison.OrdinalIgnoreCase));

        if (factorMetodologia != null && factor.PesoInstitucional != factorMetodologia.PesoInstitucional)
            errores.Add($"El factor {factor.Codigo} debe tener peso institucional {factorMetodologia.PesoInstitucional}% según la metodología vigente.");

        if (factor.Variables.Count == 0)
        {
            errores.Add($"El factor {factor.Codigo} debe incluir variables.");
            return;
        }

        var pesoInternoTotal = factor.Variables.Sum(v => v.PesoInterno);
        if (!EsTotalValido(pesoInternoTotal, metodologia.PesoTotalEsperado))
            errores.Add($"Las variables internas del factor {factor.Codigo} deben totalizar el peso esperado.");

        foreach (var variable in factor.Variables)
        {
            if (string.IsNullOrWhiteSpace(variable.Codigo))
                errores.Add($"Una variable del factor {factor.Codigo} no tiene código.");

            if (variable.Obligatoria && (!variable.TieneValor || !variable.Puntaje.HasValue))
                errores.Add($"La variable {variable.Codigo} del factor {factor.Codigo} es obligatoria.");

            if (variable.Puntaje.HasValue && (variable.Puntaje < metodologia.PuntajeMinimo || variable.Puntaje > metodologia.PuntajeMaximo))
                errores.Add($"La variable {variable.Codigo} del factor {factor.Codigo} debe tener puntaje entre {metodologia.PuntajeMinimo} y {metodologia.PuntajeMaximo}.");

            if (variable.PesoInterno < 0 || variable.PesoInterno > metodologia.PesoTotalEsperado)
                errores.Add($"La variable {variable.Codigo} del factor {factor.Codigo} tiene peso interno inválido.");
        }

        foreach (var control in factor.Controles.Where(c => c.Activo && c.TieneEvidencia))
        {
            if (!metodologia.MitigacionesPermitidas.Contains(control.MitigacionPct))
                errores.Add($"El control {control.Codigo} del factor {factor.Codigo} tiene mitigación no permitida.");
        }
    }

    private static EscalaRiesgoCalculoDto ObtenerNivel(decimal puntaje, MetodologiaCalculoDto metodologia)
    {
        var valor = Math.Clamp(puntaje, metodologia.PuntajeMinimo, metodologia.PuntajeMaximo);
        return metodologia.EscalasRiesgo
            .OrderBy(e => e.ValorMinimo)
            .First(e => valor >= e.ValorMinimo && valor <= e.ValorMaximo);
    }

    // Consolidación: en cálculo GLOBAL usa el peso institucional oficial;
    // en cálculo FACTOR permite evaluación individual o subtotal controlado.
    private static decimal CalcularPuntajeConsolidado(
        IEnumerable<FactorCalculoResultadoDto> factores,
        string? tipoCalculo,
        MetodologiaCalculoDto metodologia,
        Func<FactorCalculoResultadoDto, decimal> selector)
    {
        var listaFactores = factores.ToList();
        if (EsCalculoGlobal(tipoCalculo))
            return Redondear(listaFactores.Sum(f => selector(f) * f.PesoInstitucional / metodologia.PesoTotalEsperado), metodologia);

        if (listaFactores.Count == 1)
            return Redondear(selector(listaFactores[0]), metodologia);

        var pesoTotal = listaFactores.Sum(f => f.PesoInstitucional);
        if (pesoTotal <= 0)
            return 0m;

        return Redondear(listaFactores.Sum(f => selector(f) * f.PesoInstitucional / pesoTotal), metodologia);
    }

    private static bool EsTipoCalculoValido(string? tipoCalculo)
    {
        if (string.IsNullOrWhiteSpace(tipoCalculo))
            return false;

        return EsCalculoGlobal(tipoCalculo)
            || tipoCalculo.Equals(TipoCalculoFactor, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EsCalculoGlobal(string? tipoCalculo)
    {
        return tipoCalculo?.Equals(TipoCalculoGlobal, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool EsTotalValido(decimal total, decimal esperado)
    {
        return Math.Abs(total - esperado) <= ToleranciaTotal;
    }

    private static decimal Redondear(decimal valor, MetodologiaCalculoDto metodologia)
    {
        return Math.Round(valor, metodologia.DecimalesCalculo, MidpointRounding.AwayFromZero);
    }

    private static string CrearExplicacion(MatrizCalculoResultadoDto resultado)
    {
        return $"Riesgo inherente {resultado.PuntajeInherente:0.####} ({resultado.NivelInherente}); "
            + $"mitigación global {resultado.MitigacionPct:0.####}%; "
            + $"riesgo residual {resultado.PuntajeResidual:0.####} ({resultado.NivelResidual}).";
    }
}
