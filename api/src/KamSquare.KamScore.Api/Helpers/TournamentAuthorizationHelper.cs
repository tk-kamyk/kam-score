using KamSquare.KamScore.Application.Exceptions;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace KamSquare.KamScore.Api.Helpers;

public static class TournamentAuthorizationHelper
{
    public static bool HasAdminAccess(Tournament tournament, ICurrentUserService currentUser)
    {
        return currentUser.IsAuthenticated &&
            (tournament.IsOwnedBy(currentUser.UserId!) || currentUser.IsAdmin);
    }

    public static async Task<Tournament> GetOwnedTournamentAsync(
        this ITournamentRepository repository,
        ICurrentUserService currentUser,
        string tournamentId)
    {
        var tournament = await repository.GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new NotFoundException(nameof(Tournament), tournamentId);
        if (!HasAdminAccess(tournament, currentUser))
            throw new ForbiddenException();
        return tournament;
    }

    public static void ValidateParticipantAccess(
        Tournament tournament,
        ICurrentUserService currentUser,
        HttpContext httpContext)
    {
        if (HasAdminAccess(tournament, currentUser))
            return;

        var code = httpContext.Request.Headers["X-Tournament-Code"].FirstOrDefault();
        if (code is null)
        {
            if (!currentUser.IsAuthenticated)
                throw new UnauthorizedException("Authentication required.");
            throw new ForbiddenException();
        }
        if (!tournament.IsCodeValid(code))
            throw new ForbiddenException();
    }
}
