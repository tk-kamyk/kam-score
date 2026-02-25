using FluentAssertions;
using FluentValidation.TestHelper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Validators;

namespace KamSquare.KamScore.Application.UnitTest.Validators;

public class TournamentDtoValidatorTests
{
    private readonly TournamentDtoValidator _validator = new();

    [Fact]
    public void Valid_Dto_ShouldPassValidation()
    {
        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, null, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_ShouldFailValidation()
    {
        var dto = new TournamentDto(null, "", "Volleyball", null, null, null, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void InvalidDiscipline_ShouldFailValidation()
    {
        var dto = new TournamentDto(null, "Summer Cup", "InvalidSport", null, null, null, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Discipline);
    }

    [Fact]
    public void NegativeGameLength_ShouldFailValidation()
    {
        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, -1, null, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.GameLength);
    }

    [Fact]
    public void EvenWinningSets_ShouldFailValidation()
    {
        var conditions = new GameConditionsDto(2, [25, 25]);
        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, conditions, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.GameConditions!.WinningSets);
    }

    [Fact]
    public void MismatchedPointsPerSetCount_ShouldFailValidation()
    {
        var conditions = new GameConditionsDto(3, [25, 25]);
        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, conditions, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.GameConditions!.PointsPerSet);
    }

    [Fact]
    public void ValidGameConditions_ShouldPassValidation()
    {
        var conditions = new GameConditionsDto(3, [25, 25, 15]);
        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, 60, conditions, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void BeachVolleyball_ShouldBeValidDiscipline()
    {
        var dto = new TournamentDto(null, "Beach Cup", "BeachVolleyball", null, null, null, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
