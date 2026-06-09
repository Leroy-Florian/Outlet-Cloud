namespace Outlet.Kernel.Shared.UnitTests;

public sealed class GuardTests
{
    private const string InvalidId = "test.invalid_id";

    [Fact]
    public void Should_ReturnSuccess_When_FactoryCompletes()
    {
        var result = Guard.TryBuild(() => 42, InvalidId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Should_ReturnFailureWithError_When_FactoryThrowsArgumentException()
    {
        var result = Guard.TryBuild<int>(
            () => throw new ArgumentException("bad input"),
            InvalidId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InvalidId);
    }

    [Fact]
    public void Should_ReturnFailureWithError_When_FactoryThrowsArgumentNullException()
    {
        var result = Guard.TryBuild<string>(
            () => throw new ArgumentNullException("value"),
            InvalidId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InvalidId);
    }

    [Fact]
    public void Should_PropagateUnrelatedExceptions()
    {
        var act = () => Guard.TryBuild<int>(
            () => throw new InvalidOperationException("infra failure"),
            InvalidId);

        act.Should().Throw<InvalidOperationException>();
    }
}
