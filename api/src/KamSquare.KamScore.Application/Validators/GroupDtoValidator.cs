using FluentValidation;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Application.Validators;

public class GroupDtoValidator : AbstractValidator<GroupDto>
{
    public GroupDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
