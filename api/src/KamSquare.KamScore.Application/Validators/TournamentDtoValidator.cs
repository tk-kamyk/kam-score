using FluentValidation;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Application.Validators;

public class TournamentDtoValidator : AbstractValidator<TournamentDto>
{
    public TournamentDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tournament name is required.")
            .MaximumLength(200).WithMessage("Tournament name must not exceed 200 characters.");

        RuleFor(x => x.Discipline)
            .NotEmpty().WithMessage("Discipline is required.")
            .Must(BeValidDiscipline).WithMessage("Discipline must be 'Volleyball' or 'BeachVolleyball'.");

        RuleFor(x => x.GameLength)
            .GreaterThan(0).WithMessage("Game length must be greater than 0.")
            .When(x => x.GameLength.HasValue);

        RuleFor(x => x.GameConditions!.BestOfSets)
            .Must(v => v is 1 or 3 or 5).WithMessage("Best of sets must be 1, 3, or 5.")
            .When(x => x.GameConditions?.BestOfSets.HasValue == true);

        RuleFor(x => x.GameConditions!.PointsPerSet)
            .Must((dto, pointsPerSet) =>
                pointsPerSet!.Count == dto.GameConditions!.BestOfSets)
            .WithMessage("Points per set count must match best of sets.")
            .When(x => x.GameConditions?.PointsPerSet != null && x.GameConditions?.BestOfSets.HasValue == true);

        RuleFor(x => x.SourceTournamentId)
            .NotEmpty().WithMessage("Source tournament ID must not be empty when provided.")
            .When(x => x.SourceTournamentId is not null);
    }

    private static bool BeValidDiscipline(string discipline)
    {
        return Enum.TryParse<Discipline>(discipline, ignoreCase: true, out _);
    }
}
