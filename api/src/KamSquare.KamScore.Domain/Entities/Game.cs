using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.Entities;

public class Game : Entity
{
    public string TournamentId { get; set; } = null!;
    public string PhaseId { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public int Round { get; set; }
    public string? HomeTeamId { get; set; }
    public string? AwayTeamId { get; set; }
    public string? HomeTeamPlaceholder { get; set; }
    public string? AwayTeamPlaceholder { get; set; }
    public string? RefereeTeamId { get; set; }
    public string? CourtId { get; set; }
    public DateTime? StartTime { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Scheduled;
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public List<SetResult>? Sets { get; set; }

    public static Game Create(
        string tournamentId,
        string phaseId,
        string groupId,
        int round,
        string? homeTeamId = null,
        string? awayTeamId = null,
        string? homeTeamPlaceholder = null,
        string? awayTeamPlaceholder = null,
        string? refereeTeamId = null)
    {
        return new Game
        {
            Id = Guid.NewGuid().ToString(),
            TournamentId = tournamentId,
            PhaseId = phaseId,
            GroupId = groupId,
            Round = round,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            HomeTeamPlaceholder = homeTeamPlaceholder,
            AwayTeamPlaceholder = awayTeamPlaceholder,
            RefereeTeamId = refereeTeamId,
            Status = GameStatus.Scheduled,
            LastModified = DateTime.UtcNow
        };
    }

    public void AssignSchedule(string courtId, DateTime startTime)
    {
        CourtId = courtId;
        StartTime = startTime;
        LastModified = DateTime.UtcNow;
    }

    public void RecordResult(List<SetResult> sets)
    {
        Sets = sets;
        HomeScore = sets.Count(s => s.HomePoints > s.AwayPoints);
        AwayScore = sets.Count(s => s.AwayPoints > s.HomePoints);
        Status = GameStatus.Completed;
        LastModified = DateTime.UtcNow;
    }

    public void RecordSimpleResult(int homeScore, int awayScore)
    {
        HomeScore = homeScore;
        AwayScore = awayScore;
        Sets = null;
        Status = GameStatus.Completed;
        LastModified = DateTime.UtcNow;
    }
}
