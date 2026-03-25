using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;
using KamSquare.KamScore.Migration.Models;

namespace KamSquare.KamScore.Migration;

public class DataTransformer
{
    private readonly string _ownerId;

    public DataTransformer(string ownerId)
    {
        _ownerId = ownerId;
    }

    public record TransformedTournament(
        Tournament Tournament,
        List<Team> Teams,
        List<Court> Courts,
        TournamentStructure Structure,
        List<Game> Games);

    public TransformedTournament Transform(
        MysqlTournament mysqlTournament,
        List<MysqlTeam> mysqlTeams,
        List<MysqlPhase> mysqlPhases,
        List<MysqlGroup> mysqlGroups,
        List<MysqlGame> mysqlGames,
        List<MysqlRound> mysqlRounds,
        List<MysqlStanding> mysqlStandings,
        List<MysqlPlayoff> mysqlPlayoffs,
        List<MysqlCup> mysqlCups,
        List<MysqlPlayoffGame> mysqlPlayoffGames,
        List<MysqlGameSet> mysqlGameSets)
    {
        // Create tournament
        var tournament = Tournament.Create(mysqlTournament.Name, Discipline.Volleyball, _ownerId);
        var tournamentId = tournament.Id;

        // Create teams with ID mapping
        var teamIdMap = new Dictionary<long, string>();
        var teams = new List<Team>();
        foreach (var mt in mysqlTeams)
        {
            var team = Team.Create(mt.Name, mt.Level * 10, tournamentId, mt.Email, mt.Phone);
            teamIdMap[mt.Id] = team.Id;
            teams.Add(team);
        }

        // Create courts with field name mapping
        var courtFieldMap = new Dictionary<string, string>();
        var courts = new List<Court>();
        for (var i = 1; i <= mysqlTournament.FieldCount; i++)
        {
            var court = Court.Create($"Field {i}", tournamentId);
            courtFieldMap[$"Field {i}"] = court.Id;
            courts.Add(court);
        }

        // Build round lookup for time mapping
        var tournamentDate = new DateTime(mysqlTournament.Year, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var roundMap = mysqlRounds.ToDictionary(r => r.Id);

        // Create tournament structure
        var structure = TournamentStructure.Create(tournamentId);

        var allGames = new List<Game>();

        // --- Phase 1: Round-Robin with 2 Levels ---
        var phaseOne = mysqlPhases.FirstOrDefault(p => p.PhaseType == "PHASE_ONE");
        if (phaseOne is not null)
        {
            var phaseOneGroups = mysqlGroups.Where(g => g.PhaseId == phaseOne.Id).OrderBy(g => g.Name).ToList();
            var groupIdMap = new Dictionary<long, string>();

            var phase = CreatePhaseWithLevels(structure, "Phase One", PhaseFormat.RoundRobin,
                1, phaseOneGroups, groupIdMap, CreatePhaseOneLevels);

            // Assign teams to groups from standings
            AssignTeamsToGroups(phase, mysqlStandings, phaseOneGroups, groupIdMap, teamIdMap);

            // Create games
            var phaseOneGames = mysqlGames.Where(g => phaseOneGroups.Any(pg => pg.Id == g.GroupId)).ToList();
            allGames.AddRange(TransformRoundRobinGames(phaseOneGames, tournamentId, phase.Id,
                groupIdMap, teamIdMap, courtFieldMap, roundMap, tournamentDate));

            phase.Status = PhaseStatus.Completed;
        }

        // --- Phase 2: Round-Robin with 4 Levels ---
        var phaseTwo = mysqlPhases.FirstOrDefault(p => p.PhaseType == "PHASE_TWO");
        if (phaseTwo is not null)
        {
            var phaseTwoGroups = mysqlGroups.Where(g => g.PhaseId == phaseTwo.Id).OrderBy(g => g.Name).ToList();
            var groupIdMap = new Dictionary<long, string>();

            var phase = CreatePhaseWithLevels(structure, "Phase Two", PhaseFormat.RoundRobin,
                2, phaseTwoGroups, groupIdMap, CreatePhaseTwoLevels);

            // Assign teams to groups from standings
            AssignTeamsToGroups(phase, mysqlStandings, phaseTwoGroups, groupIdMap, teamIdMap);

            // Create games
            var phaseTwoGames = mysqlGames.Where(g => phaseTwoGroups.Any(pg => pg.Id == g.GroupId)).ToList();
            allGames.AddRange(TransformRoundRobinGames(phaseTwoGames, tournamentId, phase.Id,
                groupIdMap, teamIdMap, courtFieldMap, roundMap, tournamentDate));

            phase.Status = PhaseStatus.Completed;
        }

        // --- Phase 3: Playoffs ---
        var playoff = mysqlPlayoffs.FirstOrDefault(p => p.TournamentId == mysqlTournament.Id);
        if (playoff is not null)
        {
            var cups = mysqlCups.Where(c => c.PlayoffId == playoff.Id).OrderBy(c => c.Name).ToList();
            if (cups.Count > 0)
            {
                var hasSemiFinals = cups.Any(c => c.FirstSemiFinalId is not null);
                var playoffGames = TransformPlayoffPhase(
                    structure, tournamentId, cups, hasSemiFinals,
                    mysqlPlayoffGames, mysqlGameSets,
                    teamIdMap, courtFieldMap, roundMap, tournamentDate);
                allGames.AddRange(playoffGames);
            }
        }

        structure.LastModified = DateTime.UtcNow;

        return new TransformedTournament(tournament, teams, courts, structure, allGames);
    }

    private static Phase CreatePhaseWithLevels(
        TournamentStructure structure,
        string phaseName,
        PhaseFormat format,
        int order,
        List<MysqlGroup> mysqlGroups,
        Dictionary<long, string> groupIdMap,
        Func<Phase, List<MysqlGroup>, Dictionary<long, string>, bool> levelCreator)
    {
        // Create phase with no groups initially (we'll add them manually)
        var phase = new Phase
        {
            Id = Guid.NewGuid().ToString(),
            Name = phaseName,
            Format = format,
            Order = order
        };
        structure.Phases.Add(phase);

        // Create levels and groups
        levelCreator(phase, mysqlGroups, groupIdMap);

        return phase;
    }

    private static bool CreatePhaseOneLevels(Phase phase, List<MysqlGroup> mysqlGroups, Dictionary<long, string> groupIdMap)
    {
        // Phase One: 2 levels - Top (A-H) and Bottom (I-P)
        var topLevel = Level.Create("Top", 1);
        var bottomLevel = Level.Create("Bottom", 2);
        phase.Levels.Add(topLevel);
        phase.Levels.Add(bottomLevel);

        foreach (var mg in mysqlGroups)
        {
            var groupLetter = mg.Name.Replace("Group ", "");
            var isTop = groupLetter.Length == 1 && groupLetter[0] >= 'A' && groupLetter[0] <= 'H';
            var levelId = isTop ? topLevel.Id : bottomLevel.Id;

            var group = Group.Create(groupLetter, levelId);
            groupIdMap[mg.Id] = group.Id;
            phase.Groups.Add(group);
        }

        return true;
    }

    private static bool CreatePhaseTwoLevels(Phase phase, List<MysqlGroup> mysqlGroups, Dictionary<long, string> groupIdMap)
    {
        // Phase Two: 4 levels - Green, Blue, Red, Purple
        var colorOrder = new[] { "Green", "Blue", "Red", "Purple" };
        var levels = new Dictionary<string, Level>();

        for (var i = 0; i < colorOrder.Length; i++)
        {
            var level = Level.Create(colorOrder[i], i + 1);
            phase.Levels.Add(level);
            levels[colorOrder[i]] = level;
        }

        foreach (var mg in mysqlGroups)
        {
            // Group names are like "Group Green 1", "Group Blue 2"
            var color = colorOrder.FirstOrDefault(c => mg.Name.Contains(c, StringComparison.OrdinalIgnoreCase));
            if (color is null) continue;

            var levelId = levels[color].Id;
            var groupName = mg.Name.Replace("Group ", "");
            var group = Group.Create(groupName, levelId);
            groupIdMap[mg.Id] = group.Id;
            phase.Groups.Add(group);
        }

        return true;
    }

    private static void AssignTeamsToGroups(
        Phase phase,
        List<MysqlStanding> standings,
        List<MysqlGroup> mysqlGroups,
        Dictionary<long, string> groupIdMap,
        Dictionary<long, string> teamIdMap)
    {
        foreach (var mg in mysqlGroups)
        {
            if (!groupIdMap.TryGetValue(mg.Id, out var newGroupId)) continue;
            var group = phase.Groups.FirstOrDefault(g => g.Id == newGroupId);
            if (group is null) continue;

            var groupStandings = standings
                .Where(s => s.GroupId == mg.Id)
                .OrderBy(s => s.Position)
                .ToList();

            foreach (var standing in groupStandings)
            {
                if (teamIdMap.TryGetValue(standing.TeamId, out var newTeamId))
                {
                    group.AddTeam(newTeamId);
                }
            }
        }
    }

    private static List<Game> TransformRoundRobinGames(
        List<MysqlGame> mysqlGames,
        string tournamentId,
        string phaseId,
        Dictionary<long, string> groupIdMap,
        Dictionary<long, string> teamIdMap,
        Dictionary<string, string> courtFieldMap,
        Dictionary<long, MysqlRound> roundMap,
        DateTime tournamentDate)
    {
        var games = new List<Game>();
        // Group rounds by their base number to determine round ordering within each group
        var roundsByGroup = mysqlGames
            .GroupBy(g => g.GroupId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RoundId).Select(x => x.RoundId).Distinct().ToList());

        foreach (var mg in mysqlGames)
        {
            if (!groupIdMap.TryGetValue(mg.GroupId, out var newGroupId)) continue;

            var homeTeamId = mg.HomeTeamId.HasValue && teamIdMap.TryGetValue(mg.HomeTeamId.Value, out var hId) ? hId : null;
            var awayTeamId = mg.AwayTeamId.HasValue && teamIdMap.TryGetValue(mg.AwayTeamId.Value, out var aId) ? aId : null;
            var refTeamId = mg.RefTeamId.HasValue && teamIdMap.TryGetValue(mg.RefTeamId.Value, out var rId) ? rId : null;

            // Determine round number within the group
            var roundNumber = 1;
            if (roundsByGroup.TryGetValue(mg.GroupId, out var groupRounds))
            {
                roundNumber = groupRounds.IndexOf(mg.RoundId) + 1;
            }

            var game = Game.Create(tournamentId, phaseId, newGroupId, roundNumber,
                homeTeamId, awayTeamId);

            if (refTeamId is not null)
            {
                game.RefereeTeamId = refTeamId;
            }

            // Assign court
            if (mg.Field is not null && courtFieldMap.TryGetValue(mg.Field, out var courtId))
            {
                var startTime = GetStartTime(mg.RoundId, roundMap, tournamentDate);
                if (startTime.HasValue)
                    game.AssignSchedule(courtId, startTime.Value);
                else
                    game.CourtId = courtId;
            }

            // Record result
            if (mg.Completed && mg.HomeScore.HasValue && mg.AwayScore.HasValue)
            {
                game.RecordSimpleResult(mg.HomeScore.Value, mg.AwayScore.Value);
            }

            games.Add(game);
        }

        return games;
    }

