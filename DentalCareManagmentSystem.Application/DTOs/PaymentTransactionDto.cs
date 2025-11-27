using DentalCareManagmentSystem.Domain.Enums;

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
    public string PatientName { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingBalance { get; set; }
    public decimal TotalDiscount { get; set; }
    public List<PaymentTransactionDto> Payments { get; set; } = new();
    public List<TreatmentItemDetailDto> TreatmentItems { get; set; } = new();
}
public class TreatmentItemDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public decimal DiscountAmount { get; set; }
}
public class ApplyDiscountRequest
{
    public Guid? PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
}
public class CreatePaymentWithDiscountRequest
{
    public CreatePaymentDto Payment { get; set; } = new();
    public string CreatedBy { get; set; } = string.Empty;
    public DiscountType? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
}