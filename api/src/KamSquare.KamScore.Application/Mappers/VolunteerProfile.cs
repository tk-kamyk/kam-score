using AutoMapper;
using KamSquare.KamScore.Application.DTOs;
using KamSquare.KamScore.Domain.Entities;
using KamSquare.KamScore.Domain.ValueObjects;

namespace KamSquare.KamScore.Application.Mappers;

public class VolunteerProfile : Profile
{
    public VolunteerProfile()
    {
        CreateMap<Volunteer, VolunteerDto>();
        CreateMap<ShiftAssignment, ShiftAssignmentDto>()
            .ForCtorParam("ShiftTime", opt => opt.MapFrom(src => src.ShiftTime.HasValue ? src.ShiftTime.Value.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture) : null));
    }
}
