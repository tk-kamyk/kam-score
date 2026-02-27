using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Mappers;

public class StructureProfile : Profile
{
    public StructureProfile()
    {
        CreateMap<TournamentStructure, TournamentStructureDto>();

        CreateMap<Phase, PhaseDto>()
            .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.Format.ToString()))
            .ForMember(dest => dest.NumberOfGroups, opt => opt.Ignore());

        CreateMap<Group, GroupDto>();
    }
}
