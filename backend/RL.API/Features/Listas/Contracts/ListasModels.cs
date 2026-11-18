using System;

namespace RL.API.Features.Listas.Contracts
{
    public class CoincidenciaJuridicaDto
    {
        public string Rtn { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string NumeroPatrono { get; set; } = string.Empty;
        public string ListaCoincidencia { get; set; } = string.Empty;
        public DateTime? FechaEncontro { get; set; }
        public DateTime? FechaCalifico { get; set; }
        public DateTime? FechaRegistroInterno { get; set; }
        public string EsProveedorIhss { get; set; } = string.Empty;
        public bool TieneMotivo { get; set; }
        public bool EsManual { get; set; }

        // El listado juridico no manual proviene de la coincidencia enlazada
        // por NUMEPATRO a PATRONOS y DATOS_EMPRESA. Las fichas manuales
        // carecen de esa prueba y no reciben estado VERIFIED por su documento.
        // Propiedades serializadas en JSON para preservar la barrera de
        // identidad que el frontend exige antes de acciones sensibles.
        public string EstadoIdentidad => IdentidadMonitoreoPolicy.EstadoParaPresentacion(
            NumeroPatrono, EsManual ? 0 : 1, EsManual ? 0 : 1);
        public string EstadoAsociacion => EstadoIdentidad;
    }

    public class CoincidenciaNaturalDto
    {
        public string NumeroIdentificacion { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ListaCoincidencia { get; set; } = string.Empty;
        public int TotalRepetidos { get; set; }
        public long IdRegistroFuente { get; set; }
        public string TipoFuente { get; set; } = string.Empty;
        public string NumeroPatronal { get; set; } = string.Empty;
        public string CategoriaIdentidad { get; set; } = "IDENTIFICADA";
        public DateTime? FechaEncontro { get; set; }
        public DateTime? FechaCalifico { get; set; }
        public DateTime? FechaRegistroInterno { get; set; }
        public bool TieneMotivo { get; set; }
        public bool EsManual { get; set; }
        public string EstadoIdentidad { get; set; } = EstadoIdentidadMonitoreo.UnverifiedAssociation;
        public string EstadoAsociacion { get; set; } = EstadoIdentidadMonitoreo.UnverifiedAssociation;
    }

    public class CoincidenciaEmpleadoDto
    {
        public string Identidad { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ListaCoincidencia { get; set; } = string.Empty;
        public int TotalRepetidos { get; set; }
        public long IdRegistroFuente { get; set; }
        public string TipoFuente { get; set; } = string.Empty;
        public string NumeroPatrono { get; set; } = string.Empty;
        public string CategoriaIdentidad { get; set; } = "IDENTIFICADA";
        public DateTime? FechaEncontro { get; set; }
        public DateTime? FechaCalifico { get; set; }
        public DateTime? FechaRegistroInterno { get; set; }
        public bool TieneMotivo { get; set; }
        public bool EsManual { get; set; }
        public string EstadoIdentidad { get; set; } = EstadoIdentidadMonitoreo.UnverifiedAssociation;
        public string EstadoAsociacion { get; set; } = EstadoIdentidadMonitoreo.UnverifiedAssociation;
    }

    public class DetalleCoincidenciaNaturalDto
    {
        public long IdRegistroFuente { get; set; }
        public string TipoFuente { get; set; } = string.Empty;
        public string NumeroIdentificacion { get; set; } = string.Empty;
        public string NombresPersona { get; set; } = string.Empty;
        public string TipoCondicionActuaDesc { get; set; } = string.Empty;
        public string NumeroPatronal { get; set; } = string.Empty;
        public string NombreEmpresa { get; set; } = string.Empty;
        public string EsPep { get; set; } = string.Empty;
        public string ListaCoincidencia { get; set; } = string.Empty;
        public DateTime? FechaCalifico { get; set; }
        public DateTime? FechaCoincidencia { get; set; }
        public string EstadoAsociacion { get; set; } = EstadoIdentidadMonitoreo.UnverifiedAssociation;
    }

    public class DetalleCoincidenciaEmpleadoDto
    {
        public long IdRegistroFuente { get; set; }
        public string TipoFuente { get; set; } = string.Empty;
        public DateTime? Periodo { get; set; }
        public string Identidad { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string TipoCondicionActuaDesc { get; set; } = string.Empty;
        public string NumeroPatrono { get; set; } = string.Empty;
        public string NombreEmpresa { get; set; } = string.Empty;
        public string RazoSoci { get; set; } = string.Empty;
        public string ListaCoincidencia { get; set; } = string.Empty;
        public DateTime? FechaCalifico { get; set; }
        public DateTime? FechaCoincidencia { get; set; }
        public string EstadoAsociacion { get; set; } = EstadoIdentidadMonitoreo.UnverifiedAssociation;
    }
}
