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
            .ForMember(dest => dest.Discipline, opt => opt.MapFrom(src => src.Discipline.ToString()));

        CreateMap<GameConditions, GameConditionsDto>();

        CreateMap<GameConditionsDto, GameConditions>();
    }
}
