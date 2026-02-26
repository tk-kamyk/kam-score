using FluentValidation;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Application.Validators;

public class TeamDtoValidator : AbstractValidator<TeamDto>
{
    public TeamDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Team name is required.")
            .MaximumLength(100).WithMessage("Team name must not exceed 100 characters.");

        RuleFor(x => x.Level)
            .InclusiveBetween(0, 100).WithMessage("Team level must be between 0 and 100.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[\d\s\-()]{7,20}$").WithMessage("Phone must be a valid phone number.")
            .When(x => !string.IsNullOrEmpty(x.Phone));
    }
}
