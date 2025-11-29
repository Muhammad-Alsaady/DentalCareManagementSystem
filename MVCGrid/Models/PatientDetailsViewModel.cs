using DentalCareManagmentSystem.Application.DTOs;

namespace DentalManagementSystem.Models;

public class PatientDetailsViewModel
{
    public PatientDto Patient { get; set; } = new();
    public List<TreatmentPlanDto> TreatmentPlans { get; set; } = new();
    public List<DiagnosisNoteDto> DiagnosisNotes { get; set; } = new();
    public List<PatientImageDto> PatientImages { get; set; } = new();
    public List<PatientAppointmentDto> Appointments { get; set; } = new();
    public List<PaymentTransactionDto> Payments { get; set; } = new();
    public decimal TotalCost { get; set; }
    public decimal TotalPaid { get; set; }
}

public class DiagnosisNoteDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PatientImageDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
}

public class AddDiagnosisViewModel
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class UploadImageViewModel
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
