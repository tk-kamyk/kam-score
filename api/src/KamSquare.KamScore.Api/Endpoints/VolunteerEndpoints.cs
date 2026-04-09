namespace KamSquare.KamScore.Api.Endpoints;

public static class VolunteerEndpoints
{
    public static RouteGroupBuilder MapVolunteerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tournaments/{tournamentId}/volunteers")
            .WithTags("Volunteers")
            .RequireAuthorization();

        group.MapGet("/", GetVolunteers);
        group.MapPost("/", CreateVolunteer);
        group.MapPut("/{volunteerId}", UpdateVolunteer);
        group.MapDelete("/{volunteerId}", DeleteVolunteer);

        return group;
    }

    private static Task<IResult> GetVolunteers(string tournamentId)
        => throw new NotImplementedException();

    private static Task<IResult> CreateVolunteer(string tournamentId)
        => throw new NotImplementedException();

    private static Task<IResult> UpdateVolunteer(string tournamentId, string volunteerId)
        => throw new NotImplementedException();

    private static Task<IResult> DeleteVolunteer(string tournamentId, string volunteerId)
        => throw new NotImplementedException();
}
