namespace RL.API.Tests.Support;

/// <summary>
/// Imagen de prueba sintética sin emblemas (RGB, 128 × 64).
/// No se distribuye como logo predeterminado en la aplicación.
/// </summary>
internal static class SyntheticReportLogo
{
    internal const int Width = 128;
    internal const int Height = 64;
    internal static byte[] PngBytes => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAIAAAABACAIAAABdtOgoAAAAqElEQVR4nO3RMQ0AIADAMMAJEhGEV2T0YFWwZHOfO+IsHfC7BmANwBqANQBrANYArAFYA7AGYA3AGoA1AGsA1gCsAVgDsAZgDcAagDUAawDWAKwBWAOwBmANwBqANQBrANYArAFYA7AGYA3AGoA1AGsA1gCsAVgDsAZgDcAagDUAawDWAKwBWAOwBmANwBqANQBrANYArAFYA7AGYA3AGoA1AGsA1gDsAWE8Aa/dYmoVAAAAAElFTkSuQmCC");
}
