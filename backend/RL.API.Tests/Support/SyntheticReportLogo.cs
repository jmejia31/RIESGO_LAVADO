namespace RL.API.Tests.Support;

/// <summary>
/// Imagen de prueba sintética sin emblemas (RGB, 128 × 64).
/// No se distribuye como logo predeterminado en la aplicación.
/// </summary>
internal static class SyntheticReportLogo
{
    internal const int Width = 128;
    internal const int Height = 64;
    internal static byte[] PngBytes => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAIAAAABACAIAAABdtOgoAAAA8UlEQVR4nO3TQQ3EMBAEQYMICMMJnAMWJEZ0IPzYGak+DaClWs+7n/fX2/Pu09w1fvCy4wcvu8YPEhDxkYDWjh8kIOIjAa0lgAACEj4S0NrxgwREfCSgtQQQQEDCRwJaO36QgIiPBLSWAAIISPhIQGvHDxIQ8ZGA1hJAAAEJHwlo7fhBAiI+EtBaAsYFfPv5fr093z7NXeMHLzt+8LIEEEBAwkcCWjt+kICIjwS0lgACCEj4SEBrxw8SEPGRgNYSQAABCR8JaO34QQIiPhLQWgIIICDhIwGtHT9IQMRHAlpLAAEEJHwkoLXjBwmI+EhAa/9CNqJZyecOcwAAAABJRU5ErkJggg==");
}
