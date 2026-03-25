namespace KamSquare.KamScore.Migration.Models;

public record MysqlTournament(long Id, int FieldCount, string Name, string TeamNumber, int Year, long? PlayoffId);

public record MysqlTeam(long Id, int Level, string Name, long TournamentId, string? ContactPerson, string? Email, string? Phone);

public record MysqlPhase(long Id, string Name, string PhaseType, long TournamentId);

public record MysqlGroup(long Id, string Name, long PhaseId);

public record MysqlGame(long Id, int? AwayScore, bool Completed, string? Field, int? HomeScore,
    long? AwayTeamId, long GroupId, long? HomeTeamId, long? RefTeamId, long RoundId);

public record MysqlRound(long Id, string Name, string? Time, long TournamentId);

public record MysqlPlayoff(long Id, string? Name, long TournamentId);

public record MysqlCup(long Id, string Name, long? FinalId, long? FirstSemiFinalId,
    long PlayoffId, long? SecondSemiFinalId, long? SmallFinalId);

public record MysqlPlayoffGame(long Id, bool Completed, string? Field, long? AwayTeamId,
    long CupId, long? HomeTeamId, long? RefTeamId, long? RoundId);

public record MysqlGameSet(long Id, int? AwayScore, int? HomeScore, long PlayoffGameId);

public record MysqlStanding(long Id, int Points, int PointsConceded, int PointsScored,
    int Position, float Ratio, long TeamId, long GroupId);
