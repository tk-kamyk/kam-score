using FluentValidation.TestHelper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Validators;

namespace KamSquare.KamScore.Application.UnitTest.Validators;

public class LoginRequestDtoValidatorTests
{
    private readonly LoginRequestDtoValidator _validator = new();

    [Fact]
    public void Valid_Dto_ShouldPassValidation()
    {
        var dto = new LoginRequestDto("admin", "password");

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyUsername_ShouldFailValidation(string? username)
    {
        var dto = new LoginRequestDto(username!, "password");

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyPassword_ShouldFailValidation(string? password)
    {
        var dto = new LoginRequestDto("admin", password!);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public void ShortUsername_ShouldFailValidation(string username)
    {
        var dto = new LoginRequestDto(username, "password");

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be at least 3 characters.");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public void ShortPassword_ShouldFailValidation(string password)
    {
        var dto = new LoginRequestDto("admin", password);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 3 characters.");
    }
}
