using System;
using System.Collections.Generic;

namespace RL.API.Features.Listas.Contracts
{
    public class EvidenciaDto
    {
        public long EvidenciaId { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public string TipoMime { get; set; } = string.Empty;
    }

    public class SeguimientoDto
    {
        public long DetalleListaId { get; set; }
        public long PositivoId { get; set; }
        public string MotivoIngreso { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public long UsrCreacionId { get; set; }
        public string UsrEmail { get; set; } = string.Empty;
        public List<EvidenciaDto> Evidencias { get; set; } = new();
    }
}
