namespace KamSquare.KamScore.Application.Services;

// Expresses a caller's intent toward an assignment's station colour, distinguishing
// "leave it untouched" (None) from "set/clear it" (Set, where a null Value clears).
// Replaces a (int? station, bool setStation) parameter pair so the two null meanings
// (absent vs cleared) are no longer conflated.
public readonly record struct StationChange(bool Apply, int? Value)
{
    public static StationChange None => new(false, null);

    public static StationChange Set(int? value) => new(true, value);
}
