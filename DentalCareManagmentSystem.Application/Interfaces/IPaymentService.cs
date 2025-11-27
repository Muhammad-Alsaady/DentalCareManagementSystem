using DentalCareManagmentSystem.Application.DTOs;
using DentalCareManagmentSystem.Domain.Enums;

namespace DentalCareManagmentSystem.Application.Interfaces;

/// <summary>
/// Service interface for managing payment transactions
/// </summary>
public interface IPaymentService
{
    Task<PaymentTransactionDto> AddPaymentAsync(CreatePaymentDto payment, string createdBy);
    Task<PaymentTransactionDto> AddPaymentAsync(CreatePaymentDto payment, string createdBy, DiscountType? discountType, decimal? discountValue);
    Task DeletePaymentAsync(Guid paymentId, string deletedBy);
    Task RecalculatePaymentTotalsAsync(Guid patientId);

    // الدوال الجديدة للخصم المرن
    Task ApplyDiscountAsync(ApplyDiscountRequest request);

    // دوال الاستعلام
    List<PaymentTransactionDto> GetPatientPayments(Guid patientId);
    PatientPaymentSummaryDto GetPatientPaymentSummary(Guid patientId);
    decimal GetTotalPaid(Guid patientId);
    decimal GetRemainingBalance(Guid patientId);
    PaymentTransactionDto? GetPaymentById(Guid paymentId);
    decimal GetTotalRevenue(DateTime? startDate = null, DateTime? endDate = null);
    Dictionary<string, decimal> GetRevenueByMonth(int year);
    List<PaymentTransactionDto> GetAllPayments(DateTime? startDate = null, DateTime? endDate = null);
    List<PatientPaymentSummaryDto> GetPatientsWithOutstandingBalance();
}