using System.Text.Json;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class InstitutionalFormulaTraceabilityTests
{
    [Fact]
    public void ReadMetadata_MapsAllInstitutionalFormulaCodesTargetsCellsAndColumns()
    {
        Assert.Equal(34, InstitutionalFormulaDataset.All.Count);
        Assert.Equal(new[]
        {
            "F01_VALOR_RIESGO_INHERENTE", "F02_NIVEL_RIESGO_INHERENTE", "F03_NIVEL_CONTROL_PREVENTIVO",
            "F04_PORCENTAJE_CONTROL_PREVENTIVO", "F05_NIVEL_CONTROL_DETECTIVO", "F06_PORCENTAJE_CONTROL_DETECTIVO",
            "F07_NIVEL_CONTROL_CORRECTIVO", "F08_PORCENTAJE_CONTROL_CORRECTIVO", "F09_EFECTIVIDAD_TOTAL_PONDERADA",
            "F10_RIESGO_RESIDUAL_DESCRIPCION", "F11_FRECUENCIA_RESIDUAL", "F12_IMPACTO_RESIDUAL",
            "F13_VALOR_RIESGO_RESIDUAL", "F14_NIVEL_RIESGO_RESIDUAL", "F15_FRECUENCIA_RESIDUAL_AUX",
            "F16_IMPACTO_RESIDUAL_AUX", "F17_SUMA_RESIDUAL_REDONDEADA_AUX", "F18_F_BASE_AUX",
            "F19_I_BASE_AUX", "F20_TOPE_F_AUX", "F21_TOPE_I_AUX", "F22_CAPACIDAD_F_AUX",
            "F23_CAPACIDAD_I_AUX", "F24_RESTO_AUX", "F25_PREFIERE_I_AUX", "F26_INCREMENTO_I_AUX",
            "F27_INCREMENTO_F_AUX", "F28_VALOR_RIESGO_RESIDUAL_AUX", "F29_VERIFICACION_RIESGO_RESIDUAL",
            "F30_VRR_2", "F31_VERIFICAR_VRR_2", "F32_VERIFICAR_FRECUENCIA", "F33_VERIFICAR_IMPACTO",
            "F34_DIFERENCIA_VRI_VRR"
        }, InstitutionalFormulaDataset.All.Select(definition => definition.Code));
        var projected = InstitutionalFormulaDataset.All.Select(definition =>
            InstitutionalFormulaTraceabilityMapper.Enrich(new FormulaDto { Codigo = definition.Code }));

        Assert.Equal(34, projected.Count());
        Assert.Equal(34, projected.Select(item => item.ReferenciaInstitucional?.Numero).Distinct().Count());
        foreach (var definition in InstitutionalFormulaDataset.All)
        {
            var formula = projected.Single(item => item.Codigo == definition.Code);
            var reference = Assert.IsType<ReferenciaFormulaInstitucionalDto>(formula.ReferenciaInstitucional);
            Assert.Equal(definition.Number, reference.Numero);
            Assert.Equal(definition.TargetField, reference.TargetField);
            Assert.Equal(definition.SourceCell, reference.SourceCell);
            Assert.Equal(definition.SourceCell.Split('!')[1].TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'), reference.ExcelColumn);
        }
    }

    [Fact]
    public void NonInstitutionalFormula_HasNoInventedInstitutionalReference()
    {
        var formula = InstitutionalFormulaTraceabilityMapper.Enrich(new FormulaDto { Codigo = "CUSTOM_FORMULA", Nombre = "Custom" });

        Assert.Null(formula.ReferenciaInstitucional);
    }

    [Fact]
    public void FormulaWriteContracts_DoNotAcceptInstitutionalReferenceMetadata()
    {
        var create = JsonSerializer.Serialize(new CrearFormulaDto());
        var update = JsonSerializer.Serialize(new ActualizarFormulaBorradorDto());

        Assert.DoesNotContain("ReferenciaInstitucional", create, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ReferenciaInstitucional", update, StringComparison.OrdinalIgnoreCase);
    }
}
