using System;

namespace RL.API.DTOs
{
    public class CoincidenciaPatronoResumenDto
    {
        public DateTime? FechaEncontro { get; set; }
        public int CantidadRegistros { get; set; }
    }

    public class CoincidenciaPatronoDetalleDto
    {
        public long ReporteCoincidenciaId { get; set; }
        public long DataId { get; set; }
        public string Dni { get; set; } = string.Empty;
        public DateTime? FechaEncontro { get; set; }
        public string ListaCoincidencia { get; set; } = string.Empty;
        public string Nacionalidad { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string NumeroPatrono { get; set; } = string.Empty;
        public string ObservacionLista { get; set; } = string.Empty;
        public string TipoPersona { get; set; } = string.Empty;
        public long UsuarioEncontro { get; set; }
        public string TipoCalificacion { get; set; } = string.Empty;
    }
}
