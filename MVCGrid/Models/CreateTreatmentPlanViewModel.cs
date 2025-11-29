namespace DentalManagementSystem.Models;

public class CreateTreatmentPlanViewModel
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ItemsJson { get; set; } = "[]";
    public decimal InitialPayment { get; set; }
    public decimal DiscountPercentage { get; set; }
}
