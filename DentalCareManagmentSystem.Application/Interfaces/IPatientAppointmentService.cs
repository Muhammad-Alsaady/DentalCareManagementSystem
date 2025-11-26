using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Application.Interfaces
{
    public interface IPatientAppointmentService
    {
        Task<List<PatientAppointmentDto>> GetAllAsync();
        Task<PatientAppointmentDto?> GetByIdAsync(Guid id);
        Task CreateAsync(CreatePatientAppointmentDto dto);
        Task UpdateAsync(EditPatientAppointmentDto dto);
        Task DeleteAsync(Guid id);
    }

}
