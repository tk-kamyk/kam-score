using FluentValidation;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Application.Validators;

public class CourtDtoValidator : AbstractValidator<CourtDto>
{
    public CourtDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Court name is required.")
            .MaximumLength(100).WithMessage("Court name must not exceed 100 characters.");
    }
}
