using AppName.Common;

namespace AppName.UnitTests.Common;

public sealed class ResultTests
{
    private static readonly Error Failed = new("Failed", "Something failed");

    [Fact]
    public void A_success_has_no_error()
    {
        var result = Result.Success();

        result.Successful.Should().BeTrue();
        ((bool)result).Should().BeTrue();
    }

    [Fact]
    public void A_failure_keeps_its_error()
    {
        var result = Result.Failure(Failed);

        result.Successful.Should().BeFalse();
        result.Error.Should().BeSameAs(Failed);
    }

    [Fact]
    public void A_failure_from_a_name_and_a_message_makes_the_error()
    {
        var result = Result.Failure("Failed", "Something failed");

        result.Error.Name.Should().Be("Failed");
        result.Error.Message.Should().Be("Something failed");
    }

    [Fact]
    public void A_value_and_an_error_convert_to_a_result()
    {
        Result<int> success = 42;
        Result<int> failure = Failed;

        success.Successful.Should().BeTrue();
        success.Value.Should().Be(42);
        failure.Successful.Should().BeFalse();
        failure.Error.Should().BeSameAs(Failed);
    }

    [Fact]
    public void The_value_of_a_failure_cannot_be_read()
    {
        var result = Result.Failure<int>(Failed);

        var read = () => result.Value;

        read.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithMessage_keeps_the_name_and_the_details()
    {
        var error = new Error("Failed", "Old message", details: 7);

        var changed = error.WithMessage("New message");

        changed.Name.Should().Be("Failed");
        changed.Message.Should().Be("New message");
        changed.Details.Should().Be(7);
    }

    [Fact]
    public void An_error_converts_to_its_name()
    {
        string name = Failed;

        name.Should().Be("Failed");
    }
}

public sealed class ResultExtensionsTests
{
    private static readonly Error Failed = new("Failed", "Something failed");

    private static readonly Error Rejected = new("Rejected", "The value is not accepted");

    [Fact]
    public void Ensure_keeps_a_value_that_passes()
    {
        var result = Result.Success(5).Ensure(x => x > 0, Rejected);

        result.Value.Should().Be(5);
    }

    [Fact]
    public void Ensure_fails_a_value_that_does_not_pass()
    {
        var result = Result.Success(-5).Ensure(x => x > 0, Rejected);

        result.Error.Should().BeSameAs(Rejected);
    }

    [Fact]
    public void Ensure_keeps_the_first_error_and_does_not_run_the_check()
    {
        var checkedValue = false;

        var result = Result.Failure<int>(Failed).Ensure(_ => checkedValue = true, Rejected);

        result.Error.Should().BeSameAs(Failed);
        checkedValue.Should().BeFalse();
    }

    [Fact]
    public void Map_changes_the_value_of_a_success()
    {
        var result = Result.Success(5).Map(x => x.ToString());

        result.Value.Should().Be("5");
    }

    [Fact]
    public void Map_passes_the_error_of_a_failure()
    {
        var result = Result.Failure<int>(Failed).Map(x => x.ToString());

        result.Error.Should().BeSameAs(Failed);
    }

    [Fact]
    public async Task Bind_runs_the_next_step_only_after_a_success()
    {
        var bound = await Result.Success(5).Bind(x => Task.FromResult(Result.Success(x * 2)));
        var skipped = await Result.Failure<int>(Failed).Bind(x => Task.FromResult(Result.Success(x * 2)));

        bound.Value.Should().Be(10);
        skipped.Error.Should().BeSameAs(Failed);
    }

    [Fact]
    public async Task Bind_to_a_result_without_a_value_passes_the_error()
    {
        var bound = await Result.Success(5).Bind(_ => Task.FromResult(Result.Success()));
        var skipped = await Result.Failure<int>(Failed).Bind(_ => Task.FromResult(Result.Success()));

        bound.Successful.Should().BeTrue();
        skipped.Error.Should().BeSameAs(Failed);
    }

    [Fact]
    public async Task Match_calls_the_branch_of_the_result()
    {
        Result.Success(5).Match(x => $"value {x}", e => e.Name).Should().Be("value 5");
        Result.Failure<int>(Failed).Match(x => $"value {x}", e => e.Name).Should().Be("Failed");
        Result.Success().Match(() => "ok", e => e.Name).Should().Be("ok");
        Result.Failure(Failed).Match(() => "ok", e => e.Name).Should().Be("Failed");

        (await Task.FromResult(Result.Success()).Match(() => "ok", e => e.Name)).Should().Be("ok");
        (await Task.FromResult(Result.Failure<int>(Failed)).Match(x => $"value {x}", e => e.Name)).Should().Be("Failed");
    }
}
