using System;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos.Domain;

public sealed class MatricesRiesgoService : IMatricesRiesgoService
{
    public ServiceResult<CalculoRiesgoResultadoDto> CalcularYValidarRiesgo(
        int frecuencia, 
        int impacto, 
        decimal preventivo, 
        decimal detectivo, 
        decimal correctivo, 
        int frecResidual, 
        int impResidual)
    {
        // 1. Validaciones básicas de rangos de entrada
        if (frecuencia < 1 || frecuencia > 5 || impacto < 1 || impacto > 5)
        {
            return ServiceResult<CalculoRiesgoResultadoDto>.BadRequest("La Frecuencia y el Impacto inherente deben estar en el rango de 1 a 5.");
        }
        if (frecResidual < 1 || frecResidual > 5 || impResidual < 1 || impResidual > 5)
        {
            return ServiceResult<CalculoRiesgoResultadoDto>.BadRequest("La Frecuencia y el Impacto residual deben estar en el rango de 1 a 5.");
        }
        if (preventivo < 0 || preventivo > 100 || detectivo < 0 || detectivo > 100 || correctivo < 0 || correctivo > 100)
        {
            return ServiceResult<CalculoRiesgoResultadoDto>.BadRequest("Los porcentajes de controles (Preventivo, Detectivo, Correctivo) deben estar entre 0% y 100%.");
        }

        // 2. Calcular Riesgo Inherente (VRI)
        int vri = frecuencia + impacto - 1; // Rango 1 a 9

        // 3. Calcular Eficacia Total de Mitigación (ETP)
        decimal etp = (preventivo * 0.70m) + (detectivo * 0.15m) + (correctivo * 0.15m);

        // 4. Calcular Riesgo Residual Matemático (VRR)
        decimal mitigacionFactor = 1m - (etp / 100m);
        decimal vrrCalculadoRaw = vri * mitigacionFactor;
        
        // Regla metodológica: MAX(1, VRI * (1 - ETP/100)) redondeado sin decimales (AwayFromZero)
        decimal vrrAcotado = Math.Max(1m, vrrCalculadoRaw);
        int vrr = (int)Math.Round(vrrAcotado, MidpointRounding.AwayFromZero); // Rango 1 a 9

        // 5. Calcular Riesgo Residual del Formulario (VRR2)
        int vrr2 = frecResidual + impResidual - 1; // Rango 1 a 9

        // 6. Validar Coherencia Residual (VRR == VRR2)
        bool coherente = vrr == vrr2;

        var resultado = new CalculoRiesgoResultadoDto
        {
            Vri = vri,
            Etp = etp,
            Vrr = vrr,
            Vrr2 = vrr2,
            NivelResidual = DeterminarNivelResidual(vrr),
            Coherente = coherente
        };

        if (!coherente)
        {
            string mensajeError = $"Incoherencia de Riesgo Residual. El Riesgo Residual calculado numéricamente es {vrr} ({resultado.NivelResidual}), pero la combinación de Frecuencia Residual ({frecResidual}) e Impacto Residual ({impResidual}) en el formulario da {vrr2}. Ambas clasificaciones deben coincidir.";
            return new ServiceResult<CalculoRiesgoResultadoDto>(false, resultado, mensajeError, 400);
        }

        return ServiceResult<CalculoRiesgoResultadoDto>.Ok(resultado, "Cálculo y validación de riesgo generado correctamente.");
    }

    private static string DeterminarNivelResidual(int vrr)
    {
        return vrr switch
        {
            <= 2 => "BAJO",
            <= 4 => "MODERADO",
            <= 6 => "ALTO",
            _ => "CRÍTICO"
        };
    }
}
