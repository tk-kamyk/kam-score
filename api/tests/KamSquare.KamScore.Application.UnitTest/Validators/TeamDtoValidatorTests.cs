using FluentValidation.TestHelper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Validators;

namespace KamSquare.KamScore.Application.UnitTest.Validators;

public class TeamDtoValidatorTests
{
    private readonly TeamDtoValidator _validator = new();

    [Fact]
    public void Valid_Dto_ShouldPassValidation()
    {
        var dto = new TeamDto(null, "Eagles", 75, "eagles@example.com", "+123456789");

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_Dto_WithoutOptionalFields_ShouldPassValidation()
    {
        var dto = new TeamDto(null, "Eagles", 50, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_ShouldFailValidation()
    {
        var dto = new TeamDto(null, "", 50, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameExceeding100Characters_ShouldFailValidation()
    {
        var dto = new TeamDto(null, new string('A', 101), 50, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void LevelBelowZero_ShouldFailValidation()
    {
        var dto = new TeamDto(null, "Eagles", -1, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Level);
    }

    [Fact]
    public void LevelAbove100_ShouldFailValidation()
    {
        var dto = new TeamDto(null, "Eagles", 101, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Level);
    }

    [Fact]
    public void LevelZero_ShouldPassValidation()
    {
        var dto = new TeamDto(null, "Eagles", 0, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Level);
    }

    [Fact]
    public void Level100_ShouldPassValidation()
    {
        var dto = new TeamDto(null, "Eagles", 100, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Level);
    }

    [Fact]
    public void InvalidEmail_ShouldFailValidation()
    {
        var dto = new TeamDto(null, "Eagles", 50, "not-an-email", null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ValidEmail_ShouldPassValidation()
    {
        var dto = new TeamDto(null, "Eagles", 50, "eagles@example.com", null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void NullEmail_ShouldPassValidation()
    {
        var dto = new TeamDto(null, "Eagles", 50, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void InvalidPhone_ShouldFailValidation()
    {
        var dto = new TeamDto(null, "Eagles", 50, null, "abc");

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void ValidPhone_ShouldPassValidation()
    {
        var dto = new TeamDto(null, "Eagles", 50, null, "+48 123 456 789");

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void NullPhone_ShouldPassValidation()
    {
        var dto = new TeamDto(null, "Eagles", 50, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }
}
