using System.Text.RegularExpressions;
using KamSquare.KamScore.Application.Exceptions;
using KamSquare.KamScore.Application.Interfaces;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace KamSquare.KamScore.Api.Helpers;

public static partial class TournamentAuthorizationHelper
{
    [GeneratedRegex("^[0-9A-Fa-f]{4}$")]
    private static partial Regex TournamentCodeRegex();

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

    public static void ValidateParticipantAccess(
        Tournament tournament,
        ICurrentUserService currentUser,
        HttpContext httpContext)
    {
        var isOwner = currentUser.IsAuthenticated && tournament.IsOwnedBy(currentUser.UserId!);
        if (isOwner)
            return;

        var code = httpContext.Request.Headers["X-Tournament-Code"].FirstOrDefault();
        if (code is null)
        {
            if (!currentUser.IsAuthenticated)
                throw new UnauthorizedException("Authentication required.");
            throw new ForbiddenException();
        }
        if (!TournamentCodeRegex().IsMatch(code))
            throw new ForbiddenException();
        if (!tournament.TournamentCode.Equals(code, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException();
    }
}
