using System;

namespace RL.API.Features.MatricesRiesgos.Contracts;

public sealed class EvaluacionRiesgoDto
{
    public long EvaId { get; set; }
    public long EvaRiesgoId { get; set; }
    public long EvaVersionId { get; set; }
    public string EvaEstado { get; set; } = string.Empty; // 'BORRADOR', 'EN_REVISION', 'OBSERVADA', 'APROBADA', 'RECHAZADA', 'CERRADA'
    public string EvaDataJson { get; set; } = string.Empty; // Respuestas capturadas por el usuario
    public string EvaDataCalcJson { get; set; } = string.Empty; // Campos calculados en el backend
    public int? EvaVri { get; set; } // Valor de Riesgo Inherente
    public decimal? EvaEtp { get; set; } // Eficacia Total del Plan / Mitigación
    public int? EvaVrr { get; set; } // Valor de Riesgo Residual
    public DateTime EvaFechaEval { get; set; }
    public long EvaUsrEval { get; set; }
    public int EvaVersionRow { get; set; } // Control de concurrencia optimista
    public bool EvaActivo { get; set; }
}
