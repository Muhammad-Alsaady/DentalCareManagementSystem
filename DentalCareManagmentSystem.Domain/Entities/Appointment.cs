using DentalCareManagmentSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCareManagmentSystem.Domain.Entities;

public class Appointment
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; } // Added
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal PaidAmount { get; set; } = 0; // Amount paid for this appointment

    public virtual Patient? Patient { get; set; }
}
