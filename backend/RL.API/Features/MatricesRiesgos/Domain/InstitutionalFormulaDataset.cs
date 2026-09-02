namespace RL.API.Features.MatricesRiesgos.Domain;

/// <summary>
/// Mapa semántico de las 34 fórmulas institucionales de Matriz Consolidada.
/// El libro Excel es la fuente de trazabilidad; el runtime solo recibe DSL seguro.
/// </summary>
public sealed record InstitutionalFormulaDefinition(
    int Number,
    string Code,
    string SourceCell,
    string SemanticExpression,
    string ResultType)
{
    public string TargetField => Number switch
    {
        1 => "valor_riesgo_inherente",
        2 => "nivel_riesgo_inherente",
        3 => "nivel_control_preventivo",
        4 => "porcentaje_control_preventivo",
        5 => "nivel_control_detectivo",
        6 => "porcentaje_control_detectivo",
        7 => "nivel_control_correctivo",
        8 => "porcentaje_control_correctivo",
        9 => "efectividad_total_ponderada",
        10 => "riesgo_residual_descripcion",
        11 => "frecuencia_residual",
        12 => "impacto_residual",
        13 => "valor_riesgo_residual",
        14 => "nivel_riesgo_residual",
        15 => "frecuencia_residual_aux",
        16 => "impacto_residual_aux",
        17 => "suma_residual_redondeada_aux",
        18 => "f_base",
        19 => "i_base",
        20 => "tope_f",
        21 => "tope_i",
        22 => "capacidad_f_aux",
        23 => "capacidad_i_aux",
        24 => "resto_aux",
        25 => "prefiere_i_aux",
        26 => "incremento_i_aux",
        27 => "incremento_f_aux",
        28 => "valor_riesgo_residual_aux",
        29 => "verificacion",
        30 => "vrr_2",
        31 => "verificar_vrr_2",
        32 => "verificar_frecuencia",
        33 => "verificar_impacto",
        34 => "diferencia_vri_vrr",
        _ => throw new ArgumentOutOfRangeException(nameof(Number))
    };
}

public static class InstitutionalFormulaDataset
{
    public const int ExpectedCount = 34;

