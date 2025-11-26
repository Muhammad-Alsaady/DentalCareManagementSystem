using AutoMapper;
using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Application.Interfaces;
using DentalCareManagmentSystem.Domain.Entities;
using DentalCareManagmentSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Infrastructure.Services
{
    public class PatientAppointmentService : IPatientAppointmentService
    {
        private readonly ClinicDbContext _context;
        private readonly IMapper _mapper;

        public PatientAppointmentService(ClinicDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<PatientAppointmentDto>> GetAllAsync()
        {
            var entities = await _context.PatientAppointments.ToListAsync();
            return _mapper.Map<List<PatientAppointmentDto>>(entities);
        }

        public async Task<PatientAppointmentDto?> GetByIdAsync(Guid id)
        {
            var entity = await _context.PatientAppointments.FindAsync(id);
            return entity == null ? null : _mapper.Map<PatientAppointmentDto>(entity);
        }

        public async Task CreateAsync(CreatePatientAppointmentDto dto)
        {
            var entity = _mapper.Map<PatientAppointment>(dto);
            _context.PatientAppointments.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EditPatientAppointmentDto dto)
        {
            var entity = await _context.PatientAppointments.FindAsync(dto.Id);
            if (entity != null)
            {
                _mapper.Map(dto, entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.PatientAppointments.FindAsync(id);
            if (entity != null)
            {
                _context.PatientAppointments.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }

}
