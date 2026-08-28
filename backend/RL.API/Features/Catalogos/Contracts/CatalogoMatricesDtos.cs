namespace RL.API.Features.Catalogos.Contracts;

public sealed record ElementoCatalogoMatricesDto(long Id, string Codigo, string Valor, int Orden, bool Activo);

public sealed record CatalogoMatricesDto(long Id, string Codigo, string Nombre, bool Activo, IReadOnlyList<ElementoCatalogoMatricesDto> Elementos);

public sealed record CrearCatalogoMatricesDto(string Codigo, string Nombre);
public sealed record ActualizarCatalogoMatricesDto(string Nombre, bool Activo);
public sealed record CrearElementoCatalogoMatricesDto(string Codigo, string Valor, int Orden);
public sealed record ActualizarElementoCatalogoMatricesDto(string Valor, int Orden, bool Activo);
