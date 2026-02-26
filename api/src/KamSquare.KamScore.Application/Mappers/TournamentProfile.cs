using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Application.Mappers;

public class TournamentProfile : Profile
{
    public TournamentProfile()
    {
        CreateMap<Tournament, TournamentDto>()
            .ForMember(dest => dest.Discipline, opt => opt.MapFrom(src => src.Discipline.ToString()))
            .ForMember(dest => dest.CourtCount, opt => opt.MapFrom(src => src.Courts.Count))
            .ForMember(dest => dest.TeamCount, opt => opt.Ignore());

        CreateMap<GameConditions, GameConditionsDto>();

        CreateMap<GameConditionsDto, GameConditions>();
    }
}
