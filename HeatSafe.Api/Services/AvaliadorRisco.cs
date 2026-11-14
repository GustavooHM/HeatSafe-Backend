using HeatSafe.Api.Domain;

namespace HeatSafe.Api.Services;

public sealed class AvaliadorRisco : IAvaliadorRisco
{
    public NivelRisco Classificar(double indiceCalorC)
    {
        if (!double.IsFinite(indiceCalorC)) throw new ArgumentOutOfRangeException(nameof(indiceCalorC));
        if (indiceCalorC < 27.0) return NivelRisco.NORMAL;
        if (indiceCalorC < 32.0) return NivelRisco.ATENCAO;
        if (indiceCalorC < 41.0) return NivelRisco.CAUTELA_EXTREMA;
        if (indiceCalorC <= 54.0) return NivelRisco.PERIGO;
        return NivelRisco.PERIGO_EXTREMO;
    }
}
