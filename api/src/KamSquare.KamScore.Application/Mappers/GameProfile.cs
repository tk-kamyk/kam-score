using System.Globalization;
using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Mappers;

public class GameProfile : Profile
{
    public GameProfile()
    {
        CreateMap<Game, GameDto>()
            .ForCtorParam("Status", opt => opt.MapFrom(src => src.Status.ToString()))
            .ForCtorParam("StartTime", opt => opt.MapFrom(src =>
                src.StartTime.HasValue
                    ? src.StartTime.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                    : null))
            .ForMember(dest => dest.HomeTeamName, opt => opt.Ignore())
            .ForMember(dest => dest.AwayTeamName, opt => opt.Ignore())
            .ForMember(dest => dest.RefereeTeamName, opt => opt.Ignore())
            .ForMember(dest => dest.CourtName, opt => opt.Ignore());
    }
}
