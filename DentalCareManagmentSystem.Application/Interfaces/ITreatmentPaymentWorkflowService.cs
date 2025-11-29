using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Domain.Entities;

namespace DentalCareManagmentSystem.Application.Interfaces;

/// <summary>
/// Service for managing the complete treatment plan and payment workflow
/// </summary>
public interface ITreatmentPaymentWorkflowService
{
    /// <summary>
    /// Create a treatment plan with items for an appointment
    /// </summary>
    Task<WorkflowResult<Guid>> CreateTreatmentPlanAsync(CreateTreatmentPlanWorkflowDto dto, string createdBy);

    /// <summary>
    /// Process a payment for an appointment with treatment plan
    /// </summary>
    Task<WorkflowResult<PaymentSummaryDto>> ProcessPaymentAsync(ProcessPaymentWorkflowDto dto, string processedBy);

    /// <summary>
    /// Get payment summary for an appointment including treatment plan details
    /// </summary>
    Task<WorkflowResult<PaymentSummaryDto>> GetPaymentSummaryAsync(Guid appointmentId);

    /// <summary>
    /// Validate if payment can be processed
    /// </summary>
    Task<WorkflowResult<bool>> ValidatePaymentAsync(Guid appointmentId, decimal amount);

    /// <summary>
    /// Get treatment plan for an appointment
    /// </summary>
    Task<WorkflowResult<TreatmentPlanDto>> GetTreatmentPlanByAppointmentAsync(Guid appointmentId);
}

/// <summary>
/// Result wrapper for workflow operations
/// </summary>
public class WorkflowResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static WorkflowResult<T> Success(T data, string message = "")
    {
        return new WorkflowResult<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };
    }

    public static WorkflowResult<T> Failure(string message, List<string>? errors = null)
    {
        return new WorkflowResult<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}

/// <summary>
/// DTO for creating treatment plan workflow
/// </summary>
public class CreateTreatmentPlanWorkflowDto
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public List<TreatmentItemWorkflowDto> Items { get; set; } = new();
    public decimal InitialPayment { get; set; }
    public decimal DiscountPercentage { get; set; }
}

public class TreatmentItemWorkflowDto
{
    public Guid? PriceListItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// DTO for processing payment workflow
/// </summary>
public class ProcessPaymentWorkflowDto
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public decimal Amount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Payment summary with treatment plan details
/// </summary>
public class PaymentSummaryDto
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public List<TreatmentItemDto> TreatmentItems { get; set; } = new();
    public decimal TotalCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAfterDiscount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingBalance { get; set; }
    public bool IsFullyPaid { get; set; }
    public List<PaymentHistoryDto> PaymentHistory { get; set; } = new();
}

public class PaymentHistoryDto
{
    public Guid Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string ProcessedBy { get; set; } = string.Empty;
}
