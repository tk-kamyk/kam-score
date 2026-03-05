using FluentAssertions;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Domain.UnitTest;

public class TournamentCodeValidationTests
{
    private readonly Tournament _tournament;

    public TournamentCodeValidationTests()
    {
        _tournament = Tournament.Create("Test", Discipline.Volleyball, "owner1");
    }

    [Fact]
    public void IsCodeValid_CorrectCode_ReturnsTrue()
    {
        _tournament.IsCodeValid(_tournament.TournamentCode).Should().BeTrue();
    }

    [Fact]
    public void IsCodeValid_WrongCode_ReturnsFalse()
    {
        _tournament.IsCodeValid("0000").Should().BeFalse();
    }

    [Fact]
    public void IsCodeValid_Null_ReturnsFalse()
    {
        _tournament.IsCodeValid(null).Should().BeFalse();
    }

    [Fact]
    public void IsCodeValid_Empty_ReturnsFalse()
    {
        _tournament.IsCodeValid("").Should().BeFalse();
    }

    [Fact]
    public void IsCodeValid_NonHex_ReturnsFalse()
    {
        _tournament.IsCodeValid("ZZZZ").Should().BeFalse();
    }

    [Fact]
    public void IsCodeValid_CaseInsensitive_ReturnsTrue()
    {
        _tournament.IsCodeValid(_tournament.TournamentCode.ToLower()).Should().BeTrue();
    }

    [Fact]
    public void IsCodeValid_TooShort_ReturnsFalse()
    {
        _tournament.IsCodeValid("A3F").Should().BeFalse();
    }

    [Fact]
    public void IsCodeValid_TooLong_ReturnsFalse()
    {
        _tournament.IsCodeValid("A3F2E").Should().BeFalse();
    }
}
