using FluentValidation;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Application.Validators;

public class VolunteerDtoValidator : AbstractValidator<VolunteerDto>
{
    public VolunteerDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Volunteer name is required.")
            .MaximumLength(200).WithMessage("Volunteer name must not exceed 200 characters.");

        RuleFor(x => x.Contact)
            .MaximumLength(500).WithMessage("Contact must not exceed 500 characters.")
            .When(x => x.Contact is not null);
    }
}
