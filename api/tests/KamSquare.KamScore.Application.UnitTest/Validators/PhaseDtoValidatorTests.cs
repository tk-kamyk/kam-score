using FluentValidation.TestHelper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Validators;

namespace KamSquare.KamScore.Application.UnitTest.Validators;

public class PhaseDtoValidatorTests
{
    private readonly PhaseDtoValidator _validator = new();

    [Fact]
    public void ValidPhaseDto_ShouldPassValidation()
    {
        var dto = new PhaseDto(null, "Group Stage", "RoundRobin", NumberOfGroups: 2);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_ShouldFailValidation()
    {
        var dto = new PhaseDto(null, "", "RoundRobin");

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameTooLong_ShouldFailValidation()
    {
        var dto = new PhaseDto(null, new string('x', 201), "RoundRobin");

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void EmptyFormat_ShouldFailValidation()
    {
        var dto = new PhaseDto(null, "Groups", "");

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Format);
    }

    [Fact]
    public void InvalidFormat_ShouldFailValidation()
    {
        var dto = new PhaseDto(null, "Groups", "InvalidFormat");

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Format);
    }

    [Theory]
    [InlineData("RoundRobin")]
    [InlineData("PlayoffElimination")]
    [InlineData("PlayoffWithPlacement")]
    [InlineData("roundrobin")]
    public void ValidFormats_ShouldPassValidation(string format)
    {
        var dto = new PhaseDto(null, "Groups", format);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Format);
    }

    [Fact]
    public void NumberOfGroups_Zero_ShouldFailValidation()
    {
        var dto = new PhaseDto(null, "Groups", "RoundRobin", NumberOfGroups: 0);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.NumberOfGroups);
    }

    [Fact]
    public void NumberOfGroups_Null_ShouldPassValidation()
    {
        var dto = new PhaseDto(null, "Groups", "RoundRobin");

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.NumberOfGroups);
    }

    [Fact]
    public void GroupWinners_Zero_ShouldFailValidation()
    {
        var dto = new PhaseDto(null, "Groups", "RoundRobin", GroupWinners: 0);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.GroupWinners);
    }

    [Fact]
    public void GroupWinners_Null_ShouldPassValidation()
    {
        var dto = new PhaseDto(null, "Groups", "RoundRobin");

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.GroupWinners);
    }

    [Fact]
    public void GroupWinners_Positive_ShouldPassValidation()
    {
        var dto = new PhaseDto(null, "Groups", "RoundRobin", GroupWinners: 2);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.GroupWinners);
    }

    [Fact]
    public void TotalTeamsProceeding_Zero_ShouldFailValidation()
    {
        var dto = new PhaseDto(null, "Groups", "RoundRobin", TotalTeamsProceeding: 0);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.TotalTeamsProceeding);
    }

    [Fact]
    public void TotalTeamsProceeding_Null_ShouldPassValidation()
    {
        var dto = new PhaseDto(null, "Groups", "RoundRobin");

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.TotalTeamsProceeding);
    }

    [Fact]
    public void TotalTeamsProceeding_Positive_ShouldPassValidation()
    {
        var dto = new PhaseDto(null, "Groups", "RoundRobin", TotalTeamsProceeding: 6);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.TotalTeamsProceeding);
    }
}
