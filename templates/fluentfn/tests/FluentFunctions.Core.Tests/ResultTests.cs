using FluentFunctions.Core.Results;

namespace FluentFunctions.Core.Tests;

public class ResultTests
{
    [Fact]
    public void Validate_collects_every_error()
    {
        var result = Result.Validate(
            5,
            n => n > 10 ? null : Error.Validation("too.small", "Too small"),
            n => n % 2 == 0 ? null : Error.Validation("odd", "Odd")
        );

        Assert.True(result.IsFailure);
        Assert.Equal(["too.small", "odd"], result.Errors.Select(e => e.Code));
    }

    [Fact]
    public void Bind_short_circuits_on_failure()
    {
        var called = false;
        var result = Result
            .Failure<int>(Error.Validation("bad", "Bad"))
            .Bind(n =>
            {
                called = true;
                return Result.Success(n);
            });

        Assert.False(called);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Value_throws_on_failure() =>
        Assert.Throws<InvalidOperationException>(() => Result.Failure<int>(Error.Conflict("dup", "Duplicate")).Value);
}
