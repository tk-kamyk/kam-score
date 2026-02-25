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

        RuleFor(x => x.GameConditions!.WinningSets)
            .GreaterThan(0).WithMessage("Winning sets must be greater than 0.")
            .Must(v => v!.Value % 2 == 1).WithMessage("Winning sets must be an odd number.")
            .When(x => x.GameConditions?.WinningSets.HasValue == true);

        RuleFor(x => x.GameConditions!.PointsPerSet)
            .Must((dto, pointsPerSet) =>
                pointsPerSet!.Count == dto.GameConditions!.WinningSets)
            .WithMessage("Points per set count must match winning sets.")
            .When(x => x.GameConditions?.PointsPerSet != null && x.GameConditions?.WinningSets.HasValue == true);
    }

    private static bool BeValidDiscipline(string discipline)
    {
        return Enum.TryParse<Discipline>(discipline, ignoreCase: true, out _);
    }
}
