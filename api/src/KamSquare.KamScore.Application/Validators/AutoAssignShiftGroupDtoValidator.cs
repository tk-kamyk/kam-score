using FluentValidation;
using KamSquare.KamScore.Application.DTOs;

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
    }
}
