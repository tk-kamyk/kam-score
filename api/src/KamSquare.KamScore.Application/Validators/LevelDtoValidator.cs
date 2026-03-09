using FluentValidation;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Application.Validators;

public class LevelDtoValidator : AbstractValidator<LevelDto>
{
    public LevelDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
