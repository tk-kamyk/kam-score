using FluentValidation;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Application.Validators;

public class GenerateSeedTeamsDtoValidator : AbstractValidator<GenerateSeedTeamsDto>
{
    public GenerateSeedTeamsDtoValidator()
    {
        RuleFor(x => x.Count)
            .InclusiveBetween(1, 100).WithMessage("Count must be between 1 and 100.");
    }
}
