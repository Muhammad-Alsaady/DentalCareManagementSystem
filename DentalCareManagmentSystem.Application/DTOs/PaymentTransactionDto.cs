namespace DentalCareManagmentSystem.Application.DTOs;

/// <summary>
/// DTO for payment transactions
/// </summary>
public class PaymentTransactionDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string? PatientName { get; set; }
    public Guid? AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating new payments
/// </summary>
public class CreatePaymentDto
{
    public Guid PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for patient payment summary
/// </summary>
public class PatientPaymentSummaryDto
{
    public Guid PatientId { get; set; }
    public string? PatientName { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingBalance { get; set; }
    public List<PaymentTransactionDto> Payments { get; set; } = new();
}
