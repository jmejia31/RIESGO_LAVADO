using System;
using System.Collections.Generic;

namespace RL.API.Features.Auditoria.Contracts
{
    public class AuditoriaDto
    {
        public long AudId { get; set; }
        public string Tabla { get; set; } = string.Empty;
        public string RegistroId { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string? DatosAnt { get; set; }
        public string? DatosNvo { get; set; }
        public long? UsrId { get; set; }
        public string? UsrEmail { get; set; }
        public string? Ip { get; set; }
        public DateTime Fecha { get; set; }
        public string? Modulo { get; set; }
    }

    public class AuditoriaPaginadoDto
    {
        public List<AuditoriaDto> Datos { get; set; } = new();
        public int TotalRegistros { get; set; }
    }

    public class RegistrarExportacionAuditoriaDto
    {
        public string Tabla { get; set; } = string.Empty;
        public string RegistroId { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public Dictionary<string, object?> Detalle { get; set; } = new();
    }
}
