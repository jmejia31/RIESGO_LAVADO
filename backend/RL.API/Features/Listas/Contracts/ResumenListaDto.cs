using System;

namespace RL.API.Features.Listas.Contracts
{
    public class ResumenListaDto
    {
        public int TipoListaCautelaId { get; set; }
        public string Lista { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public DateTime? FechaCreacion { get; set; }
        public int CantidadRegistros { get; set; }
    }
}