    public static IReadOnlyList<InstitutionalFormulaDefinition> All { get; } =
    [
        F(1, "F01_VALOR_RIESGO_INHERENTE", "Matriz Consolidada!L2", "IF((frecuencia+impacto-1)=-1,\"\",frecuencia+impacto-1)", "DECIMAL"),
        F(2, "F02_NIVEL_RIESGO_INHERENTE", "Matriz Consolidada!M2", "IFERROR(LOOKUP(\"CAT_NIVEL_RIESGO\",valor_riesgo_inherente),\"\")", "TEXT"),
        F(3, "F03_NIVEL_CONTROL_PREVENTIVO", "Matriz Consolidada!V2", "IFERROR(LOOKUP(\"CAT_EFECTIVIDAD_NIVEL\",escala_preventivo,\"NUMBER\"),\"\")", "DECIMAL"),
        F(4, "F04_PORCENTAJE_CONTROL_PREVENTIVO", "Matriz Consolidada!W2", "IFERROR(LOOKUP(\"CAT_EFECTIVIDAD_PORCENTAJE\",escala_preventivo,\"NUMBER\"),\"\")", "DECIMAL"),
        F(5, "F05_NIVEL_CONTROL_DETECTIVO", "Matriz Consolidada!Z2", "IFERROR(LOOKUP(\"CAT_EFECTIVIDAD_NIVEL\",escala_detectivo,\"NUMBER\"),\"\")", "DECIMAL"),
        F(6, "F06_PORCENTAJE_CONTROL_DETECTIVO", "Matriz Consolidada!AA2", "IFERROR(LOOKUP(\"CAT_EFECTIVIDAD_PORCENTAJE\",escala_detectivo,\"NUMBER\"),\"\")", "DECIMAL"),
        F(7, "F07_NIVEL_CONTROL_CORRECTIVO", "Matriz Consolidada!AD2", "IFERROR(LOOKUP(\"CAT_EFECTIVIDAD_NIVEL\",escala_correctivo,\"NUMBER\"),\"\")", "DECIMAL"),
        F(8, "F08_PORCENTAJE_CONTROL_CORRECTIVO", "Matriz Consolidada!AE2", "IFERROR(LOOKUP(\"CAT_EFECTIVIDAD_PORCENTAJE\",escala_correctivo,\"NUMBER\"),\"\")", "DECIMAL"),
        F(9, "F09_EFECTIVIDAD_TOTAL_PONDERADA", "Matriz Consolidada!AG2", "IF(AND(control_preventivo=\"\",control_detectivo=\"\",control_correctivo=\"\"),\"\",PESO_PREVENTIVO*IF(porcentaje_control_preventivo=\"\",0,porcentaje_control_preventivo)+PESO_DETECTIVO*IF(porcentaje_control_detectivo=\"\",0,porcentaje_control_detectivo)+PESO_CORRECTIVO*IF(porcentaje_control_correctivo=\"\",0,porcentaje_control_correctivo))", "DECIMAL"),
        F(10, "F10_RIESGO_RESIDUAL_DESCRIPCION", "Matriz Consolidada!AH2", "IF(riesgo_inherente_descripcion=\"\",\"\",riesgo_inherente_descripcion)", "TEXT"),
        F(11, "F11_FRECUENCIA_RESIDUAL", "Matriz Consolidada!AI2", "IFERROR(IF(OR(frecuencia=\"\",impacto=\"\",valor_riesgo_inherente=\"\",valor_riesgo_residual=\"\"),\"\",IF(valor_riesgo_inherente=valor_riesgo_residual,frecuencia,MIN(tope_f,f_base+incremento_f_aux))),\"\")", "DECIMAL"),
        F(12, "F12_IMPACTO_RESIDUAL", "Matriz Consolidada!AJ2", "IFERROR(IF(OR(frecuencia=\"\",impacto=\"\",valor_riesgo_inherente=\"\",valor_riesgo_residual=\"\"),\"\",IF(valor_riesgo_inherente=valor_riesgo_residual,impacto,MIN(tope_i,i_base+incremento_i_aux))),\"\")", "DECIMAL"),
        F(13, "F13_VALOR_RIESGO_RESIDUAL", "Matriz Consolidada!AK2", "IFERROR(ROUND(MAX(1,valor_riesgo_inherente*(1-efectividad_total_ponderada)),0),\"\")", "DECIMAL"),
        F(14, "F14_NIVEL_RIESGO_RESIDUAL", "Matriz Consolidada!AL2", "IFERROR(LOOKUP(\"CAT_NIVEL_RIESGO\",valor_riesgo_residual),\"\")", "TEXT"),
        F(15, "F15_FRECUENCIA_RESIDUAL_AUX", "Matriz Consolidada!AX2", "IFERROR((1-efectividad_total_ponderada)*frecuencia,\"\")", "DECIMAL"),
        F(16, "F16_IMPACTO_RESIDUAL_AUX", "Matriz Consolidada!AY2", "IFERROR((1-efectividad_total_ponderada)*impacto,\"\")", "DECIMAL"),
        F(17, "F17_SUMA_RESIDUAL_REDONDEADA_AUX", "Matriz Consolidada!AZ2", "IFERROR(valor_riesgo_residual+1,\"\")", "DECIMAL"),
        F(18, "F18_F_BASE_AUX", "Matriz Consolidada!BA2", "IFERROR(MAX(1,ROUNDDOWN(frecuencia_residual_aux,0)),\"\")", "DECIMAL"),
        F(19, "F19_I_BASE_AUX", "Matriz Consolidada!BB2", "IFERROR(MAX(1,ROUNDDOWN(impacto_residual_aux,0)),\"\")", "DECIMAL"),
        F(20, "F20_TOPE_F_AUX", "Matriz Consolidada!BC2", "IF(frecuencia=\"\",\"\",frecuencia)", "DECIMAL"),
        F(21, "F21_TOPE_I_AUX", "Matriz Consolidada!BD2", "IF(impacto=\"\",\"\",impacto)", "DECIMAL"),
        F(22, "F22_CAPACIDAD_F_AUX", "Matriz Consolidada!BE2", "IF(OR(tope_f=\"\",f_base=\"\"),\"\",MAX(0,tope_f-f_base))", "DECIMAL"),
        F(23, "F23_CAPACIDAD_I_AUX", "Matriz Consolidada!BF2", "IF(OR(tope_i=\"\",i_base=\"\"),\"\",MAX(0,tope_i-i_base))", "DECIMAL"),
        F(24, "F24_RESTO_AUX", "Matriz Consolidada!BG2", "IF(OR(suma_residual_redondeada_aux=\"\",f_base=\"\",i_base=\"\"),\"\",MAX(0,suma_residual_redondeada_aux-(f_base+i_base)))", "DECIMAL"),
        F(25, "F25_PREFIERE_I_AUX", "Matriz Consolidada!BH2", "IF(OR(impacto_residual_aux=\"\",frecuencia_residual_aux=\"\"),\"\",IF(MOD(impacto_residual_aux,1)>MOD(frecuencia_residual_aux,1),1,IF(MOD(impacto_residual_aux,1)=MOD(frecuencia_residual_aux,1),1,0)))", "DECIMAL"),
        F(26, "F26_INCREMENTO_I_AUX", "Matriz Consolidada!BI2", "IF(OR(resto_aux=\"\",capacidad_i_aux=\"\",capacidad_f_aux=\"\"),\"\",IF(resto_aux=0,0,IF(resto_aux=1,MIN(capacidad_i_aux,IF(OR(prefiere_i_aux=1,capacidad_f_aux=0),1,0)),MIN(capacidad_i_aux,IF(prefiere_i_aux=1,1+IF(capacidad_f_aux>0,0,1),IF(capacidad_f_aux>0,1,2))))))", "DECIMAL"),
        F(27, "F27_INCREMENTO_F_AUX", "Matriz Consolidada!BJ2", "IF(OR(resto_aux=\"\",capacidad_f_aux=\"\",incremento_i_aux=\"\"),\"\",MIN(capacidad_f_aux,MAX(0,resto_aux-incremento_i_aux)))", "DECIMAL"),
        F(28, "F28_VALOR_RIESGO_RESIDUAL_AUX", "Matriz Consolidada!BK2", "IFERROR(MAX(ROUND(frecuencia_residual_aux+impacto_residual_aux-1+efectividad_total_ponderada,0),1),\"\")", "DECIMAL"),
        F(29, "F29_VERIFICACION_RIESGO_RESIDUAL", "Matriz Consolidada!BL2", "IFERROR(valor_riesgo_residual-valor_riesgo_residual_aux,\"\")", "DECIMAL"),
        F(30, "F30_VRR_2", "Matriz Consolidada!BM2", "IFERROR(frecuencia_residual+impacto_residual-1,\"\")", "DECIMAL"),
        F(31, "F31_VERIFICAR_VRR_2", "Matriz Consolidada!BN2", "IFERROR(valor_riesgo_residual-vrr_2,\"\")", "DECIMAL"),
        F(32, "F32_VERIFICAR_FRECUENCIA", "Matriz Consolidada!BO2", "IFERROR(frecuencia-frecuencia_residual,\"\")", "DECIMAL"),
        F(33, "F33_VERIFICAR_IMPACTO", "Matriz Consolidada!BP2", "IFERROR(impacto-impacto_residual,\"\")", "DECIMAL"),
        F(34, "F34_DIFERENCIA_VRI_VRR", "Matriz Consolidada!BQ2", "IFERROR(valor_riesgo_inherente-valor_riesgo_residual,\"\")", "DECIMAL")
    ];

    private static InstitutionalFormulaDefinition F(int number, string code, string sourceCell, string expression, string resultType) =>
        new(number, code, sourceCell, expression, resultType);
}
