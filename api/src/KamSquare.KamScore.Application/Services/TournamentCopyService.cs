using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.Exceptions;
using KamSquare.KamScore.Domain.Services;

namespace KamSquare.KamScore.Application.Services;

public class TournamentCopyService
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ITournamentStructureRepository _structureRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICourtRepository _courtRepository;
    private readonly IGameRepository _gameRepository;

    public TournamentCopyService(
        ITournamentRepository tournamentRepository,
        ITournamentStructureRepository structureRepository,
        ITeamRepository teamRepository,
        ICourtRepository courtRepository,
        IGameRepository gameRepository)
    {
        _tournamentRepository = tournamentRepository;
        _structureRepository = structureRepository;
        _teamRepository = teamRepository;
        _courtRepository = courtRepository;
        _gameRepository = gameRepository;
    }

    public async Task<Tournament> CopyAsync(string sourceTournamentId, string name, string ownerId)
    {
        var source = await _tournamentRepository.GetByIdAsync(sourceTournamentId)
            ?? throw new NotFoundException(nameof(Tournament), sourceTournamentId);

        var sourceStructure = await _structureRepository.GetByTournamentIdAsync(sourceTournamentId)
            ?? throw new NotFoundException(nameof(TournamentStructure), sourceTournamentId);

        var sourceTeamsTask = _teamRepository.GetByTournamentIdAsync(sourceTournamentId);
        var sourceCourtsTask = _courtRepository.GetByTournamentIdAsync(sourceTournamentId);
        await Task.WhenAll(sourceTeamsTask, sourceCourtsTask);

        var sourceTeams = (await sourceTeamsTask).ToList();
        var sourceCourts = (await sourceCourtsTask).ToList();

        // Create new tournament with copied settings
        var tournament = Tournament.Create(name, source.Discipline, ownerId);
        tournament.Update(name, source.Discipline, source.StartTime, source.GameLength, source.GameConditions);
        var created = await _tournamentRepository.CreateAsync(tournament);
        var newTournamentId = created.Id;

        try
        {
            // Create courts with same names
            var newCourts = sourceCourts
                .Select(c => Court.Create(c.Name, newTournamentId))
                .ToList();
            if (newCourts.Count > 0)
                newCourts = (await _courtRepository.CreateBatchAsync(newCourts)).ToList();

            // Create seed teams matching real (non-placeholder) team count
            var realTeamCount = sourceTeams.Count(t => !t.IsPlaceholder);
            var seedTeams = Team.GenerateSeedTeams(realTeamCount, 1, newTournamentId);
            if (seedTeams.Count > 0)
                seedTeams = (await _teamRepository.CreateBatchAsync(seedTeams)).ToList();

            // Create structure and copy phases
            var newStructure = TournamentStructure.Create(newTournamentId);
            var courtIds = newCourts.OrderBy(c => c.Name).Select(c => c.Id).ToList();

            foreach (var sourcePhase in sourceStructure.Phases.OrderBy(p => p.Order))
            {
                await CopyPhaseAsync(sourcePhase, newStructure, created, seedTeams, courtIds);
            }

            await _structureRepository.CreateAsync(newStructure);

            return created;
        }
        catch
        {
            await CleanupAsync(newTournamentId, ownerId);
            throw;
        }
    }

    private async Task CopyPhaseAsync(Phase sourcePhase, TournamentStructure newStructure,
        Tournament tournament, List<Team> seedTeams, List<string> courtIds)
    {
        var newTournamentId = tournament.Id;
        var numberOfLevels = sourcePhase.Levels.Count > 0 ? sourcePhase.Levels.Count : (int?)null;
        var groupsPerLevel = sourcePhase.Levels.Count > 0
            ? sourcePhase.Groups.Count / sourcePhase.Levels.Count
            : sourcePhase.Groups.Count;

        var newPhase = newStructure.AddPhase(
            sourcePhase.Name,
            sourcePhase.Format,
            groupsPerLevel,
            sourcePhase.GroupWinners,
            sourcePhase.TotalTeamsProceeding,
            sourcePhase.StartTime,
            numberOfLevels);

        CopyLevelAndGroupNames(sourcePhase, newPhase);

        // Assign teams to groups
        if (newPhase.Order == 1)
        {
            newStructure.AutoAssignTeams(newPhase.Id, seedTeams);
        }
        else
        {
            var previousPhase = newStructure.GetPreviousPhase(newPhase.Id);
            if (previousPhase is not null)
            {
                var placeholders = PlaceholderTeamGenerator.Generate(previousPhase, newTournamentId);
                if (placeholders is not null && placeholders.Count > 0)
                {
                    placeholders = (await _teamRepository.CreateBatchAsync(placeholders)).ToList();
                    var orderedIds = placeholders.OrderBy(t => t.Seed).Select(t => t.Id).ToList();
                    newStructure.AutoAssignTeams(newPhase.Id, orderedIds, previousPhase.Levels.Count);
                }
            }
        }

        // Generate games if there are teams to play and at least one court exists
        if (!CanGenerateGames(newPhase, courtIds))
            return;

        var games = newPhase.GenerateGames(newTournamentId);
        if (games.Count == 0)
            return;

        var groupOrder = newPhase.Groups.Select(g => g.Id).ToList();

        // Schedule and assign referees only when scheduling prerequisites are met
        if (CanScheduleGames(tournament, newPhase, courtIds))
        {
            var startDateTime = (tournament.StartTime?.Date ?? DateTime.Today)
                .Add(newPhase.StartTime!.Value.ToTimeSpan());
            GameScheduler.Schedule(games, courtIds, groupOrder, startDateTime, tournament.GameLength!.Value);

            if (newPhase.SupportsRefereeAssignment)
                RefereeAssigner.Assign(games, tournament.GameLength.Value);
        }
        else
        {
            CourtAssigner.AssignByGroup(games, courtIds, groupOrder);
        }

        await _gameRepository.CreateBatchAsync(games);

        // Set phase status
        if (newPhase.Order == 1)
            newStructure.ActivatePhase(newPhase.Id);
        else
            newStructure.SchedulePhase(newPhase.Id);
    }

    private async Task CleanupAsync(string tournamentId, string ownerId)
    {
        await Task.WhenAll(
            _gameRepository.DeleteByTournamentIdAsync(tournamentId),
            _teamRepository.DeleteByTournamentIdAsync(tournamentId),
            _courtRepository.DeleteByTournamentIdAsync(tournamentId));
        await _structureRepository.DeleteByTournamentIdAsync(tournamentId);
        await _tournamentRepository.DeleteAsync(tournamentId, ownerId);
    }

    private static bool CanGenerateGames(Phase phase, List<string> courtIds)
    {
        if (phase.Groups.Count == 0 || phase.Groups.All(g => g.TeamIds.Count == 0))
            return false;
        if (courtIds.Count == 0)
            return false;
        return true;
    }

    private static bool CanScheduleGames(Tournament tournament, Phase phase, List<string> courtIds)
    {
        if (tournament.GameLength is null or <= 0)
            return false;
        if (phase.StartTime is null)
            return false;
        if (courtIds.Count == 0)
            return false;
        return true;
    }

    private static void CopyLevelAndGroupNames(Phase source, Phase target)
    {
        var sourceLevels = source.Levels.OrderBy(l => l.Order).ToList();
        var targetLevels = target.Levels.OrderBy(l => l.Order).ToList();
        var levelPairs = Math.Min(sourceLevels.Count, targetLevels.Count);
        for (var i = 0; i < levelPairs; i++)
        {
            targetLevels[i].Update(sourceLevels[i].Name);
        }

        if (target.Levels.Count == 0)
        {
            var pairs = Math.Min(source.Groups.Count, target.Groups.Count);
            for (var i = 0; i < pairs; i++)
            {
                target.Groups[i].Update(source.Groups[i].Name);
            }
            return;
        }

        for (var i = 0; i < levelPairs; i++)
        {
            var sourceGroups = source.Groups.Where(g => g.LevelId == sourceLevels[i].Id).ToList();
            var targetGroups = target.Groups.Where(g => g.LevelId == targetLevels[i].Id).ToList();
            var pairs = Math.Min(sourceGroups.Count, targetGroups.Count);
            for (var j = 0; j < pairs; j++)
            {
                targetGroups[j].Update(sourceGroups[j].Name);
            }
        }
    }
}
