using FluentValidation;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Application.Validators;

public class PhaseDtoValidator : AbstractValidator<PhaseDto>
{
    public PhaseDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(format => Enum.TryParse<PhaseFormat>(format, ignoreCase: true, out _))
            .WithMessage("Format must be one of: RoundRobin, PlayoffElimination, PlayoffWithPlacement.");

        RuleFor(x => x.NumberOfGroups)
            .GreaterThanOrEqualTo(1)
            .When(x => x.NumberOfGroups.HasValue);

        RuleFor(x => x.NumberOfLevels)
            .GreaterThanOrEqualTo(1)
            .When(x => x.NumberOfLevels.HasValue);

        RuleFor(x => x.GroupWinners)
            .GreaterThanOrEqualTo(1)
            .When(x => x.GroupWinners.HasValue);

        RuleFor(x => x.TotalTeamsProceeding)
            .GreaterThanOrEqualTo(1)
            .When(x => x.TotalTeamsProceeding.HasValue);

        RuleFor(x => x.StartTime)
            .Must(value => TimeOnly.TryParseExact(value, "HH:mm", out _))
            .WithMessage("StartTime must be in HH:mm format (e.g. 09:30).")
            .When(x => x.StartTime is not null);
    }
}
