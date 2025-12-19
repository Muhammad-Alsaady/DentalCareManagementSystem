namespace DentalManagementSystem.Models
{
    public class AddDiagnosisViewModel
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
    }
}
