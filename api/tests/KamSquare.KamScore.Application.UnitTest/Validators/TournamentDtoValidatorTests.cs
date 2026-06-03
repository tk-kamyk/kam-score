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

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(0)]
    public void InvalidBestOfSets_ShouldFailValidation(int bestOfSets)
    {
        var conditions = new GameConditionsDto(bestOfSets, null);
        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, conditions, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.GameConditions!.BestOfSets);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void ValidBestOfSets_ShouldPassValidation(int bestOfSets)
    {
        var conditions = new GameConditionsDto(bestOfSets, null);
        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, conditions, null, null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.GameConditions!.BestOfSets);
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

    [Fact]
    public void MissingType_ShouldFailValidation()
    {
        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, null, null, null, Type: null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void InvalidType_ShouldFailValidation()
    {
        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, null, null, null, Type: "Bogus");

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Theory]
    [InlineData("Public")]
    [InlineData("Private")]
    [InlineData("Template")]
    public void ValidType_ShouldPassValidation(string type)
    {
        var dto = new TournamentDto(null, "Summer Cup", "Volleyball", null, null, null, null, null, Type: type);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }
}
