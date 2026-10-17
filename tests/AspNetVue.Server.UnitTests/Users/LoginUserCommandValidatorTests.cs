using FluentValidation.TestHelper;
using AspNetVue.Server.Features.Users.Login;

namespace AspNetVue.Server.UnitTests.Users;

public sealed class LoginUserCommandValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validator_Should_HaveError_WhenEmailIsEmpty()
    {
        var command = new LoginCommand(string.Empty, "Password123");

        TestValidationResult<LoginCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validator_Should_HaveError_WhenEmailIsInvalid()
    {
        var command = new LoginCommand("not-an-email", "Password123");

        TestValidationResult<LoginCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validator_Should_HaveError_WhenPasswordIsEmpty()
    {
        var command = new LoginCommand("test@example.com", string.Empty);

        TestValidationResult<LoginCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Validator_Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new LoginCommand("test@example.com", "Password123");

        TestValidationResult<LoginCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
