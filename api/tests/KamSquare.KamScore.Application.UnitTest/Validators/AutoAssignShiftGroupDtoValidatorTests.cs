using FluentValidation.TestHelper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Validators;

namespace KamSquare.KamScore.Application.UnitTest.Validators;

public class AutoAssignShiftGroupDtoValidatorTests
{
    private readonly AutoAssignShiftGroupDtoValidator _validator = new();

    [Fact]
    public void Valid_VolunteersPerShift_ShouldPass()
    {
        var dto = new AutoAssignShiftGroupDto(3);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void NonPositive_VolunteersPerShift_ShouldFail(int value)
    {
        var dto = new AutoAssignShiftGroupDto(value);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.VolunteersPerShift);
    }

    [Fact]
    public void TooLarge_VolunteersPerShift_ShouldFail()
    {
        var dto = new AutoAssignShiftGroupDto(51);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.VolunteersPerShift);
    }

    [Fact]
    public void BoundaryUpper_VolunteersPerShift_ShouldPass()
    {
        var dto = new AutoAssignShiftGroupDto(50);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void BoundaryLower_VolunteersPerShift_ShouldPass()
    {
        var dto = new AutoAssignShiftGroupDto(1);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NullStationCount_ShouldPass()
    {
        var dto = new AutoAssignShiftGroupDto(2, StationCount: null);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void StationCount_WithinPalette_ShouldPass(int stationCount)
    {
        var dto = new AutoAssignShiftGroupDto(2, stationCount);

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void StationCount_OutsidePalette_ShouldFail(int stationCount)
    {
        var dto = new AutoAssignShiftGroupDto(2, stationCount);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.StationCount!.Value);
    }
}