    private List<Game> TransformPlayoffPhase(
        TournamentStructure structure,
        string tournamentId,
        List<MysqlCup> cups,
        bool hasSemiFinals,
        List<MysqlPlayoffGame> mysqlPlayoffGames,
        List<MysqlGameSet> mysqlGameSets,
        Dictionary<long, string> teamIdMap,
        Dictionary<string, string> courtFieldMap,
        Dictionary<long, MysqlRound> roundMap,
        DateTime tournamentDate)
    {
        var games = new List<Game>();
        var colorOrder = new[] { "Green", "Blue", "Red", "Purple" };
        var gameSetsByPlayoffGame = mysqlGameSets
            .GroupBy(gs => gs.PlayoffGameId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Id).ToList());

        var playoffGameMap = mysqlPlayoffGames.ToDictionary(pg => pg.Id);

        if (hasSemiFinals)
        {
            // PlayoffWithPlacement: 4 levels (Green, Blue, Red, Purple), 1 group per level, 4 teams
            var phase = new Phase
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Playoffs",
                Format = PhaseFormat.PlayoffWithPlacement,
                Order = 3,
                Status = PhaseStatus.Completed
            };
            structure.Phases.Add(phase);

            for (var i = 0; i < colorOrder.Length; i++)
            {
                var level = Level.Create(colorOrder[i], i + 1);
                phase.Levels.Add(level);

                var cup = cups.FirstOrDefault(c => c.Name == colorOrder[i]);
                if (cup is null) continue;

                var group = Group.Create(colorOrder[i], level.Id);
                phase.Groups.Add(group);

                // Collect all teams from this cup's games
                var cupGameIds = new List<long?> { cup.FinalId, cup.SmallFinalId, cup.FirstSemiFinalId, cup.SecondSemiFinalId };
                var cupTeamIds = new HashSet<long>();
                foreach (var gameId in cupGameIds.Where(id => id.HasValue))
                {
                    if (playoffGameMap.TryGetValue(gameId!.Value, out var pg))
                    {
                        if (pg.HomeTeamId.HasValue) cupTeamIds.Add(pg.HomeTeamId.Value);
                        if (pg.AwayTeamId.HasValue) cupTeamIds.Add(pg.AwayTeamId.Value);
                    }
                }

                foreach (var oldTeamId in cupTeamIds)
                {
                    if (teamIdMap.TryGetValue(oldTeamId, out var newTeamId))
                        group.AddTeam(newTeamId);
                }

                // Create games: semi-finals (round 1), final + small final (round 2)
                games.AddRange(CreatePlayoffGame(cup.FirstSemiFinalId, playoffGameMap, gameSetsByPlayoffGame,
                    tournamentId, phase.Id, group.Id, 1, "Semi-final 1",
                    teamIdMap, courtFieldMap, roundMap, tournamentDate));
                games.AddRange(CreatePlayoffGame(cup.SecondSemiFinalId, playoffGameMap, gameSetsByPlayoffGame,
                    tournamentId, phase.Id, group.Id, 1, "Semi-final 2",
                    teamIdMap, courtFieldMap, roundMap, tournamentDate));
                games.AddRange(CreatePlayoffGame(cup.FinalId, playoffGameMap, gameSetsByPlayoffGame,
                    tournamentId, phase.Id, group.Id, 2, "Final",
                    teamIdMap, courtFieldMap, roundMap, tournamentDate));
                games.AddRange(CreatePlayoffGame(cup.SmallFinalId, playoffGameMap, gameSetsByPlayoffGame,
                    tournamentId, phase.Id, group.Id, 2, "3rd place",
                    teamIdMap, courtFieldMap, roundMap, tournamentDate));
            }
        }
        else
        {
            // PlayoffElimination: 8 levels (Color - Final, Color - 3rd place), 1 group per level, 2 teams
            var phase = new Phase
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Playoffs",
                Format = PhaseFormat.PlayoffElimination,
                Order = 3,
                Status = PhaseStatus.Completed
            };
            structure.Phases.Add(phase);

            var levelOrder = 1;
            foreach (var color in colorOrder)
            {
                var cup = cups.FirstOrDefault(c => c.Name == color);
                if (cup is null) continue;

                // Final level
                var finalLevel = Level.Create($"{color} - Final", levelOrder++);
                phase.Levels.Add(finalLevel);

                var finalGroup = Group.Create($"{color} - Final", finalLevel.Id);
                phase.Groups.Add(finalGroup);

                if (cup.FinalId.HasValue && playoffGameMap.TryGetValue(cup.FinalId.Value, out var finalPg))
                {
                    if (finalPg.HomeTeamId.HasValue && teamIdMap.TryGetValue(finalPg.HomeTeamId.Value, out var ht))
                        finalGroup.AddTeam(ht);
                    if (finalPg.AwayTeamId.HasValue && teamIdMap.TryGetValue(finalPg.AwayTeamId.Value, out var at))
                        finalGroup.AddTeam(at);
                }

                games.AddRange(CreatePlayoffGame(cup.FinalId, playoffGameMap, gameSetsByPlayoffGame,
                    tournamentId, phase.Id, finalGroup.Id, 1, "Final",
                    teamIdMap, courtFieldMap, roundMap, tournamentDate));

                // 3rd place level
                var thirdLevel = Level.Create($"{color} - 3rd place", levelOrder++);
                phase.Levels.Add(thirdLevel);

                var thirdGroup = Group.Create($"{color} - 3rd place", thirdLevel.Id);
                phase.Groups.Add(thirdGroup);

                if (cup.SmallFinalId.HasValue && playoffGameMap.TryGetValue(cup.SmallFinalId.Value, out var smallPg))
                {
                    if (smallPg.HomeTeamId.HasValue && teamIdMap.TryGetValue(smallPg.HomeTeamId.Value, out var ht))
                        thirdGroup.AddTeam(ht);
                    if (smallPg.AwayTeamId.HasValue && teamIdMap.TryGetValue(smallPg.AwayTeamId.Value, out var at))
                        thirdGroup.AddTeam(at);
                }

                games.AddRange(CreatePlayoffGame(cup.SmallFinalId, playoffGameMap, gameSetsByPlayoffGame,
                    tournamentId, phase.Id, thirdGroup.Id, 1, "3rd place",
                    teamIdMap, courtFieldMap, roundMap, tournamentDate));
            }
        }

        return games;
    }

    private static List<Game> CreatePlayoffGame(
        long? playoffGameId,
        Dictionary<long, MysqlPlayoffGame> playoffGameMap,
        Dictionary<long, List<MysqlGameSet>> gameSetsByPlayoffGame,
        string tournamentId,
        string phaseId,
        string groupId,
        int round,
        string label,
        Dictionary<long, string> teamIdMap,
        Dictionary<string, string> courtFieldMap,
        Dictionary<long, MysqlRound> roundMap,
        DateTime tournamentDate)
    {
        if (!playoffGameId.HasValue) return [];
        if (!playoffGameMap.TryGetValue(playoffGameId.Value, out var pg)) return [];

        var homeTeamId = pg.HomeTeamId.HasValue && teamIdMap.TryGetValue(pg.HomeTeamId.Value, out var hId) ? hId : null;
        var awayTeamId = pg.AwayTeamId.HasValue && teamIdMap.TryGetValue(pg.AwayTeamId.Value, out var aId) ? aId : null;
        var refTeamId = pg.RefTeamId.HasValue && teamIdMap.TryGetValue(pg.RefTeamId.Value, out var rId) ? rId : null;

        var game = Game.Create(tournamentId, phaseId, groupId, round, homeTeamId, awayTeamId, label: label);

        if (refTeamId is not null)
        {
            game.RefereeTeamId = refTeamId;
        }

        // Assign court
        if (pg.Field is not null && courtFieldMap.TryGetValue(pg.Field, out var courtId))
        {
            var startTime = pg.RoundId.HasValue ? GetStartTime(pg.RoundId.Value, roundMap, tournamentDate) : null;
            if (startTime.HasValue)
                game.AssignSchedule(courtId, startTime.Value);
            else
                game.CourtId = courtId;
        }

        // Record result with sets
        if (pg.Completed && gameSetsByPlayoffGame.TryGetValue(playoffGameId.Value, out var sets))
        {
            var validSets = sets
                .Where(s => s.HomeScore.HasValue && s.AwayScore.HasValue)
                .Select(s => new SetResult(s.HomeScore!.Value, s.AwayScore!.Value))
                .ToList();

            if (validSets.Count > 0)
            {
                game.RecordResult(validSets);
            }
        }

        return [game];
    }

    private static DateTime? GetStartTime(long roundId, Dictionary<long, MysqlRound> roundMap, DateTime tournamentDate)
    {
        if (!roundMap.TryGetValue(roundId, out var round)) return null;
        if (round.Time is null) return null;

        if (TimeOnly.TryParse(round.Time, out var time))
        {
            return tournamentDate.Add(time.ToTimeSpan());
        }

        return null;
    }
}
