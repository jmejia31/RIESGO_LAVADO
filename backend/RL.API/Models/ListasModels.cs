using System;

namespace RL.API.DTOs
{
    public class CoincidenciaJuridicaDto
    {
        public string Rtn { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string NumeroPatrono { get; set; } = string.Empty;
        public string ListaCoincidencia { get; set; } = string.Empty;
        public DateTime? FechaEncontro { get; set; }
        public DateTime? FechaCalifico { get; set; }
        public string EsProveedorIhss { get; set; } = string.Empty;
        public bool TieneMotivo { get; set; }
        public bool EsManual { get; set; }
    }

    public class CoincidenciaNaturalDto
    {
        public string NumeroIdentificacion { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ListaCoincidencia { get; set; } = string.Empty;
        public int TotalRepetidos { get; set; }
        public bool TieneMotivo { get; set; }
        public bool EsManual { get; set; }
    }

    public class CoincidenciaEmpleadoDto
    {
        public string Identidad { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ListaCoincidencia { get; set; } = string.Empty;
        public int TotalRepetidos { get; set; }
        public bool TieneMotivo { get; set; }
        public bool EsManual { get; set; }
    }

    public class DetalleCoincidenciaNaturalDto
    {
        public string NumeroIdentificacion { get; set; } = string.Empty;
        public string NombresPersona { get; set; } = string.Empty;
        public string TipoCondicionActuaDesc { get; set; } = string.Empty;
        public string NumeroPatronal { get; set; } = string.Empty;
        public string NombreEmpresa { get; set; } = string.Empty;
        public string EsPep { get; set; } = string.Empty;
        public string ListaCoincidencia { get; set; } = string.Empty;
        public DateTime? FechaCalifico { get; set; }
        public DateTime? FechaCoincidencia { get; set; }
    }

    public class DetalleCoincidenciaEmpleadoDto
    {
        public string Identidad { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string TipoCondicionActuaDesc { get; set; } = string.Empty;
        public string NumeroPatrono { get; set; } = string.Empty;
        public string NombreEmpresa { get; set; } = string.Empty;
        public string RazoSoci { get; set; } = string.Empty;
        public string ListaCoincidencia { get; set; } = string.Empty;
        public DateTime? FechaCalifico { get; set; }
        public DateTime? FechaCoincidencia { get; set; }
    }
}
