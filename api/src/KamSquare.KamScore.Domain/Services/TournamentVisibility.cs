using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Domain.Services;

// Visibility policy for tournament lists. See docs/design/tournament.md.
public static class TournamentVisibility
{
    public static IEnumerable<Tournament> VisibleInList(
        IEnumerable<Tournament> tournaments,
        string? viewerUserId,
        bool isAuthenticated,
        bool isAdmin)
    {
        if (isAdmin)
        {
            return tournaments;
        }

        if (isAuthenticated)
        {
            return tournaments.Where(t =>
                t.Type == TournamentType.Public || t.IsOwnedBy(viewerUserId));
        }

        return tournaments.Where(t => t.Type == TournamentType.Public);
    }

    public static IEnumerable<Tournament> CopySources(
        IEnumerable<Tournament> tournaments,
        string? viewerUserId,
        bool isAdmin)
    {
        if (isAdmin)
        {
            return tournaments;
        }

        return tournaments.Where(t =>
            t.Type is TournamentType.Public or TournamentType.Template
            || t.IsOwnedBy(viewerUserId));
    }
}
