using FluentValidation;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Application.Validators;

public class AutoAssignShiftGroupDtoValidator : AbstractValidator<AutoAssignShiftGroupDto>
{
    public AutoAssignShiftGroupDtoValidator()
    {
        RuleFor(x => x.VolunteersPerShift)
            .GreaterThanOrEqualTo(1)
            .WithMessage("VolunteersPerShift must be at least 1.");

        RuleFor(x => x.VolunteersPerShift)
            .LessThanOrEqualTo(50)
            .WithMessage("VolunteersPerShift must be at most 50.");

        // Optional COUNT of stations to spread across (1..Count). Distinct from a station INDEX
        // (0..Count-1), which is validated in VolunteerService.ValidateStationIndex.
        RuleFor(x => x.StationCount!.Value)
            .InclusiveBetween(1, StationPalette.Count)
            .WithMessage($"StationCount must be between 1 and {StationPalette.Count}.")
            .When(x => x.StationCount.HasValue);
    }
}
