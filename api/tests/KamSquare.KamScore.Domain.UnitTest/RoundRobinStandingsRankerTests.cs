using FluentAssertions;
using KamSquare.KamScore.Domain.Services.Formats;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Domain.UnitTest;

public class RoundRobinStandingsRankerTests
{
    private static Standing MakeStanding(string teamId, int position, int points,
        int setDiff = 0, int pointDiff = 0, int wins = 0, int losses = 0) =>
        new(teamId, position, wins + losses, wins, 0, losses,
            points, null, null, setDiff, null, null, pointDiff);

    // --- RankCrossGroupByStats — pure standings-criteria cascade, no position weighting ---

    [Fact]
    public void RankCrossGroupByStats_OrdersByPointsThenSetDifferenceThenPointDifference()
    {
        var standings = new List<Standing>
        {
            MakeStanding("t1", position: 1, points: 4, setDiff: 1, pointDiff: 5),
            MakeStanding("t2", position: 1, points: 6, setDiff: 2, pointDiff: 8),
            MakeStanding("t3", position: 2, points: 4, setDiff: 3, pointDiff: 10),
        };

        var ranked = RoundRobinStandingsRanker.RankCrossGroupByStats(standings);

        ranked.Select(s => s.TeamId).Should().Equal("t2", "t3", "t1");
    }

    [Fact]
    public void RankCrossGroupByStats_IgnoresGroupPosition()
    {
        // A group winner with bad stats sinks below a stronger runner-up.
        // This is the desired behavior when group position is not privileged
        // (e.g. picking top-N qualifiers by raw performance).
        var winnerWithBadStats = MakeStanding("winner", position: 1, points: 4, setDiff: -3);
        var strongRunnerUp = MakeStanding("runnerUp", position: 2, points: 4, setDiff: 5);

        var ranked = RoundRobinStandingsRanker.RankCrossGroupByStats(
            [winnerWithBadStats, strongRunnerUp]);

        ranked[0].TeamId.Should().Be("runnerUp");
        ranked[1].TeamId.Should().Be("winner");
    }

    // --- RankCrossGroupByPosition — group position primary, stats cascade as tiebreaker ---

    [Fact]
    public void RankCrossGroupByPosition_RanksAllPosition1AboveAllPosition2()
    {
        // Even when a Position-1 team has terrible stats and a Position-2 team has
        // dominant stats, the Position-1 team must rank first.
        var weakWinner = MakeStanding("weakWinner", position: 1, points: 2, setDiff: -5, pointDiff: -30);
        var strongRunnerUp = MakeStanding("strongRunnerUp", position: 2, points: 4, setDiff: 6, pointDiff: 25);

        var ranked = RoundRobinStandingsRanker.RankCrossGroupByPosition(
            [strongRunnerUp, weakWinner]);

        ranked[0].TeamId.Should().Be("weakWinner");
        ranked[1].TeamId.Should().Be("strongRunnerUp");
    }

    [Fact]
    public void RankCrossGroupByPosition_TiebreaksWithinPositionByStatsCascade()
    {
        // Position 1 cluster: t2 has better points; t1 and t3 tie on points but
        // t3 has better set-diff. Position 2 cluster: t4 then t5 by set-diff.
        var t1 = MakeStanding("t1", position: 1, points: 4, setDiff: 1, pointDiff: 5);
        var t2 = MakeStanding("t2", position: 1, points: 6, setDiff: 2, pointDiff: 8);
        var t3 = MakeStanding("t3", position: 1, points: 4, setDiff: 3, pointDiff: 10);
        var t4 = MakeStanding("t4", position: 2, points: 4, setDiff: 5);
        var t5 = MakeStanding("t5", position: 2, points: 4, setDiff: 2);

        var ranked = RoundRobinStandingsRanker.RankCrossGroupByPosition(
            [t5, t1, t4, t3, t2]);

        ranked.Select(s => s.TeamId).Should().Equal("t2", "t3", "t1", "t4", "t5");
    }

    [Fact]
    public void RankCrossGroupByPosition_GroupsAllPositionTiersInOrder()
    {
        // Three positions, three input items. Confirms tier clustering beyond 1-vs-2.
        var p1 = MakeStanding("p1", position: 1, points: 2, setDiff: -1);
        var p2 = MakeStanding("p2", position: 2, points: 6, setDiff: 5);
        var p3 = MakeStanding("p3", position: 3, points: 6, setDiff: 10);

        var ranked = RoundRobinStandingsRanker.RankCrossGroupByPosition(
            [p3, p1, p2]);

        ranked.Select(s => s.TeamId).Should().Equal("p1", "p2", "p3");
    }
}
