using DentalCareManagmentSystem.Application.DTOs;

namespace DentalManagementSystem.Models;

public class DashboardViewModel
{
    public int TotalPatients { get; set; }
    public int TodayAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public List<PatientDto> RecentPatients { get; set; } = new();
    public List<AppointmentDto> TodayAppointmentsList { get; set; } = new();
}
