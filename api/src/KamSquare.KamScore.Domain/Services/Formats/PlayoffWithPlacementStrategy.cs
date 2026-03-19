using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Services.Formats;

public class PlayoffWithPlacementStrategy : IPhaseFormatStrategy
{
    public bool SupportsRefereeAssignment => false;

    public void ValidateTeams(List<Group> groups)
    {
        // No format-specific team count constraints
    }

    public List<Game> GenerateGames(
        string tournamentId,
        string phaseId,
        string groupId,
        List<string> teamIds)
    {
        if (teamIds.Count <= 1)
            return [];

        if (teamIds.Count == 2)
        {
            return
            [
                Game.Create(tournamentId, phaseId, groupId, round: 1,
                    homeTeamId: teamIds[0], awayTeamId: teamIds[1],
                    label: "Final")
            ];
        }

        var n = teamIds.Count;
        var bracketSize = BracketUtilities.NextPowerOfTwo(n);
        var totalMainRounds = (int)Math.Log2(bracketSize);
        var roundNames = BracketUtilities.GetRoundNames(totalMainRounds);

        var (r1Games, r1Pool) = GenerateFirstRound(
            tournamentId, phaseId, groupId, teamIds, bracketSize, roundNames[0]);

        var eliminationGames = new List<Game>(r1Games);
        var placementGames = new List<Game>();
        var roundNumber = 1;

        var currentLevel = new List<(List<PoolEntry> Entries, string Prefix)>
        {
            (r1Pool, "")
        };

        while (currentLevel.Count > 0)
        {
            var nextLevel = new List<(List<PoolEntry> Entries, string Prefix)>();

            foreach (var (entries, prefix) in currentLevel)
            {
                ProcessLoserSubBracket(tournamentId, phaseId, groupId, entries, prefix,
                    eliminationGames, placementGames, nextLevel, ref roundNumber);

                ProcessWinnerSubBracket(tournamentId, phaseId, groupId, entries, prefix,
                    eliminationGames, placementGames, nextLevel, ref roundNumber);
            }

            currentLevel = nextLevel;
        }

        foreach (var game in placementGames)
        {
            roundNumber++;
            game.Round = roundNumber;
        }

        eliminationGames.AddRange(placementGames);
        return eliminationGames;
    }

    public List<Standing> CalculateStandings(List<Game> games, List<string> teamIds)
    {
        var gamesByRound = games.GroupBy(g => g.Round).ToDictionary(g => g.Key, g => g.ToList());
        var maxRound = gamesByRound.Count > 0 ? gamesByRound.Keys.Max() : 0;

        var placementRounds = new List<int>();
        for (var round = maxRound; round >= 1; round--)
        {
            if (gamesByRound.TryGetValue(round, out var roundGames) && roundGames.Count == 1)
                placementRounds.Add(round);
            else
                break;
        }

        var teamPositions = new Dictionary<string, int>();
        var teamWins = new Dictionary<string, int>();
        var teamLosses = new Dictionary<string, int>();

        foreach (var teamId in teamIds)
        {
            teamWins[teamId] = 0;
            teamLosses[teamId] = 0;
        }

        var completedGames = BracketStandingsHelper.GetCompletedGames(games);

        foreach (var game in completedGames)
        {
            var result = BracketStandingsHelper.ExtractWinnerAndLoser(game);
            if (result is null) continue;
            var (winnerId, loserId) = result.Value;

            if (teamWins.ContainsKey(winnerId)) teamWins[winnerId]++;
            if (teamLosses.ContainsKey(loserId)) teamLosses[loserId]++;
        }

        var nextPosition = 1;
        foreach (var round in placementRounds)
        {
            var game = gamesByRound[round][0];
            var placementResult = game.Status == GameStatus.Completed
                && game.HomeTeamId is not null
                && game.AwayTeamId is not null
                ? BracketStandingsHelper.ExtractWinnerAndLoser(game) : null;

            if (placementResult is not null)
            {
                teamPositions[placementResult.Value.WinnerId] = nextPosition;
                teamPositions[placementResult.Value.LoserId] = nextPosition + 1;
            }
            nextPosition += 2;
        }

        var unrankedPosition = teamIds.Count;

        return teamIds
            .Select(id =>
            {
                var position = teamPositions.GetValueOrDefault(id, unrankedPosition);
                var wins = teamWins.GetValueOrDefault(id, 0);
                var losses = teamLosses.GetValueOrDefault(id, 0);
                return BracketStandingsHelper.CreateBracketStanding(id, position, wins, losses);
            })
            .OrderBy(s => s.Position)
            .ThenByDescending(s => s.Wins)
            .ToList();
    }

    public List<Standing> RankCrossGroup(List<Standing> standings)
    {
        return BracketStandingsHelper.RankByPositionThenWins(standings);
    }

