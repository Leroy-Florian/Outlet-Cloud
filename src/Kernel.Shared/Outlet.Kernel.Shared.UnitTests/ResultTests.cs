namespace Outlet.Kernel.Shared.UnitTests;

public sealed class ResultTests
{
    private const string TestError = "test.error";

    [Fact]
    public void Generic_Success_Should_SetIsSuccessAndValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Generic_Failure_Should_SetIsFailureAndError()
    {
        var result = Result<int>.Failure(TestError);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TestError);
    }

    [Fact]
    public void Match_Should_CallOnSuccess_When_Success()
    {
        var result = Result<int>.Success(5);

        var mapped = result.Match(v => v * 2, _ => -1);

        mapped.Should().Be(10);
    }

    [Fact]
    public void Match_Should_CallOnFailure_When_Failure()
    {
        var result = Result<int>.Failure(TestError);

        var mapped = result.Match(v => v * 2, err => -err.Length);

        mapped.Should().Be(-TestError.Length);
    }

    [Fact]
    public void Map_Should_TransformValue_When_Success()
    {
        var result = Result<int>.Success(5);

        var mapped = result.Map(v => v.ToString());

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("5");
    }

    [Fact]
    public void Map_Should_PropagateError_When_Failure()
    {
        var result = Result<int>.Failure(TestError);

        var mapped = result.Map(v => v.ToString());

        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be(TestError);
    }

    [Fact]
    public async Task MapAsync_Should_TransformValue_When_Success()
    {
        var result = Result<int>.Success(5);

        var mapped = await result.MapAsync(v => Task.FromResult(v.ToString()));

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("5");
    }

    [Fact]
    public async Task MapAsync_Should_PropagateError_When_Failure()
    {
        var result = Result<int>.Failure(TestError);

        var mapped = await result.MapAsync(v => Task.FromResult(v.ToString()));

        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be(TestError);
    }

    [Fact]
    public void NonGeneric_Success_Should_SetIsSuccess()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void NonGeneric_Failure_Should_SetError()
    {
        var result = Result.Failure(TestError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TestError);
    }

    [Fact]
    public void NonGeneric_Match_Should_CallOnSuccess_When_Success()
    {
        var result = Result.Success();

        var output = result.Match(() => "ok", _ => "fail");

        output.Should().Be("ok");
    }

    [Fact]
    public void NonGeneric_Match_Should_CallOnFailure_When_Failure()
    {
        var result = Result.Failure(TestError);

        var output = result.Match(() => "ok", err => err);

        output.Should().Be(TestError);
    }

    [Fact]
    public void Static_Success_T_Should_CreateGenericSuccess()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Static_Failure_T_Should_CreateGenericFailure()
    {
        var result = Result.Failure<int>(TestError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TestError);
    }
}
