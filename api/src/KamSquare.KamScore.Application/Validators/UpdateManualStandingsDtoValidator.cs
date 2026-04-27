using FluentValidation;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Application.Validators;

public class UpdateManualStandingsDtoValidator : AbstractValidator<UpdateManualStandingsDto>
{
    public UpdateManualStandingsDtoValidator()
    {
        RuleFor(x => x.PhaseId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.OrderedTeamIds)
            .NotNull()
            .Must(ids => ids.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("All team IDs must be non-empty.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Ordered team IDs cannot contain duplicates.");
    }
}
