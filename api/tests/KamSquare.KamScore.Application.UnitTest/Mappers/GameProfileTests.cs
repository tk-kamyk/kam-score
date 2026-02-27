using AutoMapper;
using FluentAssertions;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Mappers;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Application.UnitTest.Mappers;

public class GameProfileTests
{
    private readonly IMapper _mapper;

    public GameProfileTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<GameProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Configuration_ShouldBeValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<GameProfile>());
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Game_To_GameDto_ShouldMapCorrectly()
    {
        var game = Game.Create("t1", "p1", "g1", 1,
            homeTeamId: "home1", awayTeamId: "away1", refereeTeamId: "ref1");
        game.AssignSchedule("court1", new DateTime(2026, 6, 1, 9, 0, 0));

        var dto = _mapper.Map<GameDto>(game);

        dto.Id.Should().Be(game.Id);
        dto.PhaseId.Should().Be("p1");
        dto.GroupId.Should().Be("g1");
        dto.Round.Should().Be(1);
        dto.HomeTeamId.Should().Be("home1");
        dto.AwayTeamId.Should().Be("away1");
        dto.RefereeTeamId.Should().Be("ref1");
        dto.CourtId.Should().Be("court1");
        dto.Status.Should().Be("Scheduled");
        dto.StartTime.Should().Be("2026-06-01T09:00:00");
    }

    [Fact]
    public void Game_To_GameDto_WithPlaceholders_ShouldMapCorrectly()
    {
        var game = Game.Create("t1", "p1", "g1", 2,
            homeTeamPlaceholder: "Winner SF1",
            awayTeamPlaceholder: "Winner SF2");

        var dto = _mapper.Map<GameDto>(game);

        dto.HomeTeamId.Should().BeNull();
        dto.AwayTeamId.Should().BeNull();
        dto.HomeTeamPlaceholder.Should().Be("Winner SF1");
        dto.AwayTeamPlaceholder.Should().Be("Winner SF2");
    }

    [Fact]
    public void Game_To_GameDto_WithNullStartTime_ShouldMapToNull()
    {
        var game = Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b");

        var dto = _mapper.Map<GameDto>(game);

        dto.StartTime.Should().BeNull();
        dto.CourtId.Should().BeNull();
    }

    [Fact]
    public void Game_To_GameDto_ResponseOnlyFieldsShouldBeNull()
    {
        var game = Game.Create("t1", "p1", "g1", 1, homeTeamId: "a", awayTeamId: "b");

        var dto = _mapper.Map<GameDto>(game);

        dto.HomeTeamName.Should().BeNull();
        dto.AwayTeamName.Should().BeNull();
        dto.RefereeTeamName.Should().BeNull();
        dto.CourtName.Should().BeNull();
    }
}
