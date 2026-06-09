namespace Outlet.Kernel.Shared.UnitTests;

public sealed class EntityTests
{
    private sealed class TestEntity(Guid id) : Entity<Guid>(id)
    {
    }

    private sealed class OtherEntity(Guid id) : Entity<Guid>(id)
    {
    }

    [Fact]
    public void Equals_Should_ReturnTrue_When_SameId()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_DifferentId()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        a.Equals(b).Should().BeFalse();
        a.Equals((object)b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_ReturnTrue_When_SameInstance()
    {
        var a = new TestEntity(Guid.NewGuid());

        a.Equals(a).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_OtherIsNull()
    {
        var a = new TestEntity(Guid.NewGuid());
        TestEntity? typedNull = null;
        object? boxedNull = null;

        a.Equals(typedNull).Should().BeFalse();
        a.Equals(boxedNull).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_OtherIsNotEntity()
    {
        var a = new TestEntity(Guid.NewGuid());

        a.Equals("not an entity").Should().BeFalse();
        a.Equals(42).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_BeBasedOnId()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        a.GetHashCode().Should().Be(b.GetHashCode());
        a.GetHashCode().Should().Be(id.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Should_Differ_When_DifferentIds()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void EqualOperator_Should_ReturnTrue_When_BothNull()
    {
        TestEntity? left = null;
        TestEntity? right = null;

        (left == right).Should().BeTrue();
    }

    [Fact]
    public void EqualOperator_Should_ReturnFalse_When_LeftIsNull()
    {
        TestEntity? left = null;
        var right = new TestEntity(Guid.NewGuid());

        (left == right).Should().BeFalse();
    }

    [Fact]
    public void EqualOperator_Should_ReturnFalse_When_RightIsNull()
    {
        var left = new TestEntity(Guid.NewGuid());
        TestEntity? right = null;

        (left == right).Should().BeFalse();
    }

    [Fact]
    public void EqualOperator_Should_ReturnTrue_When_SameId()
    {
        var id = Guid.NewGuid();
        var left = new TestEntity(id);
        var right = new TestEntity(id);

        (left == right).Should().BeTrue();
    }

    [Fact]
    public void EqualOperator_Should_ReturnFalse_When_DifferentIds()
    {
        var left = new TestEntity(Guid.NewGuid());
        var right = new TestEntity(Guid.NewGuid());

        (left == right).Should().BeFalse();
    }

    [Fact]
    public void NotEqualOperator_Should_BeOppositeOfEqual()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);
        var c = new TestEntity(Guid.NewGuid());

        (a != b).Should().BeFalse();
        (a != c).Should().BeTrue();
        ((TestEntity?)null != null).Should().BeFalse();
    }
}
