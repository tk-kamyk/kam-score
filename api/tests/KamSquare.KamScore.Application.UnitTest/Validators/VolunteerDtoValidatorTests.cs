using FluentValidation.TestHelper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Validators;

namespace KamSquare.KamScore.Application.UnitTest.Validators;

public class VolunteerDtoValidatorTests
{
    private readonly VolunteerDtoValidator _validator = new();

    [Fact]
    public void Valid_Dto_WithAllFields_ShouldPassValidation()
    {
        var dto = new VolunteerDto(null, "John Doe", "john@email.com", "team-1");

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_Dto_WithOnlyName_ShouldPassValidation()
    {
        var dto = new VolunteerDto(null, "John Doe", null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_ShouldFailValidation()
    {
        var dto = new VolunteerDto(null, "", null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameExceeding200Characters_ShouldFailValidation()
    {
        var dto = new VolunteerDto(null, new string('A', 201), null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameExactly200Characters_ShouldPassValidation()
    {
        var dto = new VolunteerDto(null, new string('A', 200), null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ContactExceeding500Characters_ShouldFailValidation()
    {
        var dto = new VolunteerDto(null, "John Doe", new string('A', 501), null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Contact);
    }

    [Fact]
    public void ContactExactly500Characters_ShouldPassValidation()
    {
        var dto = new VolunteerDto(null, "John Doe", new string('A', 500), null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Contact);
    }
}
