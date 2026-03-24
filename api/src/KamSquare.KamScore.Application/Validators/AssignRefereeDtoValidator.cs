using FluentValidation;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Application.Validators;

public class AssignRefereeDtoValidator : AbstractValidator<AssignRefereeDto>
{
    public AssignRefereeDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => !(x.TeamId is not null && x.Placeholder is not null))
            .WithName("Referee")
            .WithMessage("TeamId and Placeholder are mutually exclusive. Provide one or the other, not both.");

        RuleFor(x => x)
            .Must(x => x.TeamId is not null || x.Placeholder is not null)
            .WithName("Referee")
            .WithMessage("Either TeamId or Placeholder must be provided.");
    }
}
