using FluentValidation.TestHelper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Validators;

namespace KamSquare.KamScore.Application.UnitTest.Validators;

public class GenerateCourtsDtoValidatorTests
{
    private readonly GenerateCourtsDtoValidator _validator = new();

    [Fact]
    public void ValidCount_ShouldPassValidation()
    {
        var dto = new GenerateCourtsDto(10);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CountZero_ShouldFailValidation()
    {
        var dto = new GenerateCourtsDto(0);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public void CountOver20_ShouldFailValidation()
    {
        var dto = new GenerateCourtsDto(21);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public void CountOne_ShouldPassValidation()
    {
        var dto = new GenerateCourtsDto(1);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CountTwenty_ShouldPassValidation()
    {
        var dto = new GenerateCourtsDto(20);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
