using HeatSafe.Api.Services;

namespace HeatSafe.Tests.Services;

public sealed class CalculadoraIndiceCalorTests
{
    private readonly CalculadoraIndiceCalor _sut = new();

    [Fact]
    public void CalcularCelsius_DeveRetornarIndiceAcimaDaTemperaturaEmCondicaoQuenteUmida()
    {
        var result = _sut.CalcularCelsius(29.0, 55.0);
        Assert.True(double.IsFinite(result));
        Assert.True(result > 29.0);
        Assert.InRange(result, 29.0, 40.0);
    }


    [Theory]
    [InlineData(-20.0)]
    [InlineData(60.0)]
    public void CalcularCelsius_DeveAceitarLimitesDaFaixaDeTemperatura(double temperature)
    {
        var result = _sut.CalcularCelsius(temperature, 50.0);

        Assert.True(double.IsFinite(result));
    }

    [Theory]
    [InlineData(-21, 50)]
    [InlineData(71, 50)]
    [InlineData(30, -1)]
    [InlineData(30, 101)]
    [InlineData(double.NaN, 50)]
    [InlineData(30, double.NaN)]
    public void CalcularCelsius_DeveRejeitarEntradasInvalidas(double temperature, double humidity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.CalcularCelsius(temperature, humidity));
    }
}
