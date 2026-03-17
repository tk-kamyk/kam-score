namespace KamSquare.KamScore.Domain.Services;

internal static class SlotMapExtensions
{
    internal static bool ContainsTeam(this Dictionary<int, HashSet<string>> slotMap, int slot, string teamId)
    {
        return slotMap.TryGetValue(slot, out var teams) && teams.Contains(teamId);
    }
}
