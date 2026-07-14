namespace RL.API.Features.Listas.Contracts;

public sealed record TipoListaCautelaCreadaDto(
    int TipoListaCautelaId,
    string Descripcion,
    string? TipoArchivo,
    int? CantidadColumnas);

public sealed record EvidenciaPoliticaDto(
    int MaximoMb,
    long MaximoBytes,
    string[] ExtensionesPermitidas,
    string TiposPermitidosTexto);

public sealed record EvidenciaDescargaDto(byte[] Bytes, string Mime, string Nombre);
