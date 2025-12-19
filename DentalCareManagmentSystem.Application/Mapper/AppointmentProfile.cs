using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AutoMapper;
using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Domain.Enums;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace DentalCareManagmentSystem.Application.Mapper
{

    public class AppointmentProfile : Profile
    {
        public AppointmentProfile()
        {
            CreateMap<PatientAppointment, PatientAppointmentDto>();

            CreateMap<CreatePatientAppointmentDto, PatientAppointment>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.Now))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => AppointmentStatus.Scheduled));
            CreateMap<EditPatientAppointmentDto, PatientAppointment>()
           .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
           .ForMember(dest => dest.IsActive, opt => opt.Ignore())
           .ForMember(dest => dest.Status, opt => opt.Ignore());
            CreateMap<PatientAppointment, EditPatientAppointmentDto>();

            CreateMap<PriceListItem, PriceListItemDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.DefaultPrice, opt => opt.MapFrom(src => src.DefaultPrice))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }

}