    private static void ProcessLoserSubBracket(
        string tournamentId, string phaseId, string groupId,
        List<PoolEntry> entries, string prefix,
        List<Game> eliminationGames, List<Game> placementGames,
        List<(List<PoolEntry> Entries, string Prefix)> nextLevel,
        ref int roundNumber)
    {
        var loserEntries = entries.Where(e => e.HasLoser).ToList();
        if (loserEntries.Count < 2)
            return;

        var bPrefix = prefix + "B-";
        var bRoundName = GetSubBracketRoundName(loserEntries.Count / 2);
        var bGames = new List<Game>();
        var bPoolEntries = new List<PoolEntry>();

        for (var i = 0; i < loserEntries.Count; i += 2)
        {
            var matchNum = i / 2 + 1;
            var label = $"{bPrefix}{bRoundName}{matchNum}";
            var game = Game.Create(tournamentId, phaseId, groupId,
                round: 0,
                homeTeamPlaceholder: loserEntries[i].LoserRef,
                awayTeamPlaceholder: loserEntries[i + 1].LoserRef,
                label: label);
            bGames.Add(game);
            bPoolEntries.Add(new PoolEntry(label, null));
        }

        if (loserEntries.Count == 2)
        {
            placementGames.AddRange(bGames);
        }
        else
        {
            roundNumber++;
            foreach (var g in bGames) g.Round = roundNumber;
            eliminationGames.AddRange(bGames);
            nextLevel.Add((bPoolEntries, bPrefix));
        }
    }

    private static void ProcessWinnerSubBracket(
        string tournamentId, string phaseId, string groupId,
        List<PoolEntry> entries, string prefix,
        List<Game> eliminationGames, List<Game> placementGames,
        List<(List<PoolEntry> Entries, string Prefix)> nextLevel,
        ref int roundNumber)
    {
        if (entries.Count < 2)
            return;

        var aPrefix = prefix + "A-";
        var aRoundName = GetSubBracketRoundName(entries.Count / 2);
        var aGames = new List<Game>();
        var aPoolEntries = new List<PoolEntry>();

        for (var i = 0; i < entries.Count; i += 2)
        {
            var matchNum = i / 2 + 1;
            var label = $"{aPrefix}{aRoundName}{matchNum}";

            string? homeTeamId = null, awayTeamId = null;
            string? homePlaceholder = null, awayPlaceholder = null;

            if (entries[i].IsBye)
                homeTeamId = entries[i].WinnerRef;
            else
                homePlaceholder = entries[i].WinnerRef;

            if (entries[i + 1].IsBye)
                awayTeamId = entries[i + 1].WinnerRef;
            else
                awayPlaceholder = entries[i + 1].WinnerRef;

            var game = Game.Create(tournamentId, phaseId, groupId,
                round: 0,
                homeTeamId: homeTeamId, awayTeamId: awayTeamId,
                homeTeamPlaceholder: homePlaceholder, awayTeamPlaceholder: awayPlaceholder,
                label: label);
            aGames.Add(game);
            aPoolEntries.Add(new PoolEntry(label, null));
        }

        if (entries.Count == 2)
        {
            placementGames.AddRange(aGames);
        }
        else
        {
            roundNumber++;
            foreach (var g in aGames) g.Round = roundNumber;
            eliminationGames.AddRange(aGames);
            nextLevel.Add((aPoolEntries, aPrefix));
        }
    }

    private static (List<Game> Games, List<PoolEntry> Pool) GenerateFirstRound(
        string tournamentId,
        string phaseId,
        string groupId,
        List<string> teamIds,
        int bracketSize,
        string roundName)
    {
        var n = teamIds.Count;
        var bracketOrder = BracketUtilities.BuildBracketOrder(bracketSize);
        var games = new List<Game>();
        var pool = new List<PoolEntry>();
        var gameIndex = 0;

        var firstRoundMatchCount = bracketSize / 2;

        for (var match = 0; match < firstRoundMatchCount; match++)
        {
            var seed1Pos = bracketOrder[match * 2];
            var seed2Pos = bracketOrder[match * 2 + 1];

            var team1 = seed1Pos < n ? teamIds[seed1Pos] : null;
            var team2 = seed2Pos < n ? teamIds[seed2Pos] : null;

            if (team1 is null || team2 is null)
            {
                var advancingTeamId = team1 ?? team2!;
                pool.Add(new PoolEntry($"BYE-{match}", advancingTeamId));
                continue;
            }

            gameIndex++;
            var label = BracketUtilities.GetMatchLabel(roundName, gameIndex);
            var game = Game.Create(tournamentId, phaseId, groupId, round: 1,
                homeTeamId: team1, awayTeamId: team2, label: label);
            games.Add(game);
            pool.Add(new PoolEntry(label, null));
        }

        return (games, pool);
    }

    private static string GetSubBracketRoundName(int gameCount)
    {
        return gameCount switch
        {
            1 => "F",
            2 => "SF",
            4 => "QF",
            _ => $"R{gameCount}"
        };
    }

    private record PoolEntry(string Label, string? AutoAdvanceTeamId)
    {
        public bool IsBye => AutoAdvanceTeamId is not null;
        public string WinnerRef => IsBye ? AutoAdvanceTeamId! : $"Winner {Label}";
        public bool HasLoser => !IsBye;
        public string LoserRef => $"Loser {Label}";
    }
}
