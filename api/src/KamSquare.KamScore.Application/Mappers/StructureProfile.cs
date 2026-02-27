using System.Globalization;
using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Mappers;

public class StructureProfile : Profile
{
    public StructureProfile()
    {
        CreateMap<TournamentStructure, TournamentStructureDto>();

        CreateMap<TimeOnly?, string?>().ConvertUsing(src =>
            src.HasValue ? src.Value.ToString("HH:mm", CultureInfo.InvariantCulture) : null);

        CreateMap<string?, TimeOnly?>().ConvertUsing(src =>
            src != null ? ParseTime(src) : null);

        CreateMap<Phase, PhaseDto>()
            .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.Format.ToString()))
            .ForMember(dest => dest.NumberOfGroups, opt => opt.Ignore());

        CreateMap<Group, GroupDto>();
    }

    private static TimeOnly ParseTime(string value) =>
        TimeOnly.ParseExact(value, "HH:mm", CultureInfo.InvariantCulture);
}
