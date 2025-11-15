//C:\Users\Options\Downloads\Compressed\DentalCareManagmentSystem-master\DentalCareManagmentSystem.Web\Views\
namespace DentalCareManagmentSystem.Application.DTOs;

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientPhone { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; } // Added
    public string? Status { get; set; }
    public string? Notes { get; set; } // Added
    public decimal PaidAmount { get; set; } = 0; // Amount paid for this appointment
    public decimal TotalCost { get; set; } = 0; // Total cost from treatment plan
    public decimal Remainder => TotalCost - PaidAmount; // Calculated remainder
}
