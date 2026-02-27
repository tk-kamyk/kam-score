using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Domain.Entities;

public class Phase
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PhaseFormat Format { get; set; }
    public int Order { get; set; }
    public int? GroupWinners { get; set; }
    public int? TotalTeamsProceeding { get; set; }
    public List<Group> Groups { get; set; } = [];

    public static Phase Create(string name, PhaseFormat format, int order, int numberOfGroups,
        int? groupWinners = null, int? totalTeamsProceeding = null)
    {
        var phase = new Phase
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Format = format,
            Order = order,
            GroupWinners = groupWinners,
            TotalTeamsProceeding = totalTeamsProceeding
        };

        for (var i = 0; i < numberOfGroups; i++)
        {
            phase.Groups.Add(Group.Create(GetGroupName(i)));
        }

        return phase;
    }

    public void Update(string name, PhaseFormat format, int? groupWinners, int? totalTeamsProceeding)
    {
        Name = name;
        Format = format;
        GroupWinners = groupWinners;
        TotalTeamsProceeding = totalTeamsProceeding;
    }

    private static string GetGroupName(int index)
    {
        return ((char)('A' + index)).ToString();
    }
}
