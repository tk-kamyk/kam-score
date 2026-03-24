using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Api.Helpers;

public static class GameEnrichmentHelper
{
    public static List<GameDto> EnrichGamesWithNames(
        IEnumerable<Game> games, List<Team> teams, List<Court> courts,
        TournamentStructure? structure, IMapper mapper)
    {
        var teamLookup = teams.ToDictionary(t => t.Id, t => t.Name);
        var placeholderLookup = teams.Where(t => t.IsPlaceholder).Select(t => t.Id).ToHashSet();
        var courtLookup = courts.ToDictionary(c => c.Id, c => c.Name);

        var phaseLookup = structure?.Phases.ToDictionary(p => p.Id, p => p.Name) ?? [];
        var groupLookup = structure?.Phases
            .SelectMany(p => p.Groups)
            .ToDictionary(g => g.Id, g => g.Name) ?? [];
        var groupToLevelLookup = structure?.Phases
            .SelectMany(p => p.Groups.Where(g => g.LevelId is not null)
                .Select(g => new { g.Id, g.LevelId }))
            .ToDictionary(x => x.Id, x => x.LevelId!) ?? [];
        var levelLookup = structure?.Phases
            .SelectMany(p => p.Levels)
            .ToDictionary(l => l.Id, l => l.Name) ?? [];

        return games.Select(g =>
        {
            var dto = mapper.Map<GameDto>(g);

            string? levelName = null;
            if (groupToLevelLookup.TryGetValue(g.GroupId, out var levelId))
                levelLookup.TryGetValue(levelId, out levelName);

            return dto with
            {
                HomeTeamName = g.HomeTeamId is not null && teamLookup.TryGetValue(g.HomeTeamId, out var hn) ? hn : null,
                AwayTeamName = g.AwayTeamId is not null && teamLookup.TryGetValue(g.AwayTeamId, out var an) ? an : null,
                RefereeTeamName = g.RefereeTeamId is not null && teamLookup.TryGetValue(g.RefereeTeamId, out var rn) ? rn : g.RefereeTeamPlaceholder,
                CourtName = g.CourtId is not null && courtLookup.TryGetValue(g.CourtId, out var cn) ? cn : null,
                HomeTeamIsPlaceholder = g.HomeTeamId is not null && placeholderLookup.Contains(g.HomeTeamId) ? true : null,
                AwayTeamIsPlaceholder = g.AwayTeamId is not null && placeholderLookup.Contains(g.AwayTeamId) ? true : null,
                PhaseName = phaseLookup.TryGetValue(g.PhaseId, out var pn) ? pn : null,
                GroupName = groupLookup.TryGetValue(g.GroupId, out var gn) ? gn : null,
                LevelName = levelName
            };
        }).ToList();
    }
}
