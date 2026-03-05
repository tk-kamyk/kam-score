using FluentValidation;
using KamSquare.KamScore.Application.DTOs;

namespace KamSquare.KamScore.Application.Validators;

public class GameResultDtoValidator : AbstractValidator<GameResultDto>
{
    public GameResultDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => !(x.Sets is { Count: > 0 } && (x.HomeScore.HasValue || x.AwayScore.HasValue)))
            .WithName("Result")
            .WithMessage("Sets and HomeScore/AwayScore are mutually exclusive. Provide one or the other, not both.");

        RuleFor(x => x)
            .Must(x => (x.Sets is { Count: > 0 }) || (x.HomeScore.HasValue && x.AwayScore.HasValue))
            .WithName("Result")
            .WithMessage("Either Sets (non-empty) or both HomeScore and AwayScore must be provided.");

        When(x => x.HomeScore.HasValue, () =>
        {
            RuleFor(x => x.HomeScore!.Value).GreaterThanOrEqualTo(0);
        });

        When(x => x.AwayScore.HasValue, () =>
        {
            RuleFor(x => x.AwayScore!.Value).GreaterThanOrEqualTo(0);
        });

        When(x => x.Sets is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.Sets)
                .ChildRules(set =>
                {
                    set.RuleFor(s => s.HomePoints).GreaterThanOrEqualTo(0);
                    set.RuleFor(s => s.AwayPoints).GreaterThanOrEqualTo(0);
                });
        });

        When(x => x.Sets is null or { Count: 0 }, () =>
        {
            When(x => x.HomeScore.HasValue && x.AwayScore.HasValue, () =>
            {
                RuleFor(x => x)
                    .Must(x => x.HomeScore != x.AwayScore)
                    .WithName("Result")
                    .WithMessage("A simple result cannot be a tie.");
            });
        });

        When(x => x.Sets is { Count: > 1 }, () =>
        {
            RuleFor(x => x)
                .Must(x =>
                {
                    var homeWins = x.Sets!.Count(s => s.HomePoints > s.AwayPoints);
                    var awayWins = x.Sets!.Count(s => s.AwayPoints > s.HomePoints);
                    return homeWins != awayWins;
                })
                .WithName("Sets")
                .WithMessage("With more than one set, the result cannot be a tie.");

            RuleForEach(x => x.Sets)
                .ChildRules(set =>
                {
                    set.RuleFor(s => s)
                        .Must(s => s.HomePoints != s.AwayPoints)
                        .WithName("Set")
                        .WithMessage("In a multi-set result, each set must have a winner.");
                });
        });
    }
}
