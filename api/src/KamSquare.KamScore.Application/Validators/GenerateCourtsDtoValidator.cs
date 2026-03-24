using FluentValidation;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Application.Validators;

public class GenerateCourtsDtoValidator : AbstractValidator<GenerateCourtsDto>
{
    public GenerateCourtsDtoValidator()
    {
        RuleFor(x => x.Count)
            .InclusiveBetween(1, 20).WithMessage("Count must be between 1 and 20.");
    }
}
