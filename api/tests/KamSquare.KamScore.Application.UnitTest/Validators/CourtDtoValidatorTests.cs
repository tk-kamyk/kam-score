using FluentValidation.TestHelper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Validators;

namespace KamSquare.KamScore.Application.UnitTest.Validators;

public class CourtDtoValidatorTests
{
    private readonly CourtDtoValidator _validator = new();

    [Fact]
    public void Valid_Dto_ShouldPassValidation()
    {
        var dto = new CourtDto(null, "Court A");

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_ShouldFailValidation()
    {
        var dto = new CourtDto(null, "");

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameExceeding100Characters_ShouldFailValidation()
    {
        var dto = new CourtDto(null, new string('A', 101));

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameExactly100Characters_ShouldPassValidation()
    {
        var dto = new CourtDto(null, new string('A', 100));

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
}
