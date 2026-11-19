using AppName.ServiceName.Application.Common.Behaviors;
using AppName.Common.Errors;
using AppName.Common;
using FluentValidation;
using MediatR;

namespace AppName.ServiceName.UnitTests.Common;

/// <summary>
/// The validation pipeline turns validation failures into a failed result, so the endpoint answers 400
/// without an exception.
/// </summary>
public sealed class ValidationBehaviorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_valid_request_reaches_the_handler()
    {
        var behavior = new ValidationBehavior<Command, Result>([new CommandValidator()]);
        var called = false;

        var result = await behavior.Handle(new Command("Name"), _ =>
        {
            called = true;
            return Task.FromResult(Result.Success());
        }, Ct);

        called.Should().BeTrue();
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public async Task A_request_without_validators_reaches_the_handler()
    {
        var behavior = new ValidationBehavior<Command, Result>([]);

        var result = await behavior.Handle(new Command(""), _ => Task.FromResult(Result.Success()), Ct);

        result.Successful.Should().BeTrue();
    }

    [Fact]
    public async Task An_invalid_request_gets_a_failed_result_with_the_code_of_the_rule()
    {
        var behavior = new ValidationBehavior<Command, Result>([new CommandValidator()]);

        var result = await behavior.Handle(new Command(""), _ => throw new InvalidOperationException("The handler must not run."), Ct);

        result.Successful.Should().BeFalse();
        var error = result.Error.Should().BeOfType<ValidationError>().Subject;
        error.Name.Should().Be("NameRequired");
        error.Errors.Should().ContainKey(nameof(Command.Name));
    }

    [Fact]
    public async Task An_invalid_request_with_a_value_result_gets_a_failed_result_of_that_type()
    {
        var behavior = new ValidationBehavior<ValueQuery, Result<int>>([new ValueQueryValidator()]);

        var result = await behavior.Handle(new ValueQuery(-1, ""), _ => throw new InvalidOperationException("The handler must not run."), Ct);

        result.Successful.Should().BeFalse();
        var error = result.Error.Should().BeOfType<ValidationError>().Subject;

        // Two failures give the general code, with the messages of each property.
        error.Name.Should().Be("ValidationFailed");
        error.Errors.Should().ContainKeys(nameof(ValueQuery.Number), nameof(ValueQuery.Text));
    }

    [Fact]
    public async Task An_invalid_request_with_another_response_type_throws()
    {
        var behavior = new ValidationBehavior<TextQuery, string>([new TextQueryValidator()]);

        var act = () => behavior.Handle(new TextQuery(""), _ => Task.FromResult("text"), Ct);

        await act.Should().ThrowAsync<ValidationException>();
    }

    public sealed record Command(string Name) : IRequest<Result>;

    public sealed record ValueQuery(int Number, string Text) : IRequest<Result<int>>;

    public sealed record TextQuery(string Text) : IRequest<string>;

    private sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator() =>
            RuleFor(x => x.Name).NotEmpty().WithErrorCode("NameRequired");
    }

    private sealed class ValueQueryValidator : AbstractValidator<ValueQuery>
    {
        public ValueQueryValidator()
        {
            RuleFor(x => x.Number).GreaterThan(0);
            RuleFor(x => x.Text).NotEmpty();
        }
    }

    private sealed class TextQueryValidator : AbstractValidator<TextQuery>
    {
        public TextQueryValidator() =>
            RuleFor(x => x.Text).NotEmpty();
    }
}
