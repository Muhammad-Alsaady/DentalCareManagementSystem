using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCareManagmentSystem.Domain.Entities;

/// <summary>
/// Represents a payment transaction made by a patient
/// </summary>
public class PaymentTransaction
{
    public Guid Id { get; set; }
    
    public Guid PatientId { get; set; }
    
    public Guid? AppointmentId { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }
    
    public DateTime PaymentDate { get; set; }
    
    public string? CreatedBy { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Patient? Patient { get; set; }
    public virtual Appointment? Appointment { get; set; }
    public virtual User? CreatedByUser { get; set; }
}
