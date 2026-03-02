using FluentValidation.TestHelper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Validators;

namespace KamSquare.KamScore.Application.UnitTest.Validators;

public class GameResultDtoValidatorTests
{
    private readonly GameResultDtoValidator _validator = new();

    [Fact]
    public void Valid_DetailedResult_TwoSets_ClearWinner_ShouldPass()
    {
        var dto = new GameResultDto(
            Sets: [new SetResultDto(25, 20), new SetResultDto(25, 18)],
            HomeScore: null,
            AwayScore: null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_DetailedResult_ThreeSets_Winner_ShouldPass()
    {
        var dto = new GameResultDto(
            Sets: [new SetResultDto(25, 20), new SetResultDto(23, 25), new SetResultDto(15, 10)],
            HomeScore: null,
            AwayScore: null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_DetailedResult_OneSet_Tie_ShouldPass()
    {
        var dto = new GameResultDto(
            Sets: [new SetResultDto(25, 25)],
            HomeScore: null,
            AwayScore: null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_SimpleResult_NoTie_ShouldPass()
    {
        var dto = new GameResultDto(Sets: null, HomeScore: 2, AwayScore: 1);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalid_SimpleResult_Tie_ShouldFail()
    {
        var dto = new GameResultDto(Sets: null, HomeScore: 1, AwayScore: 1);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor("Result")
            .WithErrorMessage("A simple result cannot be a tie.");
    }

    [Fact]
    public void Invalid_DetailedResult_TwoSets_Tie_ShouldFail()
    {
        var dto = new GameResultDto(
            Sets: [new SetResultDto(25, 20), new SetResultDto(20, 25)],
            HomeScore: null,
            AwayScore: null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor("Sets")
            .WithErrorMessage("With more than one set, the result cannot be a tie.");
    }

    [Fact]
    public void Invalid_DetailedResult_FourSets_EvenTie_ShouldFail()
    {
        var dto = new GameResultDto(
            Sets: [new SetResultDto(25, 20), new SetResultDto(20, 25), new SetResultDto(25, 20), new SetResultDto(20, 25)],
            HomeScore: null,
            AwayScore: null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor("Sets")
            .WithErrorMessage("With more than one set, the result cannot be a tie.");
    }

    [Fact]
    public void Invalid_DetailedResult_MultiSet_IndividualSetDraw_ShouldFail()
    {
        var dto = new GameResultDto(
            Sets: [new SetResultDto(25, 13), new SetResultDto(13, 13)],
            HomeScore: null,
            AwayScore: null);

        var result = _validator.TestValidate(dto);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "In a multi-set result, each set must have a winner.");
    }

    [Fact]
    public void Invalid_MissingBothSetsAndScore_ShouldFail()
    {
        var dto = new GameResultDto(Sets: null, HomeScore: null, AwayScore: null);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor("Result");
    }
}
