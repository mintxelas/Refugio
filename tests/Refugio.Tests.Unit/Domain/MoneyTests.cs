using Refugio.Domain.Common;

namespace Refugio.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Ctor_RejectsNegativeValues() =>
        Assert.Throws<ArgumentException>(() => new Money(-0.01m));

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(1000)]
    public void Ctor_AcceptsZeroAndPositiveValues(decimal value) =>
        Assert.Equal(value, new Money(value).Value);

    [Fact]
    public void ImplicitConversion_ToAndFromDecimal_RoundTrips()
    {
        Money money = 42.5m;
        decimal value = money;
        Assert.Equal(42.5m, value);
    }
}
