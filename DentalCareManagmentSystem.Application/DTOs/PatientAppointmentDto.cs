using DentalCareManagmentSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DentalCareManagmentSystem.Application.DTOs
{
    public class PatientAppointmentDto
    {
        public Guid Id { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

        public string? FullName { get; set; }
        public int Age { get; set; }
        public string? Phone { get; set; }
        public Gender Gender { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        
        // Payment Information
        public decimal TotalCost { get; set; } = 0;
        public decimal PaidAmount { get; set; } = 0;
        public decimal Remainder => TotalCost - PaidAmount;
    }
}
