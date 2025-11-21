using DentalCareManagmentSystem.Application.DTOs;

namespace DentalCareManagmentSystem.Application.Interfaces;

/// <summary>
/// Service interface for managing payment transactions
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Add a new payment transaction (async with transaction support)
    /// </summary>
    Task<PaymentTransactionDto> AddPaymentAsync(CreatePaymentDto payment, string createdBy);
    
    /// <summary>
    /// Get all payments for a specific patient
    /// </summary>
    List<PaymentTransactionDto> GetPatientPayments(Guid patientId);
    
    /// <summary>
    /// Get payment summary for a patient including total costs and balances
    /// </summary>
    PatientPaymentSummaryDto GetPatientPaymentSummary(Guid patientId);
    
    /// <summary>
    /// Get total amount paid by a patient
    /// </summary>
    decimal GetTotalPaid(Guid patientId);
    
    /// <summary>
    /// Get remaining balance for a patient
    /// </summary>
    decimal GetRemainingBalance(Guid patientId);
    
    /// <summary>
    /// Get a specific payment by ID
    /// </summary>
    PaymentTransactionDto? GetPaymentById(Guid paymentId);
    
    /// <summary>
    /// Delete a payment transaction (async with transaction support)
    /// </summary>
    Task DeletePaymentAsync(Guid paymentId, string deletedBy);
    
    /// <summary>
    /// Get total revenue for a date range
    /// </summary>
    decimal GetTotalRevenue(DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// Get revenue by month
    /// </summary>
    Dictionary<string, decimal> GetRevenueByMonth(int year);
    
    /// <summary>
    /// Get all payments with filters
    /// </summary>
    List<PaymentTransactionDto> GetAllPayments(DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// Get patients with outstanding balances
    /// </summary>
    List<PatientPaymentSummaryDto> GetPatientsWithOutstandingBalance();
    
    /// <summary>
    /// Recalculate payment totals for a patient and update all related appointments
    /// This ensures consistency between PaymentTransactions and Appointment.PaidAmount
    /// </summary>
    Task RecalculatePaymentTotalsAsync(Guid patientId);
}
