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

        [Required(ErrorMessage = "El tipo de lista es requerido")]
        public int TipoPositivoId { get; set; }

        public string? NoDocumento { get; set; }

        [Required(ErrorMessage = "El nombre completo es requerido")]
        [MaxLength(255)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El motivo de ingreso es requerido")]
        [MaxLength(4000)]
        public string MotivoIngreso { get; set; } = string.Empty;

        public int? TipoListaCautelaId { get; set; }
    }

    public class ExistingPositivoDto
    {
        public int TipoDocumentoId { get; set; }
        public string MotivoIngreso { get; set; } = string.Empty;
        public int? TipoListaCautelaId { get; set; }
    }
}
