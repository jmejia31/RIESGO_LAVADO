using System.Globalization;

namespace RL.API.Features.MatricesRiesgos.Domain;

/// <summary>
/// Preserves the percentage contract written into historical V1 evaluation JSON.
/// Values are stored as percentage points (0..100), not runtime ratios (0..1).
/// </summary>
public static class LegacyControlEffectiveness
{
    public static decimal ParsePercent0To100(object? percentageValue, object? scaleValue)
    {
        if (percentageValue is not null
            && double.TryParse(Convert.ToString(percentageValue, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out double percentage))
        {
            return percentage <= 1.0
                ? (decimal)Math.Round(percentage * 100.0)
                : (decimal)Math.Round(percentage);
        }

        string scale = Convert.ToString(scaleValue, CultureInfo.InvariantCulture)?.Trim().ToLowerInvariant() ?? string.Empty;
        return scale switch
        {
            "alta efectividad" => 90m,
            "moderado" => 85m,
            "parcialmente efectivo" => 50m,
            "razonable" => 30m,
            _ => 0m
        };
    }
}
