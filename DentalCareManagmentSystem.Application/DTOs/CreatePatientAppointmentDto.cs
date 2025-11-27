using DentalCareManagmentSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Application.DTOs
{
    public class CreatePatientAppointmentDto
    {
        [Required]
        public string? FullName { get; set; }

        [Required]
        [Range(1, 150)]
        public int Age { get; set; }

        [Required]
        [Phone]
        public string? Phone { get; set; }

        [Required]
        public Gender Gender { get; set; }

        public string? Notes { get; set; }

        [Required]
        public DateTime Date { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;


        [Required]
        public TimeSpan StartTime { get; set; } 

        [Required]
        public TimeSpan EndTime { get; set; }
        
        // Payment Information
        [Range(0, double.MaxValue)]
        public decimal TotalCost { get; set; } = 0;
        
        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; } = 0;
    }
    
    public class EditPatientAppointmentDto : CreatePatientAppointmentDto
    {
        [Required]
        public Guid Id { get; set; }
    }
}
