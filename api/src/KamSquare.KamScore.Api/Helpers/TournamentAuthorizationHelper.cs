using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;

namespace KamSquare.KamScore.Api.Helpers;

public static class TournamentAuthorizationHelper
{
    public static async Task<Tournament> GetOwnedTournamentAsync(
        this ITournamentRepository repository,
        ICurrentUserService currentUser,
        string tournamentId)
    {
        var tournament = await repository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);
        if (!tournament.IsOwnedBy(currentUser.UserId!))
            throw new ForbiddenException();
        return tournament;
    }
}
