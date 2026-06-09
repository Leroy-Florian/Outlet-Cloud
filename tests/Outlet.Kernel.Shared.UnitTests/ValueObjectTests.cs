namespace Outlet.Kernel.Shared.UnitTests;

public sealed class ValueObjectTests
{
    private sealed class Money(int amount, string currency) : ValueObject
    {
        public int Amount { get; } = amount;
        public string Currency { get; } = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class WithNull(string? value) : ValueObject
    {
        public string? Value { get; } = value;
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }

    private sealed class DifferentType(int amount) : ValueObject
    {
        public int Amount { get; } = amount;
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
        }
    }

    [Fact]
    public void Equals_Should_ReturnTrue_When_SameComponents()
    {
        var a = new Money(100, "EUR");
        var b = new Money(100, "EUR");

        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_DifferentComponents()
    {
        var a = new Money(100, "EUR");
        var b = new Money(200, "EUR");
        var c = new Money(100, "USD");

        a.Equals(b).Should().BeFalse();
        a.Equals(c).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_OtherIsNull()
    {
        var a = new Money(100, "EUR");
        ValueObject? typedNull = null;
        object? boxedNull = null;

        a.Equals(typedNull).Should().BeFalse();
        a.Equals(boxedNull).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_OtherIsDifferentType()
    {
        var a = new Money(100, "EUR");
        var b = new DifferentType(100);

        a.Equals(b).Should().BeFalse();
        a.Equals((object)b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_OtherIsUnrelatedObject()
    {
        var a = new Money(100, "EUR");

        a.Equals("100 EUR").Should().BeFalse();
        a.Equals(100).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_BeSame_When_SameComponents()
    {
        var a = new Money(100, "EUR");
        var b = new Money(100, "EUR");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Should_HandleNullComponents()
    {
        var a = new WithNull(null);
        var b = new WithNull(null);
        var c = new WithNull("x");

        a.GetHashCode().Should().Be(b.GetHashCode());
        a.GetHashCode().Should().NotBe(c.GetHashCode());
    }

    [Fact]
    public void EqualOperator_Should_ReturnTrue_When_BothNull()
    {
        Money? a = null;
        Money? b = null;

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void EqualOperator_Should_ReturnFalse_When_OnlyOneNull()
    {
        var v = new Money(100, "EUR");

        ((Money?)null == v).Should().BeFalse();
        (v == (Money?)null).Should().BeFalse();
    }

    [Fact]
    public void EqualOperator_Should_ReturnTrue_When_SameComponents()
    {
        var a = new Money(100, "EUR");
        var b = new Money(100, "EUR");

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void NotEqualOperator_Should_BeOppositeOfEqual()
    {
        var a = new Money(100, "EUR");
        var b = new Money(100, "EUR");
        var c = new Money(200, "EUR");

        (a != b).Should().BeFalse();
        (a != c).Should().BeTrue();
    }
}
