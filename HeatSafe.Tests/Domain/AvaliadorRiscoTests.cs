using HeatSafe.Api.Domain;
using HeatSafe.Api.Services;

namespace HeatSafe.Tests.Domain;

public sealed class AvaliadorRiscoTests
{
    private readonly AvaliadorRisco _sut = new();

    [Theory]
    [InlineData(26.99, NivelRisco.NORMAL)]
    [InlineData(27.00, NivelRisco.ATENCAO)]
    [InlineData(31.99, NivelRisco.ATENCAO)]
    [InlineData(32.00, NivelRisco.CAUTELA_EXTREMA)]
    [InlineData(40.99, NivelRisco.CAUTELA_EXTREMA)]
    [InlineData(41.00, NivelRisco.PERIGO)]
    [InlineData(53.99, NivelRisco.PERIGO)]
    [InlineData(54.00, NivelRisco.PERIGO_EXTREMO)]
    public void Classificar_DeveRespeitarOsLimitesFuncionais(double heat, NivelRisco expected)
    {
        Assert.Equal(expected, _sut.Classificar(heat));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Classificar_DeveRejeitarValoresNaoFinitos(double heat)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Classificar(heat));
    }
}
