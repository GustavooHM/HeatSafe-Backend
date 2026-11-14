using HeatSafe.Api.Domain;

namespace HeatSafe.Api.Services;

/// <summary>Implementação da rotina Heat Index NOAA/Rothfusz, com temperaturas de entrada e saída em Celsius.</summary>
public sealed class CalculadoraIndiceCalor : ICalculadoraIndiceCalor
{
    public double CalcularCelsius(double temperaturaC, double umidade)
    {
        if (!double.IsFinite(temperaturaC) || temperaturaC is < -20 or > 60)
            throw new ArgumentOutOfRangeException(nameof(temperaturaC), "Temperatura fora da faixa aceita (-20 °C a 60 °C).");
        if (!double.IsFinite(umidade) || umidade is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(umidade), "Umidade deve estar entre 0% e 100%. ");

        var t = temperaturaC * 9.0 / 5.0 + 32.0;
        var rh = umidade;

        // Equação simples de Steadman usada pela NOAA como primeira aproximação.
        var simple = 0.5 * (t + 61.0 + ((t - 68.0) * 1.2) + (rh * 0.094));
        var apparentF = (simple + t) / 2.0;
        if (apparentF < 80.0) return ToCelsius(apparentF);

        var hi = -42.379
            + 2.04901523 * t
            + 10.14333127 * rh
            - 0.22475541 * t * rh
            - 0.00683783 * t * t
            - 0.05481717 * rh * rh
            + 0.00122874 * t * t * rh
            + 0.00085282 * t * rh * rh
            - 0.00000199 * t * t * rh * rh;

        if (rh < 13.0 && t is >= 80.0 and <= 112.0)
        {
            var adjustment = ((13.0 - rh) / 4.0) * Math.Sqrt(Math.Max(0.0, (17.0 - Math.Abs(t - 95.0)) / 17.0));
            hi -= adjustment;
        }
        else if (rh > 85.0 && t is >= 80.0 and <= 87.0)
        {
            var adjustment = ((rh - 85.0) / 10.0) * ((87.0 - t) / 5.0);
            hi += adjustment;
        }

        return ToCelsius(hi);
    }

    private static double ToCelsius(double fahrenheit) => Math.Round((fahrenheit - 32.0) * 5.0 / 9.0, 2, MidpointRounding.AwayFromZero);
}
