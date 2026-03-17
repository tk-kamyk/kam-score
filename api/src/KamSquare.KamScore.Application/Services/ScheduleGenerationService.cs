using FluentValidation;
using FluentValidation.Results;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Application.Services;

public class ScheduleGenerationService
{
    private readonly IGameRepository _gameRepository;
    private readonly ICourtRepository _courtRepository;
    private readonly ITournamentStructureRepository _structureRepository;

    public ScheduleGenerationService(
        IGameRepository gameRepository,
        ICourtRepository courtRepository,
        ITournamentStructureRepository structureRepository)
    {
        _gameRepository = gameRepository;
        _courtRepository = courtRepository;
        _structureRepository = structureRepository;
    }

    public async Task<List<Game>> GenerateAndScheduleAsync(
        Tournament tournament,
        string tournamentId,
        string phaseId,
        TournamentStructure structure)
    {
        var phase = structure.GetPhase(phaseId);
        var courts = (await _courtRepository.GetByTournamentIdAsync(tournamentId)).ToList();

        await ValidateGenerationPrerequisitesAsync(tournament, phase, courts, tournamentId, phaseId);

        var allGames = GenerateGamesForPhase(phase, tournamentId, phaseId);

        var courtIds = courts.OrderBy(c => c.Name).Select(c => c.Id).ToList();
        var groupOrder = phase.Groups.Select(g => g.Id).ToList();
        var startDateTime = tournament.StartTime?.Date.Add(phase.StartTime!.Value.ToTimeSpan())
            ?? DateTime.Today.Add(phase.StartTime!.Value.ToTimeSpan());
        GameScheduler.Schedule(allGames, courtIds, groupOrder, startDateTime, tournament.GameLength!.Value);
        RefereeAssigner.Assign(allGames, tournament.GameLength!.Value);

        var savedGames = (await _gameRepository.CreateBatchAsync(allGames)).ToList();

        // Transition phase status: Scheduled if waiting on previous phase, InProgress otherwise
        if (phase.Status == PhaseStatus.New)
        {
            var previousPhase = structure.GetPreviousPhase(phaseId);
            var hasUnresolvedDependency = previousPhase is not null
                && previousPhase.HasProgressionConfig
                && previousPhase.Status != PhaseStatus.Completed;

            if (hasUnresolvedDependency)
            {
                structure.SchedulePhase(phaseId);
            }
            else
            {
                structure.ActivatePhase(phaseId);
            }

            await _structureRepository.UpdateAsync(structure);
        }

        return savedGames;
    }

    private async Task ValidateGenerationPrerequisitesAsync(
        Tournament tournament, Phase phase, List<Court> courts,
        string tournamentId, string phaseId)
    {
        if (tournament.GameLength is null or <= 0)
            throw new ValidationException(
                [new ValidationFailure("GameLength", "Tournament must have a game length configured.")]);

        if (phase.StartTime is null)
            throw new ValidationException(
                [new ValidationFailure("StartTime", "Phase must have a start time configured.")]);

        if (courts.Count == 0)
            throw new ValidationException(
                [new ValidationFailure("Courts", "At least one court is required.")]);

        if (phase.Groups.Count == 0 || phase.Groups.All(g => g.TeamIds.Count == 0))
            throw new ValidationException(
                [new ValidationFailure("Teams", "Phase groups must have teams assigned.")]);

        if (await _gameRepository.GamesExistForPhaseAsync(tournamentId, phaseId))
            throw new ValidationException(
                [new ValidationFailure("Games", "Games already exist for this phase. Delete them first.")]);
    }

    private static List<Game> GenerateGamesForPhase(Phase phase, string tournamentId, string phaseId)
    {
        var allGames = new List<Game>();
        foreach (var group in phase.Groups)
        {
            if (group.TeamIds.Count <= 1) continue;

            var games = phase.Format switch
            {
                PhaseFormat.RoundRobin => RoundRobinGenerator.Generate(
                    tournamentId, phaseId, group.Id, group.TeamIds),
                PhaseFormat.PlayoffElimination => PlayoffEliminationGenerator.Generate(
                    tournamentId, phaseId, group.Id, group.TeamIds),
                PhaseFormat.PlayoffWithPlacement => PlayoffWithPlacementGenerator.Generate(
                    tournamentId, phaseId, group.Id, group.TeamIds),
                _ => throw new ValidationException(
                    [new ValidationFailure("Format", $"Unsupported phase format: {phase.Format}")])
            };

            allGames.AddRange(games);
        }

        if (allGames.Count == 0)
            throw new ValidationException(
                [new ValidationFailure("Games", "No games could be generated. Check team assignments.")]);

        return allGames;
    }
}
