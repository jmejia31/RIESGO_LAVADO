using System;
using System.ComponentModel.DataAnnotations;

namespace RL.API.DTOs
{
    public class TipoDocumentoDto
    {
        public int TipoDocumentoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }

    public class TipoListaCautelaDto
    {
        public int TipoListaCautelaId { get; set; }

        [Required(ErrorMessage = "La descripción es requerida")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de archivo es requerido")]
        public string? TipoArchivo { get; set; }

        [Required(ErrorMessage = "La cantidad de columnas es requerida")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad de columnas debe ser mayor a 0")]
        public int? CantidadColumnas { get; set; }
    }

    public class RegistrarPositivoDto
    {
        [Required(ErrorMessage = "El tipo de documento es requerido")]
        public int TipoDocumentoId { get; set; }

        [Required(ErrorMessage = "El tipo de persona es requerido")]
        public int TipoPositivoId { get; set; }

        public string? NoDocumento { get; set; }

        [Required(ErrorMessage = "El nombre completo es requerido")]
        [MaxLength(255)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El motivo de ingreso es requerido")]
        [MaxLength(1000, ErrorMessage = "El motivo de ingreso no debe superar los 1000 caracteres")]
        public string MotivoIngreso { get; set; } = string.Empty;

        public int? TipoListaCautelaId { get; set; }

        // Origen operativo del registro; permite identificar casos creados por noticia sin crear un módulo aparte.
        [MaxLength(50)]
        [RegularExpression("^(DNP_LISTAS|MANUAL_CUMPLIMIENTO|NOTICIA_PRENSA|OTRO)$", ErrorMessage = "El origen del registro no es valido")]
        public string? OrigenRegistro { get; set; }
    }

    public class ExistingPositivoDto
    {
        public int TipoDocumentoId { get; set; }
        public string MotivoIngreso { get; set; } = string.Empty;
        public int? TipoListaCautelaId { get; set; }
        public string? OrigenRegistro { get; set; }
        public DateTime? FechaRegistroInterno { get; set; }
    }

    // DTO usado por eliminaciones logicas que deben quedar justificadas en auditoria.
    public class MotivoEliminacionDto
    {
        [Required(ErrorMessage = "El motivo de eliminacion es requerido")]
        [MaxLength(1000, ErrorMessage = "El motivo de eliminacion no debe superar los 1000 caracteres")]
        public string MotivoEliminacion { get; set; } = string.Empty;
    }
}
