using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;

namespace KamSquare.KamScore.Application.Mappers;

public class CourtProfile : Profile
{
    public CourtProfile()
    {
        CreateMap<Court, CourtDto>();
    }
}
