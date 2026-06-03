using AutoMapper;
using FluentAssertions;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Mappers;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Application.UnitTest.Mappers;

public class TournamentProfileTests
{
    private readonly IMapper _mapper;

    public TournamentProfileTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TournamentProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Configuration_ShouldBeValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TournamentProfile>());
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Tournament_To_TournamentDto_ShouldMapCorrectly()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "user1");

        var dto = _mapper.Map<TournamentDto>(tournament);

        dto.Id.Should().Be(tournament.Id);
        dto.Name.Should().Be("Summer Cup");
        dto.Discipline.Should().Be("Volleyball");
        dto.OwnerId.Should().Be("user1");
        dto.TournamentCode.Should().Be(tournament.TournamentCode);
    }

    [Fact]
    public void Tournament_WithGameConditions_ShouldMapCorrectly()
    {
        var tournament = Tournament.Create("Beach Cup", Discipline.BeachVolleyball, "user1");
        tournament.Update("Beach Cup", Discipline.BeachVolleyball, null, 45,
            new GameConditions(BestOfSets: 3, PointsPerSet: [21, 21, 15]));

        var dto = _mapper.Map<TournamentDto>(tournament);

        dto.GameConditions.Should().NotBeNull();
        dto.GameConditions!.BestOfSets.Should().Be(3);
        dto.GameConditions.PointsPerSet.Should().BeEquivalentTo([21, 21, 15]);
        dto.GameLength.Should().Be(45);
    }

    [Fact]
    public void Tournament_Type_ShouldMapToStringName()
    {
        var tournament = Tournament.Create("Summer Cup", Discipline.Volleyball, "user1");
        tournament.Type = TournamentType.Template;

        var dto = _mapper.Map<TournamentDto>(tournament);

        dto.Type.Should().Be("Template");
    }
}
