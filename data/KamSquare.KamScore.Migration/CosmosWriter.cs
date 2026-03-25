using KamSquare.KamScore.Domain.Entities;
using Microsoft.Azure.Cosmos;

namespace KamSquare.KamScore.Migration;

public class CosmosWriter
{
    private readonly Database _database;

    public CosmosWriter(CosmosClient client, string databaseName)
    {
        _database = client.GetDatabase(databaseName);
    }

    public async Task WriteAsync(DataTransformer.TransformedTournament data)
    {
        var tournamentId = data.Tournament.Id;
        var ownerId = data.Tournament.OwnerId;

        Console.WriteLine($"  Writing tournament: {data.Tournament.Name}");

        // Tournament — partition key is ownerId
        var tournaments = _database.GetContainer("tournaments");
        await tournaments.UpsertItemAsync(data.Tournament, new PartitionKey(ownerId));
        Console.WriteLine($"    Tournament document written");

        // Teams — partition key is tournamentId
        var teams = _database.GetContainer("teams");
        foreach (var team in data.Teams)
        {
            await teams.UpsertItemAsync(team, new PartitionKey(tournamentId));
        }
        Console.WriteLine($"    {data.Teams.Count} teams written");

        // Courts — partition key is tournamentId
        var courtsContainer = _database.GetContainer("courts");
        foreach (var court in data.Courts)
        {
            await courtsContainer.UpsertItemAsync(court, new PartitionKey(tournamentId));
        }
        Console.WriteLine($"    {data.Courts.Count} courts written");

        // Tournament structure — partition key is tournamentId
        var structures = _database.GetContainer("tournamentstructures");
        await structures.UpsertItemAsync(data.Structure, new PartitionKey(tournamentId));
        Console.WriteLine($"    Tournament structure written ({data.Structure.Phases.Count} phases)");

        // Games — partition key is tournamentId
        var gamesContainer = _database.GetContainer("games");
        foreach (var game in data.Games)
        {
            await gamesContainer.UpsertItemAsync(game, new PartitionKey(tournamentId));
        }
        Console.WriteLine($"    {data.Games.Count} games written");
    }
}
