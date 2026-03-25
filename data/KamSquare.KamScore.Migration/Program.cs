using KamSquare.KamScore.Migration;
using Microsoft.Azure.Cosmos;

// Parse command-line arguments
var dumpFile = GetArg(args, "--dump-file") ?? "data/st.sql";
var connectionString = GetArg(args, "--connection-string")
    ?? Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION_STRING");
var databaseName = GetArg(args, "--database-name") ?? "KamScore";
var ownerId = GetArg(args, "--owner-id");

if (string.IsNullOrEmpty(connectionString))
{
    Console.Error.WriteLine("Error: Cosmos DB connection string required.");
    Console.Error.WriteLine("  Use --connection-string or set COSMOSDB_CONNECTION_STRING env var.");
    return 1;
}

if (string.IsNullOrEmpty(ownerId))
{
    Console.Error.WriteLine("Error: --owner-id is required.");
    return 1;
}

if (!File.Exists(dumpFile))
{
    Console.Error.WriteLine($"Error: Dump file not found: {dumpFile}");
    return 1;
}

Console.WriteLine($"Parsing SQL dump: {dumpFile}");
var parser = new SqlDumpParser(dumpFile);

var tournaments = parser.ParseTournaments();
var teams = parser.ParseTeams();
var phases = parser.ParsePhases();
var groups = parser.ParseGroups();
var games = parser.ParseGames();
var rounds = parser.ParseRounds();
var standings = parser.ParseStandings();
var playoffs = parser.ParsePlayoffs();
var cups = parser.ParseCups();
var playoffGames = parser.ParsePlayoffGames();
var gameSets = parser.ParseGameSets();

Console.WriteLine($"Parsed: {tournaments.Count} tournaments, {teams.Count} teams, " +
    $"{games.Count} games, {playoffGames.Count} playoff games");

var transformer = new DataTransformer(ownerId);

var cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    }
});

// Ensure database and containers exist
var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
await database.Database.CreateContainerIfNotExistsAsync("tournaments", "/ownerId");
await database.Database.CreateContainerIfNotExistsAsync("teams", "/tournamentId");
await database.Database.CreateContainerIfNotExistsAsync("courts", "/tournamentId");
await database.Database.CreateContainerIfNotExistsAsync("tournamentstructures", "/tournamentId");
await database.Database.CreateContainerIfNotExistsAsync("games", "/tournamentId");

var writer = new CosmosWriter(cosmosClient, databaseName);

foreach (var mysqlTournament in tournaments.OrderBy(t => t.Year))
{
    Console.WriteLine($"\nProcessing: {mysqlTournament.Name} (ID: {mysqlTournament.Id})");

    var tournamentTeams = teams.Where(t => t.TournamentId == mysqlTournament.Id).ToList();
    var tournamentPhases = phases.Where(p => p.TournamentId == mysqlTournament.Id).ToList();
    var tournamentPhaseIds = tournamentPhases.Select(p => p.Id).ToHashSet();
    var tournamentGroups = groups.Where(g => tournamentPhaseIds.Contains(g.PhaseId)).ToList();
    var tournamentGroupIds = tournamentGroups.Select(g => g.Id).ToHashSet();
    var tournamentGames = games.Where(g => tournamentGroupIds.Contains(g.GroupId)).ToList();
    var tournamentRounds = rounds.Where(r => r.TournamentId == mysqlTournament.Id).ToList();
    var tournamentStandings = standings.Where(s => tournamentGroupIds.Contains(s.GroupId)).ToList();
    var tournamentPlayoffs = playoffs.Where(p => p.TournamentId == mysqlTournament.Id).ToList();
    var tournamentPlayoffIds = tournamentPlayoffs.Select(p => p.Id).ToHashSet();
    var tournamentCups = cups.Where(c => tournamentPlayoffIds.Contains(c.PlayoffId)).ToList();
    var tournamentCupIds = tournamentCups.Select(c => c.Id).ToHashSet();
    var tournamentPlayoffGames = playoffGames.Where(pg => tournamentCupIds.Contains(pg.CupId)).ToList();
    var tournamentPlayoffGameIds = tournamentPlayoffGames.Select(pg => pg.Id).ToHashSet();
    var tournamentGameSets = gameSets.Where(gs => tournamentPlayoffGameIds.Contains(gs.PlayoffGameId)).ToList();

    Console.WriteLine($"  Teams: {tournamentTeams.Count}, Phases: {tournamentPhases.Count}, " +
        $"Games: {tournamentGames.Count}, Playoff games: {tournamentPlayoffGames.Count}");

    var result = transformer.Transform(
        mysqlTournament,
        tournamentTeams,
        tournamentPhases,
        tournamentGroups,
        tournamentGames,
        tournamentRounds,
        tournamentStandings,
        tournamentPlayoffs,
        tournamentCups,
        tournamentPlayoffGames,
        tournamentGameSets);

    await writer.WriteAsync(result);
}

Console.WriteLine("\nMigration complete!");
return 0;

static string? GetArg(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
