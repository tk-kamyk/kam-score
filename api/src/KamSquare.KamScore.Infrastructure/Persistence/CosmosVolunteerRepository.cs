using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Infrastructure.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace KamSquare.KamScore.Infrastructure.Persistence;

public class CosmosVolunteerRepository : CosmosRepository<Volunteer>, IVolunteerRepository
{
    public CosmosVolunteerRepository(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
        : base(cosmosClient, options)
    {
    }

    public async Task<Volunteer?> GetByIdAsync(string id, string tournamentId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
            .WithParameter("@id", id);

        var results = await ExecuteQueryAsync<Volunteer>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<Volunteer>> GetByTournamentIdAsync(string tournamentId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.tournamentId = @tournamentId")
            .WithParameter("@tournamentId", tournamentId);

        return await ExecuteQueryAsync<Volunteer>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
    }

    public async Task<Volunteer> CreateAsync(Volunteer volunteer)
    {
        var response = await Container.CreateItemAsync(
            volunteer,
            new PartitionKey(volunteer.TournamentId));

        return response.Resource;
    }

    public async Task<Volunteer> UpdateAsync(Volunteer volunteer)
    {
        var response = await Container.ReplaceItemAsync(
            volunteer,
            volunteer.Id,
            new PartitionKey(volunteer.TournamentId));

        return response.Resource;
    }

    public async Task DeleteAsync(string id, string tournamentId)
    {
        await Container.DeleteItemAsync<Volunteer>(id, new PartitionKey(tournamentId));
    }

    public async Task DeleteByTournamentIdAsync(string tournamentId)
    {
        var volunteers = await GetByTournamentIdAsync(tournamentId);
        await Task.WhenAll(volunteers.Select(v =>
            Container.DeleteItemAsync<Volunteer>(v.Id, new PartitionKey(tournamentId))));
    }

    public async Task<bool> ExistsByNameAsync(string tournamentId, string name, string? excludeVolunteerId = null)
    {
        var query = excludeVolunteerId is null
            ? new QueryDefinition(
                    "SELECT VALUE COUNT(1) FROM c WHERE c.tournamentId = @tournamentId AND LOWER(c.name) = LOWER(@name)")
                .WithParameter("@tournamentId", tournamentId)
                .WithParameter("@name", name)
            : new QueryDefinition(
                    "SELECT VALUE COUNT(1) FROM c WHERE c.tournamentId = @tournamentId AND LOWER(c.name) = LOWER(@name) AND c.id != @excludeId")
                .WithParameter("@tournamentId", tournamentId)
                .WithParameter("@name", name)
                .WithParameter("@excludeId", excludeVolunteerId);

        var results = await ExecuteQueryAsync<int>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });
        return results.FirstOrDefault() > 0;
    }

    public async Task ClearTeamIdAsync(string tournamentId, string teamId)
    {
        var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.tournamentId = @tournamentId AND c.teamId = @teamId")
            .WithParameter("@tournamentId", tournamentId)
            .WithParameter("@teamId", teamId);

        var volunteers = await ExecuteQueryAsync<Volunteer>(query,
            new QueryRequestOptions { PartitionKey = new PartitionKey(tournamentId) });

        await Task.WhenAll(volunteers.Select(v =>
        {
            v.TeamId = null;
            v.LastModified = DateTime.UtcNow;
            return Container.ReplaceItemAsync(v, v.Id, new PartitionKey(tournamentId));
        }));
    }
}
