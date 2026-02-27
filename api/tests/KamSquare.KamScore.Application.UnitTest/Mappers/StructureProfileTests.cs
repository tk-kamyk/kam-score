using AutoMapper;
using FluentAssertions;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Application.Mappers;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.Enums;

namespace KamSquare.KamScore.Application.UnitTest.Mappers;

public class StructureProfileTests
{
    private readonly IMapper _mapper;

    public StructureProfileTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<StructureProfile>();
        });
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Configuration_ShouldBeValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StructureProfile>());
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void TournamentStructure_MapsToDto()
    {
        var structure = TournamentStructure.Create("tournament-1");
        structure.AddPhase("Group Stage", PhaseFormat.RoundRobin, 2);

        var dto = _mapper.Map<TournamentStructureDto>(structure);

        dto.Id.Should().Be(structure.Id);
        dto.TournamentId.Should().Be("tournament-1");
        dto.Phases.Should().HaveCount(1);
    }

    [Fact]
    public void Phase_MapsToDto_WithFormatAsString()
    {
        var structure = TournamentStructure.Create("tournament-1");
        var phase = structure.AddPhase("Groups", PhaseFormat.PlayoffElimination, 1);

        var dto = _mapper.Map<PhaseDto>(phase);

        dto.Id.Should().Be(phase.Id);
        dto.Name.Should().Be("Groups");
        dto.Format.Should().Be("PlayoffElimination");
        dto.Order.Should().Be(1);
        dto.Groups.Should().HaveCount(1);
    }

    [Fact]
    public void Group_MapsToDto()
    {
        var group = Group.Create("A");
        group.TeamIds.Add("team-1");

        var dto = _mapper.Map<GroupDto>(group);

        dto.Id.Should().Be(group.Id);
        dto.Name.Should().Be("A");
        dto.TeamIds.Should().Contain("team-1");
    }
}
