using System.Globalization;
using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Application.Mappers;

public class GameProfile : Profile
{
    public GameProfile()
    {
        CreateMap<SetResult, SetResultDto>();

        // GameDto is a record with specific constructor parameters. ForCtorParam is required
        // because Status (enum->string) and StartTime (DateTime?->string?) need custom mapping
        // that cannot be inferred from matching property names alone.
        CreateMap<Game, GameDto>()
            .ForCtorParam("Status", opt => opt.MapFrom(src => src.Status.ToString()))
            .ForCtorParam("StartTime", opt => opt.MapFrom(src =>
                src.StartTime.HasValue
                    ? src.StartTime.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                    : null))
            .ForCtorParam("Sets", opt => opt.MapFrom(src => src.Sets))
            .ForMember(dest => dest.HomeTeamName, opt => opt.Ignore())
            .ForMember(dest => dest.AwayTeamName, opt => opt.Ignore())
            .ForMember(dest => dest.RefereeTeamName, opt => opt.Ignore())
            .ForMember(dest => dest.CourtName, opt => opt.Ignore());
    }
}
